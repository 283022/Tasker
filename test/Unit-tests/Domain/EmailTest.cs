using src.Domain.ValueObjects;

namespace TestProject1.Domain;

public class EmailTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("john.doe@company.co.uk")]
    [InlineData("user_name@sub.domain.com")]
    public void Valid_email_is_created_successfully(string validEmail)
    {
        // Arrange & Act
        var result = Email.Create(validEmail);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(validEmail.ToLowerInvariant(), result.Value.Value);
    }

    [Fact]
    public void Email_is_normalized_to_lowercase()
    {
        // Arrange
        var email = "USER@Example.COM";

        // Act
        var result = Email.Create(email);

        // Assert
        Assert.Equal("user@example.com", result.Value.Value);
    }

    [Fact]
    public void Email_is_trimmed_of_whitespace()
    {
        // Arrange
        var email = "  user@example.com  ";

        // Act
        var result = Email.Create(email);

        // Assert
        Assert.Equal("user@example.com", result.Value.Value);
    }

    [Fact]
    public void Local_part_of_email_is_extracted_correctly()
    {
        var data = "user@example.com";
        var email = Email.Create(data);
        
        var result = email.Value.GetLocalPart();
        
        Assert.Equal("user", result);
    }

    [Fact]
    public void Domain_part_of_email_is_extracted_correctly()
    {
        var data = "user@example.com";
        var email = Email.Create(data);

        var result = email.Value.GetDomain();
        
        Assert.Equal("example.com", result);
    }

    [Fact]
    public void Email_belongs_to_domain_when_domain_matches()
    {
        var data = "user@example.com";
        var email = Email.Create(data);
        
        var result = email.Value.IsFromDomain("example.com");
        
        Assert.True(result);
        
        
    }
    
    
    [Theory]
    [InlineData("user@example.com", "EXAMPLE.COM", true)]  // Разный регистр
    [InlineData("user@example.com", "example", false)]     // Неполный домен
    [InlineData("user@sub.example.com", "example.com", false)] // Субдомен не совпадает
    public void IsFromDomain_handles_various_domain_formats(
        string emailAddress, 
        string domain, 
        bool expected)
    {
        // Arrange
        var email = Email.Create(emailAddress).Value;

        // Act
        var result = email.IsFromDomain(domain);

        // Assert
        Assert.Equal(expected, result);
    }
    
}