using Microsoft.EntityFrameworkCore;
using src.Db;
using src.Domain.Entities;

namespace src.Repository;

public class UserTaskRepository(ApplicationContext context, ILogger<UserTaskRepository> logger)
    : IUserTaskRepository
{
    public async Task<bool> IsThisUserAuthorAsync(Guid userId, Guid taskId)
    {
        logger.LogTrace("Checking if user {UserId} is author of task {TaskId}", userId, taskId);
        return await context.Tasks
            .AnyAsync(task => task.Id == taskId && task.CreateByUserId == userId);
    }
    
    public async Task<UserTask?> GetTaskByIdAsync(Guid taskId)
    {
        logger.LogDebug("Fetching task by id {TaskId}", taskId);
        var task = await context.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId);
        
        if (task is null)
            logger.LogDebug("Task {TaskId} not found", taskId);
        
        return task;
    }

    public async Task<IEnumerable<UserTask>> GetTasksByUserAsync(Guid userId)
    {
        logger.LogDebug("Fetching tasks for user {UserId}", userId);
        var tasks = await context.Tasks
            .Where(t => t.CreateByUserId == userId || t.AssignedToUserId == userId)
            .ToListAsync();
        
        logger.LogDebug("Found {Count} tasks for user {UserId}", tasks.Count, userId);
        return tasks;
    }

    // Исправлено: возвращаемый тип теперь Task<IEnumerable<UserTask>>
    public async Task<IEnumerable<UserTask>> GetTasksByTeamIdAsync(Guid teamId)
    {
        logger.LogDebug("Fetching tasks for team {TeamId}", teamId);
        var tasks = await context.Tasks
            .Where(t => t.TeamId == teamId)
            .ToListAsync();
        
        logger.LogDebug("Found {Count} tasks for team {TeamId}", tasks.Count, teamId);
        return tasks;
    }
    
    public async Task DeleteTaskAsync(Guid taskId)
    {
        logger.LogDebug("Deleting task {TaskId}", taskId);
        var task = await context.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId);
            
        if (task != null)
        {
            context.Tasks.Remove(task);
            logger.LogTrace("Task {TaskId} marked for deletion", taskId);
        }
        else
        {
            logger.LogWarning("Task {TaskId} not found for deletion", taskId);
        }
    }

    public async Task AddTaskAsync(UserTask task)
    {
        logger.LogDebug("Adding new task '{TaskName}' created by user {UserId}", 
            task.Name, task.CreateByUserId);
        await context.Tasks.AddAsync(task);
    }
    
    public async Task UpdateTaskAsync(Guid taskId, string newName)
    {
        logger.LogDebug("Updating task {TaskId} name to '{NewName}'", taskId, newName);
        var task = await context.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId);
            
        if (task != null)
        {
            task.ChangeName(newName);
            context.Tasks.Update(task);
            logger.LogTrace("Task {TaskId} name updated to '{NewName}'", taskId, newName);
        }
        else
        {
            logger.LogWarning("Task {TaskId} not found for update", taskId);
        }
    }
    
    public async Task<IReadOnlyList<UserTask>> GetAllTasksAsync()
    {
        logger.LogTrace("Fetching all tasks");
        return await context.Tasks
            .AsNoTracking()
            .ToListAsync();
    }
    
    public async Task<bool> CanUserManageTaskAsync(Guid taskId, Guid userId)
    {
        logger.LogTrace("Checking if user {UserId} can manage task {TaskId}", userId, taskId);
        
        var task = await context.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId);
    
        if (task is null)
        {
            logger.LogWarning("Task {TaskId} not found for permission check", taskId);
            return false;
        }
    
        // 1. Если пользователь — исполнитель задачи
        if (task.AssignedToUserId == userId)
        {
            logger.LogTrace("User {UserId} is the assignee of task {TaskId}", userId, taskId);
            return true;
        }
    
        // 2. Если у задачи есть команда и пользователь — админ этой команды
        if (task.TeamId.HasValue)
        {
            var isAdmin = await context.TeamMembers
                .AnyAsync(tm => tm.TeamId == task.TeamId.Value && 
                                tm.UserId == userId && 
                                tm.IsAdmin);
            if (isAdmin)
            {
                logger.LogTrace("User {UserId} is admin of team {TeamId} for task {TaskId}", 
                    userId, task.TeamId.Value, taskId);
                return true;
            }
        }
    
        // 3. Если пользователь — создатель задачи
        if (task.CreateByUserId == userId)
        {
            logger.LogTrace("User {UserId} is the creator of task {TaskId}", userId, taskId);
            return true;
        }
    
        logger.LogTrace("User {UserId} does not have permission to manage task {TaskId}", userId, taskId);
        return false;
    }
}