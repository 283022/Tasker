using System.Runtime.InteropServices.Marshalling;
using src.Domain;
using src.Domain.Entities;
using src.Domain.ValueObjects;
using src.Mappers;

namespace src;

public class UserService(UserRepository userRepository,MapperUserFromDto mapper)
{
    private readonly UserRepository _userRepository = userRepository;

    public Result Register(CreateUserDto request)
    {
        if (request is null)
            return Result.Failure(
                new Error("Validation",
                    "Request cannot be null"));
        
        var user =  mapper.Map(request);
        if (user.IsFailure) return Result.Failure(user.Error);
        
        if (_userRepository.UserExists(user.Value.Email.Value))
            return Result.Failure(
                new Error("Conflict",
                    $"User with email {request.Email} already exists"));
        
        
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
        var email = Email.Create(request.Email);
        if (email.IsFailure)
        {
            return new Error("Validation", email.Value.Value + " isn't valid");
        }
        var user = _userRepository.GetUserByEmail(email.Value.Value);

        if (user is null)
        {
            var error = new Error("UserNotFounded", email.Value.Value + " not found");
            return error;
        }

        var password = Password.Create(request.Password);
        if (user.Password.Value != password.Value.Value)
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