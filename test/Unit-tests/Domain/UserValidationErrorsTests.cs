using src.Domain.Entities;
using src.Domain.ValueObjects;
using src.Security;

namespace TestProject1.Domain;

public class UserValidationErrorsTests
{
    
    [Fact]
    public void Create_User_returns_failure_when_name_is_empty()
    {
        // Arrange
        var email = Email.Create("user@example.com").Value;
        var password = Password.Create("Password123", new Passwordhasher()).Value;

        // Act
        var result = User.Create("", email, password);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Name cannot be null or empty", result.Error.Message);
    }
}