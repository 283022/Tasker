namespace src.Domain.ValueObjects;

public class Password : ValueObject
{
    public string Value { get; private set; }

    private Password(string password)
    {
        Value = password;
    }
    
    public static  Result<Password> Create(string password)
    {
        if (string.IsNullOrEmpty(password)) return new Error("Validation", "Password cannot be null or empty");
        if (password.Length < 8) return new Error("Validation", "Password must contain at least 8 characters");
        return new Password(password);
    }


    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}