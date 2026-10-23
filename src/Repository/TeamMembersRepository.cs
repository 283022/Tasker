using Microsoft.EntityFrameworkCore;
using src.Db;
using src.Domain.Entities;

namespace src.Repository;

public class TeamMemberRepository : ITeamMemberRepository
{
    private readonly ApplicationContext _context;
    private readonly ILogger<TeamMemberRepository> _logger;

    public TeamMemberRepository(ApplicationContext context, ILogger<TeamMemberRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AddTeamMemberAsync(TeamMember teamMember)
    {
        _logger.LogDebug("Adding user {UserId} to team {TeamId}, isAdmin: {IsAdmin}", 
            teamMember.UserId, teamMember.TeamId, teamMember.IsAdmin);
        await _context.TeamMembers.AddAsync(teamMember);
    }

    public async Task DeleteTeamMemberAsync(TeamMember teamMember)
    {
        _logger.LogDebug("Removing user {UserId} from team {TeamId}", 
            teamMember.UserId, teamMember.TeamId);
        _context.TeamMembers.Remove(teamMember);
    }

    public async Task<TeamMember?> GetByTeamAndUserAsync(Guid teamId, Guid userId)
    {
        _logger.LogTrace("Fetching team member for team {TeamId}, user {UserId}", teamId, userId);
        var member = await _context.TeamMembers
            .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == userId);
        
        if (member is null)
            _logger.LogTrace("Team member not found for team {TeamId}, user {UserId}", teamId, userId);
        
        return member;
    }

    public async Task<IEnumerable<TeamMember>> GetByTeamIdAsync(Guid teamId)
    {
        _logger.LogDebug("Fetching all members for team {TeamId}", teamId);
        var members = await _context.TeamMembers
            .Where(tm => tm.TeamId == teamId)
            .Include(tm => tm.User)
            .AsNoTracking()
            .ToListAsync();
        
        _logger.LogDebug("Found {Count} members in team {TeamId}", members.Count, teamId);
        return members;
    }

   

    public async Task<bool> IsUserAdminAsync(Guid teamId, Guid userId)
    {
        _logger.LogTrace("Checking if user {UserId} is admin of team {TeamId}", userId, teamId);
        return await _context.TeamMembers
            .AnyAsync(tm => tm.TeamId == teamId 
                            && tm.UserId == userId 
                            && tm.IsAdmin);
    }

    public async Task<bool> IsUserInTeamAsync(Guid teamId, Guid userId)
    {
        _logger.LogTrace("Checking if user {UserId} is in team {TeamId}", userId, teamId);
        return await _context.TeamMembers
            .AnyAsync(tm => tm.TeamId == teamId && tm.UserId == userId);
    }

    public async Task<IEnumerable<User>> GetTeamAdminsAsync(Guid teamId)
    {
        _logger.LogDebug("Fetching admins for team {TeamId}", teamId);
        var admins = await _context.TeamMembers
            .Where(tm => tm.TeamId == teamId && tm.IsAdmin)
            .Select(tm => tm.User)
            .AsNoTracking()
            .ToListAsync();
        
        _logger.LogDebug("Found {Count} admins in team {TeamId}", admins.Count, teamId);
        return admins;
    }
}