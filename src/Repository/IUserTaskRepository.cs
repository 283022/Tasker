using src.Domain.Entities;

namespace src;

public interface IUserTaskRepository
{
    public bool IsThisUserAuthor(Guid userId, Guid taskId);
    public UserTask? GetTaskById(Guid taskId);
    public IEnumerable<UserTask> GetTasksByUser(Guid userId);
    public void Delete(Guid taskid);
    public void Add(UserTask task);
    public void Update(Guid taskId, string newName);
    public IReadOnlyList<UserTask> GetAllTasks();
    public bool CanUserManageTask(Guid taskId, Guid userId);
    public IEnumerable<UserTask> GetTasksByTeamId(Guid teamId);
}