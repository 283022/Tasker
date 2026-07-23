namespace Authorization2;

public class User(string Name, string Email, string Password)
{
    public Guid ID { get;  } = Guid.NewGuid();
    public string Name { get; set; } = Name;
    public string Email { get; set; } = Email;
    public string Password { get; set; } = Password;
}