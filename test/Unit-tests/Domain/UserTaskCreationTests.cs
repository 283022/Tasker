using src.Domain.Entities;

namespace TestProject1.Domain;

public class UserTaskCreationTests
{
    private static readonly Guid ValidUserId = Guid.NewGuid();

    [Fact]
    public void Create_UserTask_returns_success_when_all_fields_are_valid()
    {
        // Arrange
        var name = "Complete project report";
        var userId = ValidUserId;

        // Act
        var result = UserTask.Create(name, userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(name, result.Value.Name);
        Assert.Equal(userId, result.Value.CreateByUserID);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_UserTask_returns_failure_when_name_is_empty_or_null(string invalidName)
    {
        // Arrange
        var userId = ValidUserId;

        // Act
        var result = UserTask.Create(invalidName, userId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Name cannot be null or empty", result.Error.Message);
    }

    [Fact]
    public void Create_UserTask_returns_failure_when_user_id_is_empty()
    {
        // Arrange
        var name = "Complete project report";
        var emptyUserId = Guid.Empty;

        // Act
        var result = UserTask.Create(name, emptyUserId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("CreateByUserId cannot be empty", result.Error.Message);
    }

    [Fact]
    public void Create_UserTask_returns_failure_when_both_fields_are_invalid()
    {
        // Arrange
        var invalidName = "";
        var emptyUserId = Guid.Empty;

        // Act
        var result = UserTask.Create(invalidName, emptyUserId);

        // Assert
        Assert.True(result.IsFailure);
        // Проверяем, что ошибка соответствует первому условию
        Assert.Equal("Name cannot be null or empty", result.Error.Message);
    }
}