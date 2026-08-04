using src.Domain;
using src.Domain.Entities;

namespace src.Mappers;

public class MapperTaskFromDto: IMapper<CreateTaskDto, UserTask>
{

    public Result<UserTask> Map(CreateTaskDto source)
    {
        return UserTask.Create(source.Name, source.userid);
    }
}