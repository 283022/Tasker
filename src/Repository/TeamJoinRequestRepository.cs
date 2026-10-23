using Microsoft.EntityFrameworkCore;
using src.Db;
using src.Domain.Entities;

namespace src.Repository;

public class TeamJoinRequestRepository(ApplicationContext context)

{
    public async Task<List<TeamJoinRequest>> GetAllByTeamIdAsync(
        Guid teamId)
    {
        return await context.TeamJoinRequests
            .AsNoTracking()
            .Where(request => request.TeamId == teamId)
            .OrderByDescending(request => request.CreatedAt)
            .ToListAsync();
    }
    
    public async Task<TeamJoinRequest?> GetByIdAsync(Guid requestId)
    {
        return await context.TeamJoinRequests
            .FirstOrDefaultAsync(r => r.Id == requestId);
    }

    public async Task<TeamJoinRequest?> GetPendingByTeamAndUserAsync(
        Guid teamId,
        Guid userId)
    {
        return await context.TeamJoinRequests
            .FirstOrDefaultAsync(r =>
                r.TeamId == teamId &&
                r.UserId == userId &&
                r.Status == JoinRequestStatus.Pending.ToString());
    }

    public async Task AddAsync(TeamJoinRequest request)
    {
        await context.TeamJoinRequests.AddAsync(request);
    }
}