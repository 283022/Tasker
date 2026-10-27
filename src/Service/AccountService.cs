using src.Domain;
using src.Domain.ValueObjects;
using src.UnitOfWork;

namespace src.Service;

public class AccountService(
    IUnitOfWork unitOfWork,
    ILogger<AccountService> logger,
    IPasswordHasher passwordHasher)
{
    public async Task<Result<AccountDto>> GetProfileAsync(
        Guid userId)
    {
        if (userId == Guid.Empty)
            return new Error("Validation", "Invalid user id");

        var user =
            await unitOfWork.UserRepository.GetUserByIdAsync(userId);

        if (user is null)
            return new Error("NotFound", "User not found");

        return new AccountDto(
            user.Name,
            user.DisplayName,
            user.Email.Value,
            user.Visible);
    }

    public async Task<Result> ChangeDisplayNameAsync(
        Guid userId,
        string displayName)
    {
        if (userId == Guid.Empty)
            return Errors.Validation("Invalid user id");
        
        displayName = displayName.Trim();
        if (string.IsNullOrWhiteSpace(displayName))
            return Errors.Validation(
                "Display name cannot be empty");

        var indexResult = await unitOfWork.UserRepository.SearchByDisplayNameAsync(displayName);
        if (indexResult is not null)
            return Errors.Conflict("User with Display name already exists");
        
        var user =
            await unitOfWork.UserRepository.GetUserByIdAsync(userId);

        if (user is null)
            return Errors.NotFound("User not found");

        var changeResult = user.ChangeDisplayName(displayName);

        if (changeResult.IsFailure)
            return changeResult;

        await unitOfWork.SaveChangesAsync();

        logger.LogInformation(
            "User {UserId} changed their display name",
            userId);

        return Result.Success();
    }

    public async Task<Result> ChangeVisibilityAsync(
        Guid userId,
        bool visible)
    {
        if (userId == Guid.Empty)
            return new Error("Validation", "Invalid user id");

        var user =
            await unitOfWork.UserRepository.GetUserByIdAsync(userId);

        if (user is null)
            return new Error("NotFound", "User not found");

        user.ChangeVisible(visible);
        
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation(
            "User {UserId} changed profile visibility",
            userId);

        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(ChangePasswordDto dto, Guid userId)
    {
        if (string.IsNullOrEmpty(dto.CurrentPassword) || string.IsNullOrEmpty(dto.NewPassword))
            return Errors.Validation("Data cannot be null");
        
        var user = await unitOfWork.UserRepository.GetUserByIdAsync(userId);

        if (user is null)
            return Errors.NotFound("User not founded");

        if (!passwordHasher.Verify(dto.CurrentPassword, user.Password.HashPassword))
            return Errors.Validation("Current Password is uncorrected");

        var newPasswd = Password.Create(dto.NewPassword, passwordHasher);

        if (newPasswd.IsFailure)
            return newPasswd.Error;

        user.ChangePassword(newPasswd.Value);
        await unitOfWork.SaveChangesAsync();
        return  Result.Success();
    }

    public async Task<Result> ChangeEmailAsync(ChangeEmailDto dto, Guid userId)
    {
        if (string.IsNullOrEmpty(dto.NewEmail))
            return Errors.Validation("Data cannot be null");
        
        var user = await unitOfWork.UserRepository.GetUserByIdAsync(userId);

        if (user is null)
            return Errors.NotFound("User not founded");

        var newEmail = Email.Create(dto.NewEmail);
        if (newEmail.IsFailure)
            return newEmail.Error;
        
        user.ChangeEmail(newEmail.Value);
        
        await unitOfWork.SaveChangesAsync();
        return  Result.Success();
    }

    public async Task<Result> ChangeUserNameAsync(ChangeUserNameDto dto, Guid userId)
    {
        if (string.IsNullOrEmpty(dto.NewUserName))
            return Errors.Validation("Data cannot be null");
        
        var user = await unitOfWork.UserRepository.GetUserByIdAsync(userId);

        if (user is null)
            return Errors.NotFound("User not founded");

        user.ChangeName(dto.NewUserName);
        
        await unitOfWork.SaveChangesAsync();
        return  Result.Success();

    }
    
    public async Task<Result> DeleteUserAsync(DeleteUserDto request, Guid userId)
    {
        logger.LogInformation("Delete user request for user {TargetUserId} by user {CurrentUserId}",
            request.id, userId);

        if (userId == Guid.Empty)
        {
            logger.LogWarning("Delete user failed: current user ID is empty");
            return Result.Failure(Errors.Validation("Current user ID cannot be empty.", "userId"));
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