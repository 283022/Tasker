using Microsoft.EntityFrameworkCore;
using src.Db;
using src.Domain.Entities;

namespace src.Repository;

public class UserRepository(ApplicationContext context, ILogger<UserRepository> logger) : IUserRepository
{
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        logger.LogDebug("Attempting to find user by email {Email}", email);
        
        var user = await context.Users
            .FirstOrDefaultAsync(user => user.Email.Value == email);

        if (user is null)
            logger.LogDebug("User not found with email {Email}", email);
        else
            logger.LogDebug("User found with email {Email}, id {UserId}", email, user.Id);

        return user;
    }

    public async Task AddUserAsync(User user)
    {
        logger.LogDebug("Adding new user with email {Email}", user.Email.Value);
        await context.Users.AddAsync(user);
    }

    public async Task DeleteUserAsync(User user)
    {
        logger.LogDebug("Deleting user {UserId} with email {Email}", user.Id, user.Email.Value);
        context.Users.Remove(user);
    }

    public async Task<bool> UserExistsAsync(string email)
    {
        logger.LogTrace("Checking if user exists with email {Email}", email);
        return await context.Users
            .AnyAsync(user => user.Email.Value == email);
    }
    
    public async Task<IReadOnlyList<User>> GetAllUsersAsync()
    {
        logger.LogTrace("Fetching all users");
        return await context.Users
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<User?> GetUserByIdAsync(Guid id)
    {
        logger.LogDebug("Fetching user by id {UserId}", id);
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == id);
        
        if (user is null)
            logger.LogDebug("User not found with id {UserId}", id);
        
        return user;
    }
    
    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await context.Users
            .FirstOrDefaultAsync(user => user.DisplayName == username);
    }
}