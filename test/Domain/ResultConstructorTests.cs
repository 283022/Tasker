using src.Domain;

namespace TestProject1.Domain;

public class ResultConstructorTests
{
    [Fact]
    public void Constructor_throws_when_success_has_error()
    {
        
        var error = new Error("Validation", "Invalid");
        
        
        Assert.Throws<ArgumentException>(() => Result<string>.Failure(error));
        
    }
}