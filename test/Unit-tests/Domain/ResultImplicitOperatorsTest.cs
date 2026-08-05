using src.Domain;

namespace TestProject1.Domain;

public class ResultImplicitOperatorsTests
{
    [Fact]
    public void Implicit_conversion_to_ResultT_works_correctly()
    {
        // Arrange
        var value = "test";

        // Act
        Result<string> result = value;

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void Implicit_conversion_to_ResultT_from_error_works_correctly()
    {
        // Arrange
        var error = new Error("Validation", "Invalid");

        // Act
        Result<string> result = error;

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
    }
}