using src.Domain.Entities;

namespace src.Repository;

public interface ITeamRepository
{
    Task AddTeamAsync(Team team);
    Task DeleteTeamAsync(Team team);
    Task<IEnumerable<Team>> GetAllTeamsAsync();
    Task<IEnumerable<Team>> GetTeamsByUserIdAsync(Guid userId);
    Task<Team?> GetTeamWithMembersAsync(Guid teamId);
    Task<Team?> GetTeamByIdAsync(Guid teamId);
    Task<bool> IsUserAdminOfTeamAsync(Guid userId, Guid teamId);
    Task<bool> IsUserInTeamAsync(Guid userId, Guid teamId);
}