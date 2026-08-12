using Microsoft.EntityFrameworkCore;
using src.Db;
using src.Domain.Entities;

namespace src.Repository;

public class TeamRepository(ApplicationContext context) : ITeamRepository
{
    private readonly ApplicationContext _context = context;

    public async Task AddTeamAsync(Team team)
    {
        await _context.Teams.AddAsync(team);
        // SaveChangesAsync вызывается в UnitOfWork
    }

    public async Task DeleteTeamAsync(Team team)
    {
        _context.Teams.Remove(team);
        // SaveChangesAsync вызывается в UnitOfWork
    }

    public async Task<IEnumerable<Team>> GetAllTeamsAsync()
    {
        return await _context.Teams
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Team>> GetTeamsByUserIdAsync(Guid userId)
    {
        return await _context.Teams
            .Where(t => t.Members.Any(m => m.UserId == userId))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Team?> GetTeamWithMembersAsync(Guid teamId)
    {
        return await _context.Teams
            .Include(t => t.Members)
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(t => t.Id == teamId);
    }

    public async Task<Team?> GetTeamByIdAsync(Guid teamId)
    {
        return await _context.Teams
            .FirstOrDefaultAsync(t => t.Id == teamId);
    }
    
    public async Task<bool> IsUserAdminOfTeamAsync(Guid userId, Guid teamId)
    {
        return await _context.TeamMembers
            .AnyAsync(m => m.UserId == userId && m.TeamId == teamId && m.IsAdmin == true);
    }

    public async Task<bool> IsUserInTeamAsync(Guid userId, Guid teamId)
    {
        return await _context.TeamMembers
            .AnyAsync(m => m.UserId == userId && m.TeamId == teamId);
    }
}