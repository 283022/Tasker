using src.Domain.Entities;

namespace src;

public interface IUserRepository
{
    Task<User?> GetUserByEmailAsync(string email);
    Task AddUserAsync(User user);
    Task DeleteUserAsync(User user);
    Task<bool> UserExistsAsync(string email);
    Task<IReadOnlyList<User>> GetAllUsersAsync();
    Task<User?> GetUserByIdAsync(Guid id);
}