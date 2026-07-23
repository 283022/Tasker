namespace Authorization2.Mappers;

public class MapperUserFromDto : IMapper<CreateUserDto,User>
{
    public User Map(CreateUserDto source)
    {
        return new User(source.Name, source.Email, source.Password);
        
    }
}