using src.Domain;
using src.Domain.Entities;
using src.Domain.ValueObjects;

namespace src.Mappers;

public class MapperUserFromDto : IMapper<CreateUserDto,User>
{
    public Result<User> Map(CreateUserDto source)
    {
        var email = Email.Create(source.Email);
        var password = Password.Create(source.Password);

        return email.IsFailure ? Result<User>.Failure(email.Error) :
            password.IsFailure ? Result<User>.Failure(password.Error) :
            User.Create(source.Name, email.Value, password.Value);
    }
}