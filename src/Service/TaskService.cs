using Microsoft.AspNetCore.Mvc;
using src.Domain;
using src.Domain.Entities;
using src.UnitOfWork;

namespace src;

public class TaskService(IUnitOfWork unitOfWork)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public Result<Guid> Create(CreateTaskDto createTaskDto, Guid currentUserId)
    {
        // 1. Валидация DTO
        if (createTaskDto is null)
            return Result<Guid>.Failure(new Error("Validation", "Request cannot be null"));

        // 2. Маппинг
        var results = UserTask.Create(
            createTaskDto.Name,
            currentUserId,
            createTaskDto.teamId,
            createTaskDto.assignedToUserId);
        
        if (results.IsFailure)
            return Result<Guid>.Failure(results.Error);

        var task = results.Value;

        // 3. Проверяем, кто создает задачу
        if (task.CreateByUserID != currentUserId)
            return Result<Guid>.Failure(new Error("Authorization", "Cannot create task for another user"));

        // 4. Определяем сценарий: личная задача или командная
        bool isPersonal = task.AssignedToUserID == null && task.TeamID == null;
        bool isTeamTask = task.AssignedToUserID != null && task.TeamID != null;
        bool isInvalid = (task.AssignedToUserID == null && task.TeamID != null) ||
                         (task.AssignedToUserID != null && task.TeamID == null);

        // 5. Если комбинация невалидна
        if (isInvalid)
            return Result<Guid>.Failure(new Error("Validation",
                "Both AssignedToUserID and TeamID must be provided or both must be null"));

        // 6. Если задача командная — проверяем права
        if (isTeamTask)
        {
            var teamId = task.TeamID.Value;
            var assignedUserId = task.AssignedToUserID.Value;

            // Проверяем, что команда существует
            var team = _unitOfWork.TeamRepository.GetTeamById(teamId);
            if (team is null)
                return Result<Guid>.Failure(new Error("NotFound", "Team not found"));

            // Проверяем, что текущий пользователь — админ команды
            var isAdmin = _unitOfWork.TeamRepository.IsUserAdminOfTeam(currentUserId, teamId);
            if (!isAdmin)
                return Result<Guid>.Failure(new Error("Authorization",
                    "Only team admin can create tasks in this team"));

            // Проверяем, что пользователь, которому назначают, состоит в команде
            var isUserInTeam = _unitOfWork.TeamRepository.IsUserInTeam(assignedUserId, teamId);
            if (!isUserInTeam)
                return Result<Guid>.Failure(new Error("Validation", "User is not a member of this team"));

            // Проверяем, что пользователь, которому назначают, существует
            var assignedUser = _unitOfWork.UserRepository.GetUserById(assignedUserId);
            if (assignedUser is null)
                return Result<Guid>.Failure(new Error("NotFound", "Assigned user not found"));
        }

        // 7. Если задача личная — проверяем, что пользователь не пытается ее кому-то назначить
        if (isPersonal)
        {
            // Личная задача — назначается только себе
            task.AssignTo(currentUserId);
        }

        // 8. Добавляем задачу (единственное место)
        _unitOfWork.UserTaskRepository.Add(task);
        _unitOfWork.SaveChanges();

        return Result<Guid>.Success(task.Id);
    }

    public Result Delete(DeleteTaskDto deleteTaskDto, Guid userid)
    {
        if (deleteTaskDto is null)
            return Result.Failure(new Error("Validation", "Request cannot be null"));
        

        if (userid == Guid.Empty)
            return new Error("Validation", "Id cannot be empty");
        _unitOfWork.BeginTransaction();
        try
        {
            var task = _unitOfWork.UserTaskRepository.GetTaskById(deleteTaskDto.id);
            if (task is null)
                return Result.Failure(new Error("NotFounded", "Task ID isn't correct"));
            var canDelete = _unitOfWork.UserTaskRepository.CanUserManageTask(task.Id, userid); 
            if (!canDelete)
                return new Error("Authorization", "Can't delete task");
            
            _unitOfWork.UserTaskRepository.Delete(deleteTaskDto.id);
            _unitOfWork.SaveChanges();
            _unitOfWork.Commit();
            return Result.Success();
        }
        catch (Exception e)
        {
            _unitOfWork.Rollback();
            return Result.Failure(new Error("Server Error", "Error occured" + e.Message));
        }
    }

    public Result<IEnumerable<UserTask>> GetTasks(Guid id)
    {
        if (id == Guid.Empty)
            return new Error("Validation", "Task ID cannot be empty");

        var task = _unitOfWork.UserTaskRepository.GetTasksByUser(id);
        if (task is null)
            return new Error("NotFounded", "Task:" + id.ToString());

        return Result<IEnumerable<UserTask>>.Success(task);
    }

    public Result<UserTask> GetTaskById(Guid taskId, Guid userId)
    {
       var canUserManage =  _unitOfWork.UserTaskRepository.CanUserManageTask(taskId, userId);
       if(!canUserManage)
           return new Error("Validation", "Can't manage Task");
       var task = _unitOfWork.UserTaskRepository.GetTaskById(taskId);
       if (task is null)
           return new Error("Validation", "Task not founded");
       return task;
    }
    
    public Result RenameTask(RenameTaskDto results, Guid currentUserId)
    {
        // 1. Валидация DTO
        if (results is null)
            return Result.Failure(new Error("Validation", "Request cannot be null"));

        // 2. Валидация ID
        if (results.id == Guid.Empty)
            return Result.Failure(new Error("Validation", "Task ID cannot be empty"));

        // 3. Валидация имени
        if (string.IsNullOrWhiteSpace(results.Name))
            return Result.Failure(new Error("Validation", "Name cannot be empty"));

        // 4. Получаем задачу
        var task = _unitOfWork.UserTaskRepository.GetTaskById(results.id);
        if (task is null)
            return Result.Failure(new Error("NotFound", $"Task '{results.id}' not found"));

        // 5. Проверка прав (может ли пользователь изменять эту задачу)
        if (!_unitOfWork.UserTaskRepository.CanUserManageTask(results.id, currentUserId))
            return Result.Failure(new Error("Forbidden", "You don't have permission to rename this task"));

        // 6. Меняем имя (используем метод сущности, если есть)
        task.ChangeName(results.Name); // Или task.Name = results.NewName;

        // 7. Сохраняем
        _unitOfWork.SaveChanges();

        return Result.Success();
    }

    public Result AssignTask(AssignTaskDto assignTaskDto, Guid currentUserId)
    {
        // 1. Валидация DTO
        if (assignTaskDto is null)
            return Result.Failure(new Error("Validation", "Request cannot be null"));

        // 2. Валидация ID текущего пользователя
        if (currentUserId == Guid.Empty)
            return Result.Failure(new Error("Validation", "Current user ID cannot be empty"));

        // 3. Валидация ID задачи
        if (assignTaskDto.id == Guid.Empty)
            return Result.Failure(new Error("Validation", "Task ID cannot be empty"));

        // 4. Валидация ID пользователя, которому назначают
        if (assignTaskDto.AssigneeToUserId == Guid.Empty)
            return Result.Failure(new Error("Validation", "Assignee user ID cannot be empty"));

        // 5. Получаем задачу
        var task = _unitOfWork.UserTaskRepository.GetTaskById(assignTaskDto.id);
        if (task is null)
            return Result.Failure(new Error("NotFound", $"Task '{assignTaskDto.id}' not found"));

        // 6. Проверяем, что задача принадлежит команде
        if (!task.TeamID.HasValue)
            return Result.Failure(new Error("Validation", "Task is not assigned to any team"));

        var teamId = task.TeamID.Value;

        // 7. Проверяем, что текущий пользователь — админ команды
        var isAdmin = _unitOfWork.TeamMemberRepository.IsUserAdmin(teamId, currentUserId);
        if (!isAdmin)
            return Result.Failure(new Error("Authorization", "Only team admins can reassign tasks"));

        // 8. Проверяем, что пользователь, которому назначают, состоит в команде
        var isUserInTeam = _unitOfWork.TeamMemberRepository.IsUserInTeam(teamId, assignTaskDto.AssigneeToUserId);
        if (!isUserInTeam)
            return Result.Failure(new Error("Validation", "User is not a member of this team"));

        // 9. Проверяем, что пользователь, которому назначают, существует
        var assigneeUser = _unitOfWork.UserRepository.GetUserById(assignTaskDto.AssigneeToUserId);
        if (assigneeUser is null)
            return Result.Failure(new Error("NotFound", "Assignee user not found"));

        // 10. Назначаем задачу
        task.AssignTo(assignTaskDto.AssigneeToUserId);

        // 11. Сохраняем
        _unitOfWork.SaveChanges();

        return Result.Success();
    }
}