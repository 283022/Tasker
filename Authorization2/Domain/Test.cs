namespace Authorization2.Domain;

public class Test
{
    private string Name { get; set; }
    private string Email { get; set; }
    private string Password { get; set; }
    private Test(string name, string email, string password)
    {
        Name = name;
        Email = email;
        Password = password;
    }
    
    public static Result<Test> ConstructoResult(string Name, string Password, string Email)
    {
        if (string.IsNullOrEmpty(Name)) return new Error("Erorr.Type", "Zxc");
        
        return new Test(Name, Email, Password);
    }
}