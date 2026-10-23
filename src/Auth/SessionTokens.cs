namespace src.Auth;

public record SessionTokens(
    Guid SessionId,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);