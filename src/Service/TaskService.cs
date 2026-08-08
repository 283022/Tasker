using src.Domain;
using src.Domain.Entities;
using src.Mappers;
using src.UnitOfWork;

namespace src;

public class TaskService(IUnitOfWork unitOfWork, MapperTaskFromDto mapper)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly MapperTaskFromDto _mapperTaskFromDto = mapper;
    
    public Result<Guid> Create(CreateTaskDto createTaskDto)
    {
        if (createTaskDto is null)
            return Result<Guid>.Failure(new Error("Validation", "Request cannot be null"));
        
        var results = _mapperTaskFromDto.Map((createTaskDto));
        if (results.IsFailure) return Result<Guid>.Failure(results.Error);
        
        _unitOfWork.UserTaskRepository.Add(results.Value);
        _unitOfWork.SaveChanges();
        
        return results.Value.Id;
    }

    public Result Delete(DeleteTaskDto deleteTaskDto)
    {
        if (deleteTaskDto is null)
            return Result.Failure(new Error("Validation","Request cannot be null"));
        _unitOfWork.BeginTransaction();

        try
        {
            var task = _unitOfWork.UserTaskRepository.GetTaskById(deleteTaskDto.id);
            if (task is null)
                return Result.Failure(new Error("NotFounded","Task ID isn't correct"));
            
            _unitOfWork.UserTaskRepository.Delete(deleteTaskDto.id);
            _unitOfWork.SaveChanges();
            _unitOfWork.Commit();
            return Result.Success();
        }
        catch (Exception e)
        {
            _unitOfWork.Rollback();
            return Result.Failure(new Error("Server Error", "Error occured" + e.Message ));
        }
        
        
    }

    public Result<UserTask> GetTask(Guid id)
    {
        if (id == Guid.Empty)
            return Result<UserTask>.Failure(new Error("Validation","Task ID cannot be empty"));

        var task = _unitOfWork.UserTaskRepository.GetTaskById(id);
        if (task is null)
            return Result<UserTask>.Failure(new Error("NotFounded" ,"Task:" + id.ToString()));

        return Result<UserTask>.Success(task);
    }
    
}