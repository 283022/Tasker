using System.Data;
using src.Domain;
using src.Domain.Entities;
using src.Mappers;

namespace src;

public class UserService(UserRepository userRepository,MapperUserFromDto mapper)
{
    private readonly UserRepository _userRepository = userRepository;

    public Result Register(CreateUserDto request)
    {
        if (request is null)
            return Result.Failure(new Error("Validation","Request cannot be null"));
        if (_userRepository.UserExists(request.Email))
            return Result.Failure(new Error("Conflict",$"User with email {request.Email} already exists"));
        var user =  mapper.Map(request);
        if (user.IsFailure) return Result.Failure(user.Error);
        _userRepository.CreateUser(user.Value);
        
        return Result.Success();
        
    }

    private bool UserExists(string email)
    {
        return _userRepository.UserExists(email);
    }

    public Result<User> Userlogin(LoginUserDto request)
    {
        if (request is null)
            return Result<User>.Failure(new Error("Validation","Request cannot be null"));

        var user = _userRepository.GetUserByEmail(request.Email);
        if (user is null)
            return Result<User>.Failure(new Error("UserNotFounded", request.Email + "not found"));

        if (user.Password.Value != request.Password)
            return Result<User>.Failure(new Error("Unauthorized","Invalid credentials"));

        return Result<User>.Success(user);
    }

    public Result<Guid> GetUserId(LoginUserDto request)
    {
        if (request is null)
            return Result<Guid>.Failure(new Error("Validation","Request cannot be null"));

        var user = _userRepository.GetUserByEmail(request.Email);
        if (user is null)
            return Result<Guid>.Failure(new Error("NotFound", request.Email + "not found"));

        return Result<Guid>.Success(user.ID);
    }
    
}