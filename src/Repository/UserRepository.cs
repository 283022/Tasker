using Microsoft.EntityFrameworkCore;
using src.Domain.Entities;
using src.Db;

namespace src;

public class UserRepository : IUserRepository
{
    private readonly ApplicationContext _context;

    public UserRepository(ApplicationContext context)
    {
        _context = context;
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(user => user.Email.Value == email);
    }

    public async Task AddUserAsync(User user)
    {
        await _context.Users.AddAsync(user);
        // SaveChangesAsync вызывается в UnitOfWork
    }

    public async Task DeleteUserAsync(User user)
    {
        _context.Users.Remove(user);
        // SaveChangesAsync вызывается в UnitOfWork
    }

    public async Task<bool> UserExistsAsync(string email)
    {
        return await _context.Users
            .AnyAsync(user => user.Email.Value == email);
    }
    
    public async Task<IReadOnlyList<User>> GetAllUsersAsync()
    {
        return await _context.Users
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<User?> GetUserByIdAsync(Guid id)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id);
    }
}