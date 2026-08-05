using src.Domain.Entities;

namespace src;

public class UserRepository
{
    private List<User> _users = [];

    public User? GetUserByEmail(string email)
    {
        return _users.FirstOrDefault(user => user.Email.Value == email);
    }

    public void CreateUser(User user)
    {
        _users.Add(user);
    }

    public void DeleteUser(User user)
    {
        _users.Remove(user);
    }

    public bool UserExists(string email)
    {
        return _users.Any(user => user.Email.Value == email);
    }
    
    public IReadOnlyList<User> GetAllUsers() => _users.AsReadOnly();
    
}