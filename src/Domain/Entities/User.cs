using src.Domain.ValueObjects;

namespace src.Domain.Entities;

public class User
{
    private User(string name, Email email, Password password)
    {
        Name = name;
        Email = email;
        Password = password;
    }


    public static Result<User> Create(string Name, Email Email, Password Password)
    {
        if(string.IsNullOrEmpty(Name)) return new Error("Validation", "Name cannot be null or empty"); 
        
        return new User(Name, Email, Password);
    }
    
    public Guid ID { get;  } = Guid.NewGuid();
    public string Name { get; set; } 
    public Email Email { get; set; }
    public Password Password { get; set; } 
}