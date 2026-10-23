using Microsoft.EntityFrameworkCore;
using src.Db;
using src.Domain.Entities;

namespace src.Repository;

public class SessionRepository(ApplicationContext context)
{
    public async Task<Session?> GetByRefreshTokenHashAsync(
        string hash,
        CancellationToken cancellationToken = default)
    {
        return await context.Set<Session>()
            .FirstOrDefaultAsync(
                x => x.RefreshTokenHash == hash,
                cancellationToken);
    }

    public async Task AddAsync(
        Session session,
        CancellationToken cancellationToken = default)
    {
        await context.Set<Session>()
            .AddAsync(session, cancellationToken);
    }
    
    public async Task RevokeByRefreshTokenHashAsync(
        string hash,
        DateTimeOffset revokedAt,
        CancellationToken cancellationToken = default)
    {
        await context.Sessions
            .Where(s =>
                s.RefreshTokenHash == hash)
            .ExecuteDeleteAsync(
                cancellationToken);
    }
    
    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}