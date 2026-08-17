using src.Domain.ValueObjects;

namespace src.Domain.Entities;

public class User
{
    private User()
    {
        
    }
    private User(string name, Email email, Password password)
    {
        Name = name;
        Email = email;
        Password = password;
    }


    public static Result<User> Create(string Name, Email Email, Password Password)
    {
        if(string.IsNullOrEmpty(Name)) return new Error("Validation", "Name cannot be null or empty"); 
        if (Name.Trim().Length == 0) return new Error("Validation", "Name cannot be empty");
        return new User(Name, Email, Password);
    }
    
    public Guid Id { get; private set; } 
    public string Name { get;private set; } 
    public Email Email { get; private set; }
    public Password Password { get; private set; } 
}