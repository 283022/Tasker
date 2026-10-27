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
        logger.LogInformation("Starting user registration for email {Email}", request.Email);

        if (request.DisplayName != null)
        {
            var indexResult = await unitOfWork.UserRepository.SearchByDisplayNameAsync(request.DisplayName);
        
            if (indexResult is not null)
                return Result.Failure(Errors.Conflict("User with this display name already exists."));
        }


        var email = Email.Create(request.Email);
        var password = Password.Create(request.Password, passwordHasher);

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

        var userResult = User.Create(request.Name, email.Value, password.Value, request.DisplayName, request.Visible);

        if (userResult.IsFailure)
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

   
    

    public async Task<Result<User>> UserLoginAsync(LoginUserDto request)
    {
        logger.LogInformation("Login attempt for email {Email}", request.Email);

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
        logger.LogDebug("Getting user ID for email {Email}", request.Email);

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

    
}