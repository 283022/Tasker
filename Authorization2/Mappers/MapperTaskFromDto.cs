using Authorization2.Domain;
using Authorization2.Domain.Entities;

namespace Authorization2.Mappers;

public class MapperTaskFromDto: IMapper<CreateTaskDto, UserTask>
{

    public Result<UserTask> Map(CreateTaskDto source)
    {
        return UserTask.Create(source.Name, source.userid);
    }
}