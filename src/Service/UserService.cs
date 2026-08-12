using src.Domain;
using src.Domain.Entities;
using src.Domain.ValueObjects;
using src.UnitOfWork;

namespace src;

public class UserService(IUnitOfWork unitOfWork)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result> RegisterAsync(CreateUserDto request)
    {
        if (request is null)
            return Result.Failure(
                new Error("Validation",
                    "Request cannot be null"));
        
        var email = Email.Create(request.Email);
        var password = Password.Create(request.Password);

        if (email.IsFailure)
            return Result.Failure(new Error(email.Error.Errortype, email.Error.Message));
        if (password.IsFailure)
            return Result.Failure(new Error(password.Error.Errortype, password.Error.Message));
        
        var user = User.Create(request.Name, email.Value, password.Value);
        
        if (user.IsFailure) 
            return Result.Failure(user.Error);
        if (user.Value is null) 
            return Result.Failure(new Error("User isn't exist", "User doesn't exist"));
        
        // Проверяем, существует ли пользователь с таким email (асинхронно)
        if (await _unitOfWork.UserRepository.UserExistsAsync(user.Value.Email.Value))
            return Result.Failure(
                new Error("Conflict",
                    $"User with email {request.Email} already exists"));
        
        await _unitOfWork.UserRepository.AddUserAsync(user.Value);
        await _unitOfWork.SaveChangesAsync();
        
        return Result.Success();
    }

    public async Task<bool> UserExistsAsync(string email)
    {
        return await _unitOfWork.UserRepository.UserExistsAsync(email);
    }

    public async Task<Result<User>> UserLoginAsync(LoginUserDto request)
    {
        if (request is null)
            return Result<User>.Failure(new Error("Validation", "Request cannot be null"));
        
        var email = Email.Create(request.Email);
        if (email.IsFailure)
        {
            return Result<User>.Failure(new Error("Validation", email.Error.Message + " isn't valid"));
        }
        
        var user = await _unitOfWork.UserRepository.GetUserByEmailAsync(email.Value.Value);
        if (user is null)
        {
            return Result<User>.Failure(new Error("UserNotFounded", email.Value.Value + " not found"));
        }

        var password = Password.Create(request.Password);
        if (user.Password.Value != password.Value.Value)
            return Result<User>.Failure(new Error("Unauthorized", "Invalid credentials"));

        return Result<User>.Success(user);
    }

    public async Task<Result<Guid>> GetUserIdAsync(LoginUserDto request)
    {
        if (request is null)
            return Result<Guid>.Failure(new Error("Validation", "Request cannot be null"));

        var user = await _unitOfWork.UserRepository.GetUserByEmailAsync(request.Email);
        if (user is null)
            return Result<Guid>.Failure(new Error("NotFound", request.Email + " not found"));

        return Result<Guid>.Success(user.Id);
    }

    public async Task<Result<User>> GetUserByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
            return Result<User>.Failure(new Error("Validation", "Id cannot be empty"));
        
        var user = await _unitOfWork.UserRepository.GetUserByIdAsync(id);
        if (user is null)
            return Result<User>.Failure(new Error("Validation", "User not found"));
        
        return Result<User>.Success(user);
    }

    public async Task<Result> DeleteUserAsync(DeleteUserDto request, Guid userId)
    {
        if (userId == Guid.Empty)
            return Result.Failure(new Error("Validation", "Current user Id cannot be empty"));
        
        if (request is null)
            return Result.Failure(new Error("Validation", "Request cannot be null"));
        
        if (request.id == Guid.Empty)
            return Result.Failure(new Error("Validation", "User Id cannot be empty"));
        
        var user = await _unitOfWork.UserRepository.GetUserByIdAsync(request.id);
        if (user is null)
            return Result.Failure(new Error("Validation", "User not found"));
        
        if (user.Id != userId)
            return Result.Failure(new Error("Authorization", "You cannot delete this user"));
        
        await _unitOfWork.UserRepository.DeleteUserAsync(user);
        await _unitOfWork.SaveChangesAsync();
        
        return Result.Success();
    }
}