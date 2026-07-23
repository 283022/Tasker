namespace Authorization2;

public class UserRepository
{
    private List<User> _users = [];

    public User? GetUserByEmail(string email)
    {
        return _users.FirstOrDefault(user => user.Email == email);
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
        return _users.Any(user => user.Email == email);
    }
    
    
}