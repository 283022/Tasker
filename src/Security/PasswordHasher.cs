
using src.Domain;

namespace src.Security;

public class Passwordhasher : IPasswordHasher
{
    public string Hash(string password)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        return hash ?? throw new  Exception("Password has invalid hash");
    }

    public bool Verify(string password, string hash)
    {
        var boolean = BCrypt.Net.BCrypt.Verify(password, Hash(password));
        return boolean;
    }
}