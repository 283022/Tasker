using src.Domain.Entities;

namespace TestProject1.Domain;

public class UserTaskTests
{
    [Fact]
    public void Validation_UserTask_create_successfully()
    {
        var name = "email";
        Guid id = Guid.NewGuid();
        
        var userTask = UserTask.Create(name, id);
        
        Assert.True(userTask.IsSuccess);
        Assert.NotNull(userTask.Value);
    }
}