namespace src.Domain.ValueObjects;

public class Password : ValueObject
{
    
    //for ef core
    private Password()
    {
        
    } 
    
    public string Value { get; private set; }

    private Password(string password)
    {
        Value = password;
    }
    
    public static  Result<Password> Create(string password)
    {
        if (string.IsNullOrEmpty(password)) return new Error("Validation", "Password cannot be null or empty");
     
        
        password = password.Trim();

        
        if (password.Contains(" "))
            return Result<Password>.Failure(new Error("Validation", "Password cannot contain spaces"));
        
        if (password.Length < 8) return new Error("Validation", "Password must contain at least 8 characters");
        return new Password(password);
    }


    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}