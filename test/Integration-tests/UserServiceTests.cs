using src;
using src.Db;
using src.Domain.Entities;
using src.Domain.ValueObjects;

namespace TestProject1.Integration_tests;


public class UserServiceTests : IClassFixture<TestDatabaseFixture>, IDisposable
{
    private readonly TestDatabaseFixture _fixture;

    public UserServiceTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private User CreateAndSaveUser(ApplicationContext context, string name = "User", string email = null,
        string password = "Password123")
    {
        email ??= $"user_{Guid.NewGuid():N}@test.com";
        var user = User.Create(name, Email.Create(email).Value, Password.Create(password).Value).Value;
        context.Users.Add(user);
        context.SaveChanges();
        return user;
    }

    [Fact]
    public void Register_User_HappyPath()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var userService = new UserService(unitOfWork.UnitOfWork);
        var dto = new CreateUserDto("Username", "string231@gmail.com", "1234567890");

        var result = userService.Register(dto);

        Assert.True(result.IsSuccess);
        var user = context.Users.FirstOrDefault(u => u.Email.Value == dto.Email);
        Assert.NotNull(user);
        Assert.Equal(dto.Email, user.Email.Value);
        Assert.Equal(dto.Password, user.Password.Value);
        Assert.Equal(dto.Name, user.Name);
    }

    [Theory]
    [InlineData(null, "valid@email.com", "ValidPass123")]
    [InlineData("", "valid@email.com", "ValidPass123")]
    [InlineData("   ", "valid@email.com", "ValidPass123")]
    [InlineData("ValidName", null, "ValidPass123")]
    [InlineData("ValidName", "", "ValidPass123")]
    [InlineData("ValidName", "   ", "ValidPass123")]
    [InlineData("ValidName", "invalid-email", "ValidPass123")]
    [InlineData("ValidName", "valid@email.com", null)]
    [InlineData("ValidName", "valid@email.com", "")]
    [InlineData("ValidName", "valid@email.com", "   ")]
    [InlineData("ValidName", "valid@email.com", "123")]
    [InlineData("ValidName", "valid@email.com", "1234567")]
    public void Register_User_WithInvalidData_ReturnsValidationError(
        string name,
        string email,
        string password)
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var userService = new UserService(unitOfWork.UnitOfWork);

        var dto = new CreateUserDto(name, email, password);
        var result = userService.Register(dto);

        Assert.True(result.IsFailure,
            $"Регистрация должна провалиться для Name='{name}', Email='{email}', Password='{password}'");
        Assert.Equal("Validation", result.Error.Errortype);

        if (!string.IsNullOrEmpty(email) && email.Contains('@') && email.Contains('.'))
        {
            var savedUser = context.Users.FirstOrDefault(u => u.Email.Value == email);
            Assert.Null(savedUser);
        }
    }

    [Fact]
    public void Register_User_WithExistingEmail_ReturnsConflict()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var userService = new UserService(unitOfWork.UnitOfWork);
        var dto = new CreateUserDto("User", "existing@email.com", "ValidPass123");

        var firstResult = userService.Register(dto);
        Assert.True(firstResult.IsSuccess);

        var secondResult = userService.Register(dto);

        Assert.True(secondResult.IsFailure);
        Assert.Equal("Conflict", secondResult.Error.Errortype);
        Assert.Contains("already exists", secondResult.Error.Message);
    }

    [Fact]
    public void User_Login_HappyPath()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var userService = new UserService(unitOfWork.UnitOfWork);

        var user = CreateAndSaveUser(context, "user", "valid@gmail.com", "1234567890");
        var dto = new LoginUserDto("valid@gmail.com", "1234567890");

        var result = userService.Userlogin(dto);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("valid@gmail.com", result.Value.Email.Value);
    }

    [Fact]
    public void User_Login_UserNotFound_ReturnsFailure()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var userService = new UserService(unitOfWork.UnitOfWork);
        var dto = new LoginUserDto("notfound@gmail.com", "1234567890");

        var result = userService.Userlogin(dto);

        Assert.True(result.IsFailure);
        Assert.Equal("UserNotFounded", result.Error.Errortype);
        Assert.Contains("not found", result.Error.Message);
    }

    [Fact]
    public void User_Login_PasswordIsIncorrect_ReturnsUnauthorized()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var userService = new UserService(unitOfWork.UnitOfWork);

        var user = CreateAndSaveUser(context, "user", "valid@gmail.com", "1234567890");
        var dto = new LoginUserDto("valid@gmail.com", "wrongPassword");

        var result = userService.Userlogin(dto);

        Assert.True(result.IsFailure);
        Assert.Equal("Unauthorized", result.Error.Errortype);
        Assert.Contains("Invalid credentials", result.Error.Message);
    }

    [Fact]
    public void GetUserById_HappyPath()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var userService = new UserService(unitOfWork.UnitOfWork);

        var user = CreateAndSaveUser(context);
        var result = userService.GetUserById(user.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value.Id);
        Assert.Equal(user.Name, result.Value.Name);
        Assert.Equal(user.Email.Value, result.Value.Email.Value);
    }

    [Fact]
    public void GetUserById_WhenUserNotFound_ReturnsFailure()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var userService = new UserService(unitOfWork.UnitOfWork);

        var nonExistentId = Guid.NewGuid();
        var result = userService.GetUserById(nonExistentId);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Errortype);
        Assert.Contains("User not found", result.Error.Message);
    }

    [Fact]
    public void DeleteUser_HappyPath()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var userService = new UserService(unitOfWork.UnitOfWork);

        var user = CreateAndSaveUser(context);
        var result = userService.DeleteUser(new DeleteUserDto(user.Id), user.Id);

        Assert.True(result.IsSuccess);
        var userAfter = context.Users.FirstOrDefault(u => u.Id == user.Id);
        Assert.Null(userAfter);
    }

    [Fact]
    public void DeleteUser_WhenNotAuthorized_ReturnsFailure()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var userService = new UserService(unitOfWork.UnitOfWork);

        var user = CreateAndSaveUser(context);
        var anotherUserId = Guid.NewGuid();
        var result = userService.DeleteUser(new DeleteUserDto(user.Id), anotherUserId);

        Assert.True(result.IsFailure);
        Assert.Equal("Authorization", result.Error.Errortype);
        Assert.Contains("Cannot delete another user", result.Error.Message);
    }

    [Fact]
    public void DeleteUser_WhenUserNotFound_ReturnsFailure()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var userService = new UserService(unitOfWork.UnitOfWork);

        var nonExistentId = Guid.NewGuid();
        var result = userService.DeleteUser(new DeleteUserDto(nonExistentId), nonExistentId);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Errortype);
        Assert.Contains("User is not founded", result.Error.Message);
    }

    [Fact]
    public void GetUserId_HappyPath()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var userService = new UserService(unitOfWork.UnitOfWork);

        var user = CreateAndSaveUser(context, "user", "valid@test.com", "validPassword12345");
        var dto = new LoginUserDto(user.Email.Value, "validPassword12345");

        var result = userService.GetUserId(dto);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value);
    }

    [Fact]
    public void GetUserId_WhenUserNotFound_ReturnsFailure()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var userService = new UserService(unitOfWork.UnitOfWork);

        var dto = new LoginUserDto("notfound@gmail.com", "password");
        var result = userService.GetUserId(dto);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound", result.Error.Errortype);
        Assert.Contains("not found", result.Error.Message);
    }

    public void Dispose()
    {
        // Ничего не делаем
    }
}