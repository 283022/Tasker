using Microsoft.EntityFrameworkCore;
using src.Db;
using src.Domain.Entities;

namespace src.Repository;

public class TeamRepository(ApplicationContext context, ILogger<TeamRepository> logger) : ITeamRepository
{
    public async Task AddTeamAsync(Team team)
    {
        logger.LogDebug("Adding new team '{TeamName}'", team.Name);
        await context.Teams.AddAsync(team);
    }

    public async Task DeleteTeamAsync(Team team)
    {
        logger.LogDebug("Deleting team {TeamId} '{TeamName}'", team.Id, team.Name);
        context.Teams.Remove(team);
    }

    public async Task<IEnumerable<Team>> GetAllTeamsAsync()
    {
        logger.LogTrace("Fetching all teams");
        return await context.Teams
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Team>> GetTeamsByUserIdAsync(Guid userId)
    {
        logger.LogDebug("Fetching teams for user {UserId}", userId);
        var teams = await context.Teams
            .Where(t => t.Members.Any(m => m.UserId == userId))
            .AsNoTracking()
            .ToListAsync();
        
        logger.LogDebug("Found {Count} teams for user {UserId}", teams.Count, userId);
        return teams;
    }

    public async Task<Team?> GetTeamWithMembersAsync(Guid teamId)
    {
        logger.LogDebug("Fetching team {TeamId} with members", teamId);
        var team = await context.Teams
            .Include(t => t.Members)
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(t => t.Id == teamId);
        
        if (team is null)
            logger.LogDebug("Team {TeamId} not found", teamId);
        else
            logger.LogDebug("Team {TeamId} found with {MemberCount} members", teamId, team.Members?.Count ?? 0);
        
        return team;
    }

    public async Task<Team?> GetTeamByIdAsync(Guid teamId)
    {
        logger.LogDebug("Fetching team by id {TeamId}", teamId);
        var team = await context.Teams
            .FirstOrDefaultAsync(t => t.Id == teamId);
        
        if (team is null)
            logger.LogDebug("Team {TeamId} not found", teamId);
        
        return team;
    }
    
    public async Task<bool> IsUserAdminOfTeamAsync(Guid userId, Guid teamId)
    {
        logger.LogTrace("Checking if user {UserId} is admin of team {TeamId}", userId, teamId);
        return await context.TeamMembers
            .AnyAsync(m => m.UserId == userId && m.TeamId == teamId && m.IsAdmin);
    }

    public async Task<bool> IsUserInTeamAsync(Guid userId, Guid teamId)
    {
        logger.LogTrace("Checking if user {UserId} is in team {TeamId}", userId, teamId);
        return await context.TeamMembers
            .AnyAsync(m => m.UserId == userId && m.TeamId == teamId);
    }
}