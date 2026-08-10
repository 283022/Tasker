using src.Domain.Entities;

namespace src.Repository;

public interface ITeamRepository
{
    void Add(Team team);
    void Delete(Team team);
    IEnumerable<Team> GetAll();
    IEnumerable<Team> GetByUserId(Guid userId);
    Team? GetTeamWithMembers(Guid teamId);
    bool IsUserAdminOfTeam(Guid userId, Guid teamId);
    bool IsUserInTeam(Guid userId, Guid teamId);
    public Team? GetTeamById(Guid teamId);
}  