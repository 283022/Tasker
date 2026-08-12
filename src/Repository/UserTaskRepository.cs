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

    public async Task<bool> IsThisUserAuthorAsync(Guid userId, Guid taskId)
    {
        return await _context.Tasks
            .AnyAsync(task => task.Id == taskId && task.CreateByUserID == userId);
    }
    
    public async Task<UserTask?> GetTaskByIdAsync(Guid taskId)
    {
        return await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId);
    }

    public async Task<IEnumerable<UserTask>> GetTasksByUserAsync(Guid userId)
    {
        return await _context.Tasks
            .Where(t => t.CreateByUserID == userId || t.AssignedToUserID == userId)
            .ToListAsync();
    }

    // Исправлено: возвращаемый тип теперь Task<IEnumerable<UserTask>>
    public async Task<IEnumerable<UserTask>> GetTasksByTeamIdAsync(Guid teamId)
    {
        return await _context.Tasks
            .Where(t => t.TeamID == teamId)
            .ToListAsync();
    }
    
    public async Task DeleteTaskAsync(Guid taskId)
    {
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId);
            
        if (task != null)
        {
            _context.Tasks.Remove(task);
        }
        // SaveChangesAsync вызывается в UnitOfWork
    }

    public async Task AddTaskAsync(UserTask task)
    {
        await _context.Tasks.AddAsync(task);
        // SaveChangesAsync вызывается в UnitOfWork
    }
    
    public async Task UpdateTaskAsync(Guid taskId, string newName)
    {
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId);
            
        if (task != null)
        {
            task.ChangeName(newName);
            _context.Tasks.Update(task);
        }
        // SaveChangesAsync вызывается в UnitOfWork
    }
    
    public async Task<IReadOnlyList<UserTask>> GetAllTasksAsync()
    {
        return await _context.Tasks
            .AsNoTracking()
            .ToListAsync();
    }
    
    public async Task<bool> CanUserManageTaskAsync(Guid taskId, Guid userId)
    {
        // 1. Получаем задачу с информацией о команде
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId);
    
        if (task is null)
            return false;
    
        // 2. Если пользователь — исполнитель задачи
        if (task.AssignedToUserID == userId)
            return true;
    
        // 3. Если у задачи есть команда и пользователь — админ этой команды
        if (task.TeamID.HasValue)
        {
            var isAdmin = await _context.TeamMembers
                .AnyAsync(tm => tm.TeamId == task.TeamID.Value && 
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