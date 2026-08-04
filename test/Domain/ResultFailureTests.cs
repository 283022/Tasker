using src.Domain;

namespace TestProject1.Domain;

public class ResultFailureTests
{
    [Fact]
    public void Failure_returns_result_with_success_flag_false()
    {
        // Arrange
        var error = new Error("Validation", "Invalid data");

        // Act
        var result = Result<string>.Failure(error);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Implicit_conversion_from_error_creates_failure_result()
    {
        // Arrange
        var error = new Error("Validation", "Invalid data");

        // Act
        Result<string> result = error;

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Failure_result_throws_when_accessing_value()
    {
        // Arrange
        var error = new Error("Validation", "Invalid data");
        var result = Result<string>.Failure(error);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => result.Value
        );
        Assert.Equal("The value of a failure result", exception.Message);
    }
}