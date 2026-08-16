using src.Domain.ValueObjects;
using src.Security;

namespace TestProject1.Domain;

public class PasswordTests
{
    [Theory]
    [InlineData("Password123")]
    [InlineData("StrongP@ssw0rd")]
    [InlineData("MySecurePass1")]
    public void Valid_password_is_created_successfully(string validPassword)
    {
        var hasher = new Passwordhasher();
        var result = Password.Create(validPassword, hasher);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(hasher.Verify(validPassword, result.Value.HashPassword));
    }

    [Fact]
    public void Password_does_trim_whitespace()
    {
        
        var  hasher = new Passwordhasher();
        var passwordWithSpaces = "  Password123  ";

        
        var result = Password.Create(passwordWithSpaces, hasher);

        
        Assert.True(result.IsSuccess);
        Assert.True(hasher.Verify("Password123",result.Value.HashPassword)); // Пробелы сохраняются
    }
}