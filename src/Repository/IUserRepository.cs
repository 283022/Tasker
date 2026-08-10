using src.Domain.Entities;

namespace src;

public interface IUserRepository
{
    public User? GetUserByEmail(string email);
    public void Add(User user);
    public void Delete(User user);
    public bool UserExists(string email);
    public IReadOnlyList<User> GetAllUsers();
    public User? GetUserById(Guid id);
}