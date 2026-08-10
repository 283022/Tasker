using Microsoft.EntityFrameworkCore;
using src.Db;
using src.Domain.Entities;

namespace src.Repository;

public class TeamRepository(ApplicationContext context) : ITeamRepository
{
    private readonly ApplicationContext _context = context;

    public void Add(Team team)
    {
        _context.Teams.Add(team);
    }

    public void Delete(Team team)
    {
        _context.Teams.Remove(team);
    }

    public IEnumerable<Team> GetAll()
    {
        return _context.Teams.AsNoTracking().ToList();
    }

    public IEnumerable<Team> GetByUserId(Guid userId)
    {
        return _context.Teams
            .Where(t=>t.Members.Any(m=>m.UserId == userId))
            .AsNoTracking()
            .ToList();
    }

    public Team? GetTeamWithMembers(Guid teamId)
    {
        return _context.Teams
            .Include(t => t.Members)
            .ThenInclude(m => m.User)
            .FirstOrDefault(t => t.Id == teamId);
        
    }

    public Team? GetTeamById(Guid teamId)
    {
        return _context.Teams.FirstOrDefault(t => t.Id == teamId);
    }
    
    public bool IsUserAdminOfTeam(Guid userId, Guid teamId)
    {
        return _context.TeamMembers.Any(m => m.UserId == userId && m.TeamId == teamId && m.IsAdmin == true);
    }

    public bool IsUserInTeam(Guid userId, Guid teamId)
    {
        return  _context.TeamMembers.Any(m => m.UserId == userId && m.TeamId == teamId);
    }
}