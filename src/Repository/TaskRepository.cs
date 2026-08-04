using src.Domain.Entities;

namespace src;

public class TaskRepository
{
    private readonly List<UserTask> _tasks = [];

    public bool IsThisUserAuthor(Guid userId, Guid taskId)
    {
        return _tasks.FirstOrDefault((task) => task.Id == userId && task.CreateByUserID == userId) != null;
    }
    
    
    public UserTask? GetTaskById(Guid taskId)
    {
        return _tasks.FirstOrDefault(t => t.Id == taskId);
    }

    public IEnumerable<UserTask> GetTasksByUser(Guid userId)
    {
        return _tasks.Where(t => t.CreateByUserID == userId);
    }
    
    public void Delete(Guid taskid)
    {
        for (var index = 0; index < _tasks.Count; index++)
        {
            var task = _tasks[index];
            if (task.Id == taskid)
            {
                _tasks.Remove(task);
            }
        }
    }

    public Guid Create(UserTask task)
    {
        _tasks.Add(task);
        return task.Id;
    }
    
    public void Update(Guid taskId, string newName)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == taskId);
        if (task != null)
        {
            task.Name = newName;
        }
    }
    
}