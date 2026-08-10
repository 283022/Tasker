using Microsoft.EntityFrameworkCore;
using src.Db;
using src.Domain.Entities;

namespace src.Repository;

public class TeamMemberRepository(ApplicationContext context) : ITeamMemberRepository
{
    private readonly ApplicationContext _context = context;

    public void Add(TeamMember teamMember)
    {
        _context.TeamMembers.Add(teamMember);
    }

    public void Delete(TeamMember teamMember)
    {
        _context.TeamMembers.Remove(teamMember);
    }

    public TeamMember? GetByTeamAndUser(Guid teamId, Guid userId)
    {
        return _context.TeamMembers
            .FirstOrDefault(tm => tm.TeamId == teamId && tm.UserId == userId);
    }

    public IEnumerable<TeamMember> GetByTeamId(Guid teamId)
    {
        return _context.TeamMembers
            .Where(tm => tm.TeamId == teamId)
            .Include(tm => tm.User) // Загружаем данные пользователя
            .AsNoTracking()
            .ToList();
    }

    public IEnumerable<TeamMember> GetByUserId(Guid userId)
    {
        return _context.TeamMembers
            .Where(tm => tm.UserId == userId)
            .Include(tm => tm.Team)
            .AsNoTracking()
            .ToList();
    }

    public bool IsUserAdmin(Guid teamId, Guid userId)
    {
        return _context.TeamMembers
            .Any(tm => tm.TeamId == teamId 
                       && tm.UserId == userId 
                       && tm.IsAdmin);
    }

    public bool IsUserInTeam(Guid teamId, Guid userId)
    {
        return _context.TeamMembers
            .Any(tm => tm.TeamId == teamId && tm.UserId == userId);
    }

    public IEnumerable<User> GetTeamAdmins(Guid teamId)
    {
        return _context.TeamMembers
            .Where(tm => tm.TeamId == teamId && tm.IsAdmin)
            .Select(tm => tm.User)
            .AsNoTracking()
            .ToList();
    }
}