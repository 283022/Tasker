using Authorization2.Mappers;

namespace Authorization2;

public class TaskService(TaskRepository taskRepository, MapperTaskFromDto mapper)
{
    private readonly TaskRepository _taskRepository = taskRepository;
    private readonly MapperTaskFromDto _mapperTaskFromDto = mapper;
    
    public Guid Create(CreateTaskDto createTaskDto)
    {
        return _taskRepository.Create(_mapperTaskFromDto.Map((createTaskDto)));
    }

    public void Delete(DeleteTaskDto deleteTaskDto)
    {
        _taskRepository.Delete(deleteTaskDto.id);
    }

    public UserTask GetTask(Guid id)
    { 
        return _taskRepository.GetTaskById(id);
    }
    
}