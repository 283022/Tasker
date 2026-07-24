using Authorization2.Domain;
using Authorization2.Domain.Entities;
using Authorization2.Domain.ValueObjects;

namespace Authorization2.Mappers;

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