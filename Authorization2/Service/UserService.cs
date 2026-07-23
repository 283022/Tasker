using System.Data;
using Authorization2.Mappers;

namespace Authorization2;

public class UserService(UserRepository userRepository,MapperUserFromDto mapper)
{
    private readonly UserRepository _userRepository = userRepository;

    public void Register(CreateUserDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Name is null || 
            _userRepository.UserExists(request.Email))
        {
            throw new DataException("This data is not available");
        }

        _userRepository.CreateUser(mapper.Map(request));
    }

    private bool UserExists(string email)
    {
        return _userRepository.UserExists(email);
    }

    public bool Userlogin(LoginUserDto request)
    {
        var user = _userRepository.GetUserByEmail(request.Email);
        if (user is null) throw new UnauthorizedAccessException("user is not found");
        return user.Password == request.Password;
    }

    public Guid GetUseId(LoginUserDto request)
    {
        var user = _userRepository.GetUserByEmail(request.Email);
        if (user is null) throw new UnauthorizedAccessException("user is not found");
        return user.ID;
    }
    
}