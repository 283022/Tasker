using src.Domain.Entities;

namespace src;

public interface IUserTaskRepository
{
    Task<bool> IsThisUserAuthorAsync(Guid userId, Guid taskId);
    Task<UserTask?> GetTaskByIdAsync(Guid taskId);
    Task<IEnumerable<UserTask>> GetTasksByUserAsync(Guid userId);
    Task DeleteTaskAsync(Guid taskId);
    Task AddTaskAsync(UserTask task);
    Task UpdateTaskAsync(Guid taskId, string newName);
    Task<IReadOnlyList<UserTask>> GetAllTasksAsync();
    Task<bool> CanUserManageTaskAsync(Guid taskId, Guid userId);
    Task<IEnumerable<UserTask>> GetTasksByTeamIdAsync(Guid teamId);
}