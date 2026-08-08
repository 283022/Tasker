using Microsoft.EntityFrameworkCore;
using src.Domain.Entities;
using src.Db;

namespace src;

public class UserTaskRepository : IUserTaskRepository
{
    private readonly ApplicationContext _context;

    public UserTaskRepository(ApplicationContext context)
    {
        _context = context;
    }

    public bool IsThisUserAuthor(Guid userId, Guid taskId)
    {
        return _context.Tasks
            .Any(task => task.Id == taskId && task.CreateByUserID == userId);
    }
    
    public UserTask? GetTaskById(Guid taskId)
    {
        return _context.Tasks
            .FirstOrDefault(t => t.Id == taskId);
    }

    public IEnumerable<UserTask> GetTasksByUser(Guid userId)
    {
        return  _context.Tasks
            .Where(t => t.CreateByUserID == userId)
            .ToList();
    }
    
    public void Delete(Guid taskId)
    {
        var task =  _context.Tasks
            .FirstOrDefault(t => t.Id == taskId);
            
        if (task != null)
        {
            _context.Tasks.Remove(task);
        }
    }

    public void Add(UserTask task)
    {
         _context.Tasks.Add(task);
    }
    
    public void Update(Guid taskId, string newName)
    {
        var task =  _context.Tasks
            .FirstOrDefault(t => t.Id == taskId);
            
        if (task != null)
        {
            task.Name = newName;
            _context.Tasks.Update(task);
        }
    }
    
    public  IReadOnlyList<UserTask> GetAllTasks()
    {
        return  _context.Tasks
            .AsNoTracking()
            .ToList();
    }
}