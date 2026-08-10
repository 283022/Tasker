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
            .Where(t => t.CreateByUserID == userId || t.AssignedToUserID == userId)
            .ToList();
    }

    public IEnumerable<UserTask> GetTasksByTeamId(Guid teamId)
    {
        return _context.Tasks.Where(t => t.TeamID == teamId).ToList();
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
            task.ChangeName(newName);
            _context.Tasks.Update(task);
        }
    }
    
    public  IReadOnlyList<UserTask> GetAllTasks()
    {
        return  _context.Tasks
            .AsNoTracking()
            .ToList();
    }
    
    
    public bool CanUserManageTask(Guid taskId, Guid userId)
    {
        // 1. Получаем задачу с информацией о команде
        var task = _context.Tasks
            .FirstOrDefault(t => t.Id == taskId);
    
        if (task is null)
            return false;
    
        // 2. Если пользователь — исполнитель задачи
        if (task.AssignedToUserID == userId)
            return true;
    
        // 3. Если у задачи есть команда и пользователь — админ этой команды
        if (task.TeamID.HasValue)
        {
            var isAdmin = _context.TeamMembers
                .Any(tm => tm.TeamId == task.TeamID.Value && 
                           tm.UserId == userId && 
                           tm.IsAdmin);
            if (isAdmin)
                return true;
        }
    
        // 4. Если пользователь — создатель задачи
        if (task.CreateByUserID == userId)
            return true;
    
        return false;
    }
    
}