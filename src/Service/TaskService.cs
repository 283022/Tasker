using src.Domain;
using src.Domain.Entities;
using src.UnitOfWork;

namespace src.Service;

public class TaskService(IUnitOfWork unitOfWork, ILogger<TaskService> logger)
{
    public async Task<Result<Guid>> CreateAsync(CreateTaskDto createTaskDto, Guid currentUserId)
    {
        logger.LogInformation("Creating new task for user {UserId}: '{TaskName}'", 
            currentUserId, createTaskDto?.Name);

        if (createTaskDto is null)
        {
            logger.LogWarning("Task creation failed: request is null");
            return Result<Guid>.Failure(Errors.Validation("Request cannot be null."));
        }

        var results = UserTask.Create(
            createTaskDto.Name,
            currentUserId,
            createTaskDto.teamId,
            createTaskDto.assignedToUserId);

        if (results.IsFailure)
        {
            logger.LogWarning("Task creation failed: invalid task data. Error: {Error}", results.Error.Message);
            return Result<Guid>.Failure(results.Error);
        }

        var task = results.Value;

        if (task.CreateByUserID != currentUserId)
        {
            logger.LogWarning("Task creation failed: user {UserId} cannot create task for another user", currentUserId);
            return Result<Guid>.Failure(Errors.Forbidden("Cannot create task for another user."));
        }

        bool isPersonal = task.AssignedToUserID == null && task.TeamID == null;
        bool isTeamTask = task.AssignedToUserID != null && task.TeamID != null;
        bool isInvalid = (task.AssignedToUserID == null && task.TeamID != null) ||
                         (task.AssignedToUserID != null && task.TeamID == null);

        if (isInvalid)
        {
            logger.LogWarning("Task creation failed: invalid combination of AssignedToUserID and TeamID");
            return Result<Guid>.Failure(Errors.Validation(
                "Both AssignedToUserID and TeamID must be provided or both must be null."));
        }

        if (isTeamTask)
        {
            var teamId = task.TeamID.Value;
            var assignedUserId = task.AssignedToUserID.Value;

            logger.LogDebug("Creating team task in team {TeamId} for user {AssignedUserId}", teamId, assignedUserId);

            var team = await unitOfWork.TeamRepository.GetTeamByIdAsync(teamId);
            if (team is null)
            {
                logger.LogWarning("Task creation failed: team {TeamId} not found", teamId);
                return Result<Guid>.Failure(Errors.NotFound("Team", teamId.ToString()));
            }

            var isAdmin = await unitOfWork.TeamRepository.IsUserAdminOfTeamAsync(currentUserId, teamId);
            if (!isAdmin)
            {
                logger.LogWarning("Task creation failed: user {UserId} is not admin of team {TeamId}", 
                    currentUserId, teamId);
                return Result<Guid>.Failure(Errors.Forbidden(
                    "Only team administrators can create tasks in this team."));
            }

            var isUserInTeam = await unitOfWork.TeamRepository.IsUserInTeamAsync(assignedUserId, teamId);
            if (!isUserInTeam)
            {
                logger.LogWarning("Task creation failed: user {AssignedUserId} is not in team {TeamId}", 
                    assignedUserId, teamId);
                return Result<Guid>.Failure(Errors.Validation(
                    $"User '{assignedUserId}' is not a member of this team.", "assignedToUserId"));
            }

            var assignedUser = await unitOfWork.UserRepository.GetUserByIdAsync(assignedUserId);
            if (assignedUser is null)
            {
                logger.LogWarning("Task creation failed: assigned user {AssignedUserId} not found", assignedUserId);
                return Result<Guid>.Failure(Errors.NotFound("User", assignedUserId.ToString()));
            }
        }

        if (isPersonal)
        {
            logger.LogDebug("Creating personal task for user {UserId}", currentUserId);
            task.AssignTo(currentUserId);
        }

        await unitOfWork.UserTaskRepository.AddTaskAsync(task);
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("Task created successfully: {TaskId}, name '{TaskName}', user {UserId}", 
            task.Id, task.Name, currentUserId);

        return Result<Guid>.Success(task.Id);
    }

    public async Task<Result> DeleteAsync(DeleteTaskDto deleteTaskDto, Guid userId)
    {
        logger.LogInformation("Delete task request for task {TaskId} by user {UserId}", 
            deleteTaskDto?.id, userId);

        if (deleteTaskDto is null)
        {
            logger.LogWarning("Delete task failed: request is null");
            return Result.Failure(Errors.Validation("Request cannot be null."));
        }

        if (userId == Guid.Empty)
        {
            logger.LogWarning("Delete task failed: user ID is empty");
            return Result.Failure(Errors.Validation("User ID cannot be empty.", "userId"));
        }

        var user = await unitOfWork.UserRepository.GetUserByIdAsync(userId);
        if (user is null)
        {
            logger.LogWarning("Delete task failed: user {UserId} not found", userId);
            return Result.Failure(Errors.NotFound("User", userId.ToString()));
        }

        await unitOfWork.BeginTransactionAsync();
        try
        {
            var task = await unitOfWork.UserTaskRepository.GetTaskByIdAsync(deleteTaskDto.id);
            if (task is null)
            {
                logger.LogWarning("Delete task failed: task {TaskId} not found", deleteTaskDto.id);
                return Result.Failure(Errors.NotFound("Task", deleteTaskDto.id.ToString()));
            }

            var canDelete = await unitOfWork.UserTaskRepository.CanUserManageTaskAsync(task.Id, userId);
            if (!canDelete)
            {
                logger.LogWarning("Delete task failed: user {UserId} has no permission to delete task {TaskId}", 
                    userId, deleteTaskDto.id);
                return Result.Failure(Errors.Forbidden("You don't have permission to delete this task."));
            }

            await unitOfWork.UserTaskRepository.DeleteTaskAsync(deleteTaskDto.id);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitTransactionAsync();

            logger.LogInformation("Task {TaskId} deleted successfully by user {UserId}", deleteTaskDto.id, userId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting task {TaskId} by user {UserId}", deleteTaskDto.id, userId);
            await unitOfWork.RollbackTransactionAsync();
            return Result.Failure(Errors.Internal("An error occurred while deleting the task."));
        }
    }

    public async Task<Result<IEnumerable<UserTask>>> GetTasksAsync(Guid userId)
    {
        logger.LogDebug("Fetching tasks for user {UserId}", userId);

        if (userId == Guid.Empty)
        {
            logger.LogWarning("Get tasks failed: user ID is empty");
            return Result<IEnumerable<UserTask>>.Failure(Errors.Validation("User ID cannot be empty.", "userId"));
        }

        var tasks = await unitOfWork.UserTaskRepository.GetTasksByUserAsync(userId);
        if (tasks is null || !tasks.Any())
        {
            logger.LogDebug("No tasks found for user {UserId}", userId);
            return Result<IEnumerable<UserTask>>.Failure(Errors.NotFound($"No tasks found for user '{userId}'."));
        }

        logger.LogDebug("Found {Count} tasks for user {UserId}", tasks.Count(), userId);
        return Result<IEnumerable<UserTask>>.Success(tasks);
    }

    public async Task<Result<UserTask>> GetTaskByIdAsync(Guid taskId, Guid userId)
    {
        logger.LogDebug("Fetching task {TaskId} for user {UserId}", taskId, userId);

        var canUserManage = await unitOfWork.UserTaskRepository.CanUserManageTaskAsync(taskId, userId);
        if (!canUserManage)
        {
            logger.LogWarning("User {UserId} has no permission to view task {TaskId}", userId, taskId);
            return Result<UserTask>.Failure(Errors.Forbidden("You don't have permission to view this task."));
        }

        var task = await unitOfWork.UserTaskRepository.GetTaskByIdAsync(taskId);
        if (task is null)
        {
            logger.LogWarning("Task {TaskId} not found", taskId);
            return Result<UserTask>.Failure(Errors.NotFound("Task", taskId.ToString()));
        }

        logger.LogDebug("Task {TaskId} found for user {UserId}", taskId, userId);
        return Result<UserTask>.Success(task);
    }

    public async Task<Result> RenameTaskAsync(RenameTaskDto results, Guid currentUserId)
    {
        logger.LogInformation("Rename task request for task {TaskId} by user {UserId}", 
            results?.id, currentUserId);

        if (results is null)
        {
            logger.LogWarning("Rename task failed: request is null");
            return Result.Failure(Errors.Validation("Request cannot be null."));
        }

        if (results.id == Guid.Empty)
        {
            logger.LogWarning("Rename task failed: task ID is empty");
            return Result.Failure(Errors.Validation("Task ID cannot be empty.", "id"));
        }

        if (string.IsNullOrWhiteSpace(results.Name))
        {
            logger.LogWarning("Rename task failed: new name is empty for task {TaskId}", results.id);
            return Result.Failure(Errors.Validation("Task name cannot be empty.", "name"));
        }

        var task = await unitOfWork.UserTaskRepository.GetTaskByIdAsync(results.id);
        if (task is null)
        {
            logger.LogWarning("Rename task failed: task {TaskId} not found", results.id);
            return Result.Failure(Errors.NotFound("Task", results.id.ToString()));
        }

        var canManage = await unitOfWork.UserTaskRepository.CanUserManageTaskAsync(results.id, currentUserId);
        if (!canManage)
        {
            logger.LogWarning("Rename task failed: user {UserId} has no permission to rename task {TaskId}", 
                currentUserId, results.id);
            return Result.Failure(Errors.Forbidden("You don't have permission to rename this task."));
        }

        var oldName = task.Name;
        task.ChangeName(results.Name);
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("Task {TaskId} renamed from '{OldName}' to '{NewName}' by user {UserId}", 
            results.id, oldName, results.Name, currentUserId);

        return Result.Success();
    }

    public async Task<Result> AssignTaskAsync(AssignTaskDto assignTaskDto, Guid currentUserId)
    {
        logger.LogInformation("Assign task request for task {TaskId} by user {UserId}", 
            assignTaskDto?.id, currentUserId);

        if (assignTaskDto is null)
        {
            logger.LogWarning("Assign task failed: request is null");
            return Result.Failure(Errors.Validation("Request cannot be null."));
        }

        if (currentUserId == Guid.Empty)
        {
            logger.LogWarning("Assign task failed: current user ID is empty");
            return Result.Failure(Errors.Validation("Current user ID cannot be empty.", "userId"));
        }

        if (assignTaskDto.id == Guid.Empty)
        {
            logger.LogWarning("Assign task failed: task ID is empty");
            return Result.Failure(Errors.Validation("Task ID cannot be empty.", "id"));
        }

        if (assignTaskDto.AssigneeToUserId == Guid.Empty)
        {
            logger.LogWarning("Assign task failed: assignee user ID is empty");
            return Result.Failure(Errors.Validation("Assignee user ID cannot be empty.", "assigneeToUserId"));
        }

        var task = await unitOfWork.UserTaskRepository.GetTaskByIdAsync(assignTaskDto.id);
        if (task is null)
        {
            logger.LogWarning("Assign task failed: task {TaskId} not found", assignTaskDto.id);
            return Result.Failure(Errors.NotFound("Task", assignTaskDto.id.ToString()));
        }

        if (!task.TeamID.HasValue)
        {
            logger.LogWarning("Assign task failed: task {TaskId} is not assigned to any team", assignTaskDto.id);
            return Result.Failure(Errors.Validation("This task is not assigned to any team."));
        }

        var teamId = task.TeamID.Value;

        var isAdmin = await unitOfWork.TeamMemberRepository.IsUserAdminAsync(teamId, currentUserId);
        if (!isAdmin)
        {
            logger.LogWarning("Assign task failed: user {UserId} is not admin of team {TeamId}", 
                currentUserId, teamId);
            return Result.Failure(Errors.Forbidden("Only team administrators can reassign tasks."));
        }

        var isUserInTeam = await unitOfWork.TeamMemberRepository.IsUserInTeamAsync(
            teamId, assignTaskDto.AssigneeToUserId);
        if (!isUserInTeam)
        {
            logger.LogWarning("Assign task failed: user {AssigneeUserId} is not in team {TeamId}", 
                assignTaskDto.AssigneeToUserId, teamId);
            return Result.Failure(Errors.Validation(
                $"User '{assignTaskDto.AssigneeToUserId}' is not a member of this team.", "assigneeToUserId"));
        }

        var assigneeUser = await unitOfWork.UserRepository.GetUserByIdAsync(assignTaskDto.AssigneeToUserId);
        if (assigneeUser is null)
        {
            logger.LogWarning("Assign task failed: assignee user {AssigneeUserId} not found", 
                assignTaskDto.AssigneeToUserId);
            return Result.Failure(Errors.NotFound("User", assignTaskDto.AssigneeToUserId.ToString()));
        }

        var oldAssignee = task.AssignedToUserID;
        task.AssignTo(assignTaskDto.AssigneeToUserId);
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("Task {TaskId} reassigned from user {OldAssignee} to user {NewAssignee} by user {UserId}", 
            assignTaskDto.id, oldAssignee?.ToString() ?? "null", assignTaskDto.AssigneeToUserId, currentUserId);

        return Result.Success();
    }
}