namespace Authorization2.Mappers;

public class MapperTaskFromDto: IMapper<CreateTaskDto, UserTask>
{
    public UserTask Map(CreateTaskDto source)
    {
        return new UserTask(source.Name, source.userid);
    }
}