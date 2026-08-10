using src.Domain.Entities;

namespace src.Repository;

public interface ITeamMemberRepository
{
    // Добавление участника
    void Add(TeamMember teamMember);
    
    // Удаление участника
    void Delete(TeamMember teamMember);
    
    // Получить участника по TeamId и UserId
    TeamMember? GetByTeamAndUser(Guid teamId, Guid userId);
    
    // Получить всех участников команды
    IEnumerable<TeamMember> GetByTeamId(Guid teamId);
    
    // Получить все команды пользователя
    IEnumerable<TeamMember> GetByUserId(Guid userId);
    
    // Проверить, является ли пользователь администратором команды
    bool IsUserAdmin(Guid teamId, Guid userId);
    
    // Проверить, состоит ли пользователь в команде
    bool IsUserInTeam(Guid teamId, Guid userId);
    
    // Получить всех администраторов команды
    IEnumerable<User> GetTeamAdmins(Guid teamId);
}