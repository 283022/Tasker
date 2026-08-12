using src.Domain.Entities;

namespace src.Repository;

public interface ITeamMemberRepository
{
    // Добавление участника
    Task AddTeamMemberAsync(TeamMember teamMember);
    
    // Удаление участника
    Task DeleteTeamMemberAsync(TeamMember teamMember);
    
    // Получить участника по TeamId и UserId
    Task<TeamMember?> GetByTeamAndUserAsync(Guid teamId, Guid userId);
    
    // Получить всех участников команды
    Task<IEnumerable<TeamMember>> GetByTeamIdAsync(Guid teamId);
    
    // Получить все команды пользователя
    Task<IEnumerable<TeamMember>> GetByUserIdAsync(Guid userId);
    
    // Проверить, является ли пользователь администратором команды
    Task<bool> IsUserAdminAsync(Guid teamId, Guid userId);
    
    // Проверить, состоит ли пользователь в команде
    Task<bool> IsUserInTeamAsync(Guid teamId, Guid userId);
    
    // Получить всех администраторов команды
    Task<IEnumerable<User>> GetTeamAdminsAsync(Guid teamId);
}