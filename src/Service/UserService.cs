using src.Domain;
using src.Domain.Entities;
using src.Domain.ValueObjects;
using src.UnitOfWork;

namespace src.Service;

public class UserService(IUnitOfWork unitOfWork)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result> RegisterAsync(CreateUserDto request)
    {
        if (request is null)
            return Result.Failure(Errors.Validation("Request cannot be null."));

        var email = Email.Create(request.Email);
        var password = Password.Create(request.Password);

        if (email.IsFailure)
            return Result.Failure(Errors.Validation(email.Error.Message, "email"));
        if (password.IsFailure)
            return Result.Failure(Errors.Validation(password.Error.Message, "password"));

        var user = User.Create(request.Name, email.Value, password.Value);

        if (user.IsFailure)
            return Result.Failure(user.Error);
        if (user.Value is null)
            return Result.Failure(Errors.Internal("User creation failed."));

        if (await _unitOfWork.UserRepository.UserExistsAsync(user.Value.Email.Value))
            return Result.Failure(Errors.Conflict($"User with email '{request.Email}' already exists."));

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
            return Result<User>.Failure(Errors.Validation("Request cannot be null."));

        var email = Email.Create(request.Email);
        if (email.IsFailure)
            return Result<User>.Failure(Errors.Validation("Invalid email format.", "email"));

        var user = await _unitOfWork.UserRepository.GetUserByEmailAsync(email.Value.Value);
        if (user is null)
            return Result<User>.Failure(Errors.NotFound($"User with email '{email.Value.Value}' not found."));

        var password = Password.Create(request.Password);
        if (user.Password.Value != password.Value.Value)
            return Result<User>.Failure(Errors.Unauthorized());

        return Result<User>.Success(user);
    }

    public async Task<Result<Guid>> GetUserIdAsync(LoginUserDto request)
    {
        if (request is null)
            return Result<Guid>.Failure(Errors.Validation("Request cannot be null."));

        var user = await _unitOfWork.UserRepository.GetUserByEmailAsync(request.Email);
        if (user is null)
            return Result<Guid>.Failure(Errors.NotFound($"User with email '{request.Email}' not found."));

        return Result<Guid>.Success(user.Id);
    }

    public async Task<Result<User>> GetUserByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
            return Result<User>.Failure(Errors.Validation("User ID cannot be empty.", "userId"));

        var user = await _unitOfWork.UserRepository.GetUserByIdAsync(id);
        if (user is null)
            return Result<User>.Failure(Errors.NotFound("User", id.ToString()));

        return Result<User>.Success(user);
    }

    public async Task<Result> DeleteUserAsync(DeleteUserDto request, Guid userId)
    {
        if (userId == Guid.Empty)
            return Result.Failure(Errors.Validation("Current user ID cannot be empty.", "userId"));

        if (request is null)
            return Result.Failure(Errors.Validation("Request cannot be null."));

        if (request.id == Guid.Empty)
            return Result.Failure(Errors.Validation("User ID to delete cannot be empty.", "userId"));

        var user = await _unitOfWork.UserRepository.GetUserByIdAsync(request.id);
        if (user is null)
            return Result.Failure(Errors.NotFound("User", request.id.ToString()));

        if (user.Id != userId)
            return Result.Failure(Errors.Forbidden("You can only delete your own account."));

        await _unitOfWork.UserRepository.DeleteUserAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }
}