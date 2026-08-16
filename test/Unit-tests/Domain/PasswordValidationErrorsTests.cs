using src.Domain.ValueObjects;
using src.Security;

namespace TestProject1.Domain;

public class PasswordValidationErrorsTests
{
    [Theory]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("           ", false)]
    public void Creating_password_with_empty_value_return_failure(string password, bool expected)
    {
        var sut = Password.Create(password, new Passwordhasher());
        
        Assert.Equal(sut.IsSuccess, expected);
    }
    
    [Theory]
    [InlineData("1234567",false )] // 7 символов
    [InlineData("12345", false)]  //5 символов
    [InlineData("", false)]                   // пустая строка
    public void Creating_password_shorter_than_8_characters_returns_failure(
        string shortPassword, 
        bool  expected)
    {
        // Act
        var result = Password.Create(shortPassword, new Passwordhasher());

        // Assert
        Assert.Equal(expected, result.IsSuccess);
    }
    
    
}