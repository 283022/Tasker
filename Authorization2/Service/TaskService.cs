using Authorization2.Domain;
using Authorization2.Domain.Entities;
using Authorization2.Mappers;

namespace Authorization2;

public class TaskService(TaskRepository taskRepository, MapperTaskFromDto mapper)
{
    private readonly TaskRepository _taskRepository = taskRepository;
    private readonly MapperTaskFromDto _mapperTaskFromDto = mapper;
    
    public Result<Guid> Create(CreateTaskDto createTaskDto)
    {
        var results = _mapperTaskFromDto.Map((createTaskDto));
        if (results.IsFailure) return Result<Guid>.Failure(results.Error);
        
        return _taskRepository.Create(results.Value);
    }

    public Result Delete(DeleteTaskDto deleteTaskDto)
    {
        if (deleteTaskDto is null)
            return Result.Failure(new Error("Validation","Request cannot be null"));

        var task = _taskRepository.GetTaskById(deleteTaskDto.id);
        if (task is null)
            return Result.Failure(new Error("NotFounded","Task:" +deleteTaskDto.id.ToString()));

        _taskRepository.Delete(deleteTaskDto.id);
        return Result.Success();
    }

    public Result<UserTask> GetTask(Guid id)
    {
        if (id == Guid.Empty)
            return Result<UserTask>.Failure(new Error("Validation","Task ID cannot be empty"));

        var task = _taskRepository.GetTaskById(id);
        if (task is null)
            return Result<UserTask>.Failure(new Error("NotFounded" ,"Task:" + id.ToString()));

        return Result<UserTask>.Success(task);
    }
    
}