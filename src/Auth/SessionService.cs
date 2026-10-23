using System.Security.Cryptography;
using System.Text;
using src.Domain;
using src.Domain.Entities;
using src.Repository;

namespace src.Auth;

public class SessionService(SessionRepository repository, TokenService tokenService, IConfiguration configuration)
{
    private readonly int _refreshTokenExpiryDays = configuration.GetValue<int>(
            "Auth:RefreshTokenExpiryDays", 30);

    //Todo: тесты
    public async Task<Result<SessionTokens>> CreateAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var refreshToken = tokenService.GenerateRefreshToken();

        var session = new Session
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RefreshTokenHash = HashToken(refreshToken),
            CreatedAt = now,
            ExpiresAt = now.AddDays(_refreshTokenExpiryDays)
        };

        await repository.AddAsync(session, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        var accessToken = tokenService.GenerateToken(userId);

        return Result<SessionTokens>.Success(
            new SessionTokens(
                session.Id,
                accessToken,
                refreshToken,
                session.ExpiresAt));
    }

    public async Task<Result<SessionTokens>> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result<SessionTokens>.Failure(
                new Error("Auth", "Refresh token is required."));
        }

        var hash = HashToken(refreshToken);

        var session = await repository.GetByRefreshTokenHashAsync(
            hash, cancellationToken);

        if (session is null)
        {
            return Result<SessionTokens>.Failure(
                new Error("Auth", "Invalid refresh token."));
        }

        var now = DateTimeOffset.UtcNow;

        if (session.ExpiresAt <= now)
        {
            return Result<SessionTokens>.Failure(
                new Error("Auth", "Refresh token has expired."));
        }

        var newRefreshToken = tokenService.GenerateRefreshToken();

        var nextSession = new Session
        {
            Id = Guid.NewGuid(),
            UserId = session.UserId,
            RefreshTokenHash = HashToken(newRefreshToken),
            CreatedAt = now,
            ExpiresAt = now.AddDays(_refreshTokenExpiryDays)
        };
        
        await repository.AddAsync(nextSession, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        var accessToken = tokenService.GenerateToken(session.UserId);

        return Result<SessionTokens>.Success(
            new SessionTokens(
                nextSession.Id,
                accessToken,
                newRefreshToken,
                nextSession.ExpiresAt));
    }

    public async Task<Result<bool>> LogoutAsync(
        string? refreshToken,
        CancellationToken cancellationToken = default)
    {
        // Logout должен оставаться успешным,
        // даже если cookie уже отсутствует.
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Result<bool>.Success(true);

        var hash = HashToken(refreshToken);

        await repository.RevokeByRefreshTokenHashAsync(
            hash,
            DateTimeOffset.UtcNow,
            cancellationToken);

        // Не раскрываем клиенту, существовал ли токен в БД.
        return Result<bool>.Success(true);
    }
 
    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(token));

        return Convert.ToBase64String(bytes);
    }
}