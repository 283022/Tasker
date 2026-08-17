using src.Domain;
using src.Domain.Entities;
using src.Domain.ValueObjects;
using src.UnitOfWork;

namespace src.Service;

public class UserService(
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ILogger<UserService> logger)
{
    public async Task<Result> RegisterAsync(CreateUserDto request)
    {
        logger.LogInformation("Starting user registration for email {Email}", request?.Email);

        if (request is null)
        {
            logger.LogWarning("Registration failed: request is null");
            return Result.Failure(Errors.Validation("Request cannot be null."));
        }

        var email = Email.Create(request.Email);
        var password = Password.Create(request.Password, passwordHasher);

        if (request.Name is null)
            return Result.Failure(Errors.Validation("Name cannot be null.", "name"));

        if (email.IsFailure)
        {
            logger.LogWarning("Registration failed: invalid email format {Email}", request.Email);
            return Result.Failure(Errors.Validation(email.Error.Message, "email"));
        }

        if (password.IsFailure)
        {
            logger.LogWarning("Registration failed: invalid password format for email {Email}", request.Email);
            return Result.Failure(Errors.Validation(password.Error.Message, "password"));
        }

        var userResult = User.Create(request.Name, email.Value, password.Value);

        if (userResult.IsFailure || userResult.Value is null)
        {
            logger.LogError("Registration failed: user creation failed for email {Email}", request.Email);
            return Result.Failure(Errors.Internal("User creation failed."));
        }

        var user = userResult.Value;

        if (await unitOfWork.UserRepository.UserExistsAsync(user.Email.Value))
        {
            logger.LogWarning("Registration failed: user with email {Email} already exists", request.Email);
            return Result.Failure(Errors.Conflict($"User with email '{request.Email}' already exists."));
        }

        await unitOfWork.UserRepository.AddUserAsync(user);
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("User registered successfully: {UserId}, email {Email}",
            user.Id, user.Email.Value);

        return Result.Success();
    }

    public async Task<bool> UserExistsAsync(string email)
    {
        logger.LogTrace("Checking if user exists with email {Email}", email);
        return await unitOfWork.UserRepository.UserExistsAsync(email);
    }

    public async Task<Result<User>> UserLoginAsync(LoginUserDto request)
    {
        logger.LogInformation("Login attempt for email {Email}", request?.Email);

        if (request is null)
        {
            logger.LogWarning("Login failed: request is null");
            return Result<User>.Failure(Errors.Validation("Request cannot be null."));
        }

        var email = Email.Create(request.Email);
        if (email.IsFailure)
        {
            logger.LogWarning("Login failed: invalid email format {Email}", request.Email);
            return Result<User>.Failure(Errors.Validation("Invalid email format.", "email"));
        }

        var user = await unitOfWork.UserRepository.GetUserByEmailAsync(email.Value.Value);
        if (user is null)
        {
            logger.LogWarning("Login failed: user not found with email {Email}", request.Email);
            return Result<User>.Failure(Errors.NotFound($"User with email '{email.Value.Value}' not found."));
        }

        var result = passwordHasher.Verify(request.Password, user.Password.HashPassword);
        
        if (!result)
        {
            logger.LogWarning("Login failed: invalid password for user {UserId} with email {Email}",
                user.Id, request.Email);
            return Result<User>.Failure(Errors.Unauthorized());
        }

        logger.LogInformation("User logged in successfully: {UserId}, email {Email}", user.Id, user.Email.Value);
        return Result<User>.Success(user);
    }

    public async Task<Result<Guid>> GetUserIdAsync(LoginUserDto request)
    {
        logger.LogDebug("Getting user ID for email {Email}", request?.Email);

        if (request is null)
        {
            logger.LogWarning("Get user ID failed: request is null");
            return Result<Guid>.Failure(Errors.Validation("Request cannot be null."));
        }

        var user = await unitOfWork.UserRepository.GetUserByEmailAsync(request.Email);
        if (user is null)
        {
            logger.LogWarning("Get user ID failed: user not found with email {Email}", request.Email);
            return Result<Guid>.Failure(Errors.NotFound($"User with email '{request.Email}' not found."));
        }

        logger.LogDebug("User ID found for email {Email}: {UserId}", request.Email, user.Id);
        return Result<Guid>.Success(user.Id);
    }

    public async Task<Result<User>> GetUserByIdAsync(Guid id)
    {
        logger.LogDebug("Fetching user by id {UserId}", id);

        if (id == Guid.Empty)
        {
            logger.LogWarning("Get user by id failed: user ID is empty");
            return Result<User>.Failure(Errors.Validation("User ID cannot be empty.", "userId"));
        }

        var user = await unitOfWork.UserRepository.GetUserByIdAsync(id);
        if (user is null)
        {
            logger.LogWarning("User not found with id {UserId}", id);
            return Result<User>.Failure(Errors.NotFound("User", id.ToString()));
        }

        logger.LogDebug("User found with id {UserId}, email {Email}", user.Id, user.Email.Value);
        return Result<User>.Success(user);
    }

    public async Task<Result> DeleteUserAsync(DeleteUserDto request, Guid userId)
    {
        logger.LogInformation("Delete user request for user {TargetUserId} by user {CurrentUserId}",
            request?.id, userId);

        if (userId == Guid.Empty)
        {
            logger.LogWarning("Delete user failed: current user ID is empty");
            return Result.Failure(Errors.Validation("Current user ID cannot be empty.", "userId"));
        }

        if (request is null)
        {
            logger.LogWarning("Delete user failed: request is null");
            return Result.Failure(Errors.Validation("Request cannot be null."));
        }

        if (request.id == Guid.Empty)
        {
            logger.LogWarning("Delete user failed: target user ID is empty");
            return Result.Failure(Errors.Validation("User ID to delete cannot be empty.", "userId"));
        }

        var user = await unitOfWork.UserRepository.GetUserByIdAsync(request.id);
        if (user is null)
        {
            logger.LogWarning("Delete user failed: target user {TargetUserId} not found", request.id);
            return Result.Failure(Errors.NotFound("User", request.id.ToString()));
        }

        if (user.Id != userId)
        {
            logger.LogWarning(
                "Delete user failed: user {CurrentUserId} attempted to delete another user {TargetUserId}",
                userId, request.id);
            return Result.Failure(Errors.Forbidden("You can only delete your own account."));
        }

        await unitOfWork.UserRepository.DeleteUserAsync(user);
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("User {UserId} deleted successfully", userId);
        return Result.Success();
    }
}