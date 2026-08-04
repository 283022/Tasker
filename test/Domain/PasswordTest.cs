using src.Domain.ValueObjects;

namespace TestProject1.Domain;

public class PasswordTests
{
    [Theory]
    [InlineData("Password123")]
    [InlineData("StrongP@ssw0rd")]
    [InlineData("MySecurePass1")]
    public void Valid_password_is_created_successfully(string validPassword)
    {
        
        var result = Password.Create(validPassword);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(validPassword, result.Value.Value);
    }

    [Fact]
    public void Password_does_trim_whitespace()
    {
        
        var passwordWithSpaces = "  Password123  ";

        
        var result = Password.Create(passwordWithSpaces);

        
        Assert.True(result.IsSuccess);
        Assert.Equal("Password123", result.Value.Value); // Пробелы сохраняются
    }
}