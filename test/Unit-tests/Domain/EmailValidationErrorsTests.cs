using src.Domain.ValueObjects;

namespace TestProject1.Domain;

public class EmailValidationErrorsTests
{
    [Theory]
    [InlineData("")]                    // Пустая строка
    [InlineData(" ")]                   // Только пробелы
    [InlineData(null)]                  // null
    public void Creating_email_with_empty_value_returns_failure(string invalidEmail)
    {
        // Act
        var result = Email.Create(invalidEmail);

        // Assert
        
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Creating_email_exceeding_max_length_returns_failure()
    {
        // Arrange
        var longEmail = new string('a', 240) + "@example.com"; // 240 + 13 = 253 символа (ок)
        var tooLongEmail = new string('a', 250) + "@example.com"; // 250 + 13 = 263 > 254

        // Act
        var result = Email.Create(tooLongEmail);

        // Assert
        
        Assert.True(result.IsFailure);
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("user@")]
    [InlineData("@example.com")]
    [InlineData("user@example.")]
    [InlineData("user name@example.com")] // Пробел в локальной части
    public void Creating_email_with_invalid_format_returns_failure(string invalidEmail)
    {
        // Act
        var result = Email.Create(invalidEmail);

        Assert.True(result.IsFailure);

    }

    [Fact]
    public void Creating_email_with_local_part_exceeding_64_characters_returns_failure()
    {
        // Arrange
        var localPart = new string('a', 65);
        var email = $"{localPart}@example.com";

        // Act
        var result = Email.Create(email);

        // Assert
        Assert.True(result.IsFailure);

    }
}