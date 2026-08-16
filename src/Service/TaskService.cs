using src.Domain;
using src.Domain.Entities;
using src.Security;
using src.UnitOfWork;

namespace src.Service;

public class TaskService(IUnitOfWork unitOfWork)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result<Guid>> CreateAsync(CreateTaskDto createTaskDto, Guid currentUserId)
    {
        if (createTaskDto is null)
            return Result<Guid>.Failure(Errors.Validation("Request cannot be null."));

        var results = UserTask.Create(
            createTaskDto.Name,
            currentUserId,
            createTaskDto.teamId,
            createTaskDto.assignedToUserId);

        if (results.IsFailure)
            return Result<Guid>.Failure(results.Error);

        var task = results.Value;

        if (task.CreateByUserID != currentUserId)
            return Result<Guid>.Failure(Errors.Forbidden("Cannot create task for another user."));

        bool isPersonal = task.AssignedToUserID == null && task.TeamID == null;
        bool isTeamTask = task.AssignedToUserID != null && task.TeamID != null;
        bool isInvalid = (task.AssignedToUserID == null && task.TeamID != null) ||
                         (task.AssignedToUserID != null && task.TeamID == null);

        if (isInvalid)
            return Result<Guid>.Failure(Errors.Validation(
                "Both AssignedToUserID and TeamID must be provided or both must be null."));

        if (isTeamTask)
        {
            var teamId = task.TeamID.Value;
            var assignedUserId = task.AssignedToUserID.Value;

            var team = await _unitOfWork.TeamRepository.GetTeamByIdAsync(teamId);
            if (team is null)
                return Result<Guid>.Failure(Errors.NotFound("Team", teamId.ToString()));

            var isAdmin = await _unitOfWork.TeamRepository.IsUserAdminOfTeamAsync(currentUserId, teamId);
            if (!isAdmin)
                return Result<Guid>.Failure(Errors.Forbidden(
                    "Only team administrators can create tasks in this team."));

            var isUserInTeam = await _unitOfWork.TeamRepository.IsUserInTeamAsync(assignedUserId, teamId);
            if (!isUserInTeam)
                return Result<Guid>.Failure(Errors.Validation(
                    $"User '{assignedUserId}' is not a member of this team.", "assignedToUserId"));

            var assignedUser = await _unitOfWork.UserRepository.GetUserByIdAsync(assignedUserId);
            if (assignedUser is null)
                return Result<Guid>.Failure(Errors.NotFound("User", assignedUserId.ToString()));
        }

        if (isPersonal)
        {
            task.AssignTo(currentUserId);
        }

        await _unitOfWork.UserTaskRepository.AddTaskAsync(task);
        await _unitOfWork.SaveChangesAsync();

        return Result<Guid>.Success(task.Id);
    }

    public async Task<Result> DeleteAsync(DeleteTaskDto deleteTaskDto, Guid userid)
    {
        if (deleteTaskDto is null)
            return Result.Failure(Errors.Validation("Request cannot be null."));

        if (userid == Guid.Empty)
            return Result.Failure(Errors.Validation("User ID cannot be empty.", "userId"));

        var user = await _unitOfWork.UserRepository.GetUserByIdAsync(userid);
        if (user is null)
            return Result.Failure(Errors.NotFound("User", userid.ToString()));

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var task = await _unitOfWork.UserTaskRepository.GetTaskByIdAsync(deleteTaskDto.id);
            if (task is null)
                return Result.Failure(Errors.NotFound("Task", deleteTaskDto.id.ToString()));

            var canDelete = await _unitOfWork.UserTaskRepository.CanUserManageTaskAsync(task.Id, userid);
            if (!canDelete)
                return Result.Failure(Errors.Forbidden("You don't have permission to delete this task."));

            await _unitOfWork.UserTaskRepository.DeleteTaskAsync(deleteTaskDto.id);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            return Result.Success();
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure(Errors.Internal("An error occurred while deleting the task."));
        }
    }

    public async Task<Result<IEnumerable<UserTask>>> GetTasksAsync(Guid id)
    {
        if (id == Guid.Empty)
            return Result<IEnumerable<UserTask>>.Failure(Errors.Validation("User ID cannot be empty.", "userId"));

        var tasks = await _unitOfWork.UserTaskRepository.GetTasksByUserAsync(id);
        if (tasks is null || !tasks.Any())
            return Result<IEnumerable<UserTask>>.Failure(Errors.NotFound($"No tasks found for user '{id}'."));

        return Result<IEnumerable<UserTask>>.Success(tasks);
    }

    public async Task<Result<UserTask>> GetTaskByIdAsync(Guid taskId, Guid userId)
    {
        var canUserManage = await _unitOfWork.UserTaskRepository.CanUserManageTaskAsync(taskId, userId);
        if (!canUserManage)
            return Result<UserTask>.Failure(Errors.Forbidden("You don't have permission to view this task."));

        var task = await _unitOfWork.UserTaskRepository.GetTaskByIdAsync(taskId);
        if (task is null)
            return Result<UserTask>.Failure(Errors.NotFound("Task", taskId.ToString()));

        return Result<UserTask>.Success(task);
    }

    public async Task<Result> RenameTaskAsync(RenameTaskDto results, Guid currentUserId)
    {
        if (results is null)
            return Result.Failure(Errors.Validation("Request cannot be null."));

        if (results.id == Guid.Empty)
            return Result.Failure(Errors.Validation("Task ID cannot be empty.", "id"));

        if (string.IsNullOrWhiteSpace(results.Name))
            return Result.Failure(Errors.Validation("Task name cannot be empty.", "name"));

        var task = await _unitOfWork.UserTaskRepository.GetTaskByIdAsync(results.id);
        if (task is null)
            return Result.Failure(Errors.NotFound("Task", results.id.ToString()));

        var canManage = await _unitOfWork.UserTaskRepository.CanUserManageTaskAsync(results.id, currentUserId);
        if (!canManage)
            return Result.Failure(Errors.Forbidden("You don't have permission to rename this task."));

        task.ChangeName(results.Name);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> AssignTaskAsync(AssignTaskDto assignTaskDto, Guid currentUserId)
    {
        if (assignTaskDto is null)
            return Result.Failure(Errors.Validation("Request cannot be null."));

        if (currentUserId == Guid.Empty)
            return Result.Failure(Errors.Validation("Current user ID cannot be empty.", "userId"));

        if (assignTaskDto.id == Guid.Empty)
            return Result.Failure(Errors.Validation("Task ID cannot be empty.", "id"));

        if (assignTaskDto.AssigneeToUserId == Guid.Empty)
            return Result.Failure(Errors.Validation("Assignee user ID cannot be empty.", "assigneeToUserId"));

        var task = await _unitOfWork.UserTaskRepository.GetTaskByIdAsync(assignTaskDto.id);
        if (task is null)
            return Result.Failure(Errors.NotFound("Task", assignTaskDto.id.ToString()));

        if (!task.TeamID.HasValue)
            return Result.Failure(Errors.Validation("This task is not assigned to any team."));

        var teamId = task.TeamID.Value;

        var isAdmin = await _unitOfWork.TeamMemberRepository.IsUserAdminAsync(teamId, currentUserId);
        if (!isAdmin)
            return Result.Failure(Errors.Forbidden("Only team administrators can reassign tasks."));

        var isUserInTeam =
            await _unitOfWork.TeamMemberRepository.IsUserInTeamAsync(teamId, assignTaskDto.AssigneeToUserId);
        if (!isUserInTeam)
            return Result.Failure(Errors.Validation(
                $"User '{assignTaskDto.AssigneeToUserId}' is not a member of this team.", "assigneeToUserId"));

        var assigneeUser = await _unitOfWork.UserRepository.GetUserByIdAsync(assignTaskDto.AssigneeToUserId);
        if (assigneeUser is null)
            return Result.Failure(Errors.NotFound("User", assignTaskDto.AssigneeToUserId.ToString()));

        task.AssignTo(assignTaskDto.AssigneeToUserId);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }
}