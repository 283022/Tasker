using src.Domain;
using src.Domain.Entities;

namespace src.Mappers;

public class MapperTaskFromDto: IMapper<CreateTaskDto, UserTask>
{

    public Result<UserTask> Map(CreateTaskDto source)
    {
        return source is null ? 
            Result<UserTask>.Failure(new Error("Validation", "Request cannot be null")) : 
            UserTask.Create(source.Name, source.userid);
    }
}