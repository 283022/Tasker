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

    public  User? GetUserByEmail(string email)
    {
        return  _context.Users
            .FirstOrDefault(user => user.Email.Value == email);
    }

    public void Add(User user)
    {
        _context.Users.Add(user);
    }

    public void Delete(User user)
    {
        _context.Users.Remove(user);
    }

    public bool UserExists(string email)
    {
        return _context.Users
            .Any(user => user.Email.Value == email);
    }
    
    public  IReadOnlyList<User> GetAllUsers()
    {
        return  _context.Users
            .AsNoTracking()
            .ToList();
    }

    public User? GetUserById(Guid id)
    {
        return  _context.Users
            .FirstOrDefault(u => u.Id == id);
    }
}