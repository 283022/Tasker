using src.Domain;

namespace TestProject1.Domain;

public class ResultTests
{
    [Fact]
    public void Success_returns_result_with_success_flag_true()
    {
        // Act
        var result = Result<string>.Success("test");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal("test", result.Value);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Implicit_conversion_from_value_creates_success_result()
    {
        // Act
        Result<string> result = "test";

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("test", result.Value);
    }

    [Fact]
    public void Success_result_throws_when_accessing_value_after_construction()
    {
        // Arrange & Act
        var result = Result<string>.Success("test");

        // Assert - доступ к Value не должен выбрасывать исключение
        var exception = Record.Exception(() => result.Value);
        Assert.Null(exception);
    }
}