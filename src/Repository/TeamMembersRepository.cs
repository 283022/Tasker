using Microsoft.EntityFrameworkCore;
using src.Db;
using src.Domain.Entities;

namespace src.Repository;

public class TeamMemberRepository(ApplicationContext context) : ITeamMemberRepository
{
    private readonly ApplicationContext _context = context;

    public async Task AddTeamMemberAsync(TeamMember teamMember)
    {
        await _context.TeamMembers.AddAsync(teamMember);
        // SaveChangesAsync вызывается в UnitOfWork
    }

    public async Task DeleteTeamMemberAsync(TeamMember teamMember)
    {
        _context.TeamMembers.Remove(teamMember);
        // SaveChangesAsync вызывается в UnitOfWork
    }

    public async Task<TeamMember?> GetByTeamAndUserAsync(Guid teamId, Guid userId)
    {
        return await _context.TeamMembers
            .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == userId);
    }

    public async Task<IEnumerable<TeamMember>> GetByTeamIdAsync(Guid teamId)
    {
        return await _context.TeamMembers
            .Where(tm => tm.TeamId == teamId)
            .Include(tm => tm.User)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<TeamMember>> GetByUserIdAsync(Guid userId)
    {
        return await _context.TeamMembers
            .Where(tm => tm.UserId == userId)
            .Include(tm => tm.Team)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> IsUserAdminAsync(Guid teamId, Guid userId)
    {
        return await _context.TeamMembers
            .AnyAsync(tm => tm.TeamId == teamId 
                            && tm.UserId == userId 
                            && tm.IsAdmin);
    }

    public async Task<bool> IsUserInTeamAsync(Guid teamId, Guid userId)
    {
        return await _context.TeamMembers
            .AnyAsync(tm => tm.TeamId == teamId && tm.UserId == userId);
    }

    public async Task<IEnumerable<User>> GetTeamAdminsAsync(Guid teamId)
    {
        return await _context.TeamMembers
            .Where(tm => tm.TeamId == teamId && tm.IsAdmin)
            .Select(tm => tm.User)
            .AsNoTracking()
            .ToListAsync();
    }
}