using Castle.Core.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using src.Db;
using src.Domain.Entities;
using src.Domain.ValueObjects;
using src.Security;
using src.Service;

namespace TestProject1.Integration_tests;

public class UserServiceTests : IClassFixture<TestDatabaseFixture>, IDisposable
{
    private readonly TestDatabaseFixture _fixture;

    public UserServiceTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<User> CreateAndSaveUserAsync(ApplicationContext context, string name = "User",
        string? email = null,
        string password = "Password123")
    {
        email ??= $"user_{Guid.NewGuid():N}@test.com";
        var user = User.Create(name, Email.Create(email).Value, Password.Create(password, new Passwordhasher()).Value)
            .Value;
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task Register_User_HappyPath()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var hasher = new Passwordhasher();
        var userService = new UserService(unitOfWork.UnitOfWork, hasher,new NullLogger<UserService>());
        var dto = new CreateUserDto("Username", "string231@gmail.com", "1234567890");

        var result = await userService.RegisterAsync(dto);

        Assert.True(result.IsSuccess);
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email.Value == dto.Email);
        Assert.NotNull(user);
        Assert.Equal(dto.Email, user.Email.Value);
        Assert.True(hasher.Verify(dto.Password, user.Password.HashPassword));
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
    public async Task Register_User_WithInvalidData_ReturnsValidationError(
        string? name,
        string? email,
        string? password)
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var hasher = new Passwordhasher();
        var userService = new UserService(unitOfWork.UnitOfWork, hasher,new NullLogger<UserService>());

        var dto = new CreateUserDto(name, email, password);
        var result = await userService.RegisterAsync(dto);

        Assert.True(result.IsFailure);

        if (!string.IsNullOrEmpty(email) && email.Contains('@') && email.Contains('.'))
        {
            var savedUser = await context.Users.FirstOrDefaultAsync(u => u.Email.Value == email);
            Assert.Null(savedUser);
        }
    }

    [Fact]
    public async Task Register_User_WithExistingEmail_ReturnsConflict()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var hashser = new Passwordhasher();
        var userService = new UserService(unitOfWork.UnitOfWork, hashser,new NullLogger<UserService>());
        var dto = new CreateUserDto("User", "existing@email.com", "ValidPass123");

        var firstResult = await userService.RegisterAsync(dto);
        Assert.True(firstResult.IsSuccess);

        var secondResult = await userService.RegisterAsync(dto);

        Assert.True(secondResult.IsFailure);
        Assert.Equal("Conflict", secondResult.Error.Errortype);
        Assert.Contains("already exists", secondResult.Error.Message);
    }

    [Fact]
    public async Task User_Login_HappyPath()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var userService = new UserService(unitOfWork.UnitOfWork, new Passwordhasher(), new NullLogger<UserService>());

        var user = await CreateAndSaveUserAsync(context, "user", "valid@gmail.com", "1234567890");
        var dto = new LoginUserDto("valid@gmail.com", "1234567890");

        var result = await userService.UserLoginAsync(dto);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("valid@gmail.com", result.Value.Email.Value);
    }

    [Fact]
    public async Task User_Login_UserNotFound_ReturnsFailure()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var userService = new UserService(unitOfWork.UnitOfWork, new Passwordhasher(),new NullLogger<UserService>());
        var dto = new LoginUserDto("notfound@gmail.com", "1234567890");

        var result = await userService.UserLoginAsync(dto);

        Assert.True(result.IsFailure);
        Assert.Equal("UserNotFounded", result.Error.Errortype);
        Assert.Contains("not found", result.Error.Message);
    }

    [Fact]
    public async Task User_Login_PasswordIsIncorrect_ReturnsUnauthorized()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var userService = new UserService(unitOfWork.UnitOfWork, new Passwordhasher(),new NullLogger<UserService>());

        var user = await CreateAndSaveUserAsync(context, "user", "valid@gmail.com", "1234567890");
        var dto = new LoginUserDto("valid@gmail.com", "wrongPassword");

        var result = await userService.UserLoginAsync(dto);

        Assert.True(result.IsFailure);
        Assert.Equal("Unauthorized", result.Error.Errortype);
    }

    [Fact]
    public async Task GetUserById_HappyPath()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var userService = new UserService(unitOfWork.UnitOfWork, new Passwordhasher(),new NullLogger<UserService>());

        var user = await CreateAndSaveUserAsync(context);
        var result = await userService.GetUserByIdAsync(user.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value.Id);
        Assert.Equal(user.Name, result.Value.Name);
        Assert.Equal(user.Email.Value, result.Value.Email.Value);
    }

    [Fact]
    public async Task GetUserById_WhenUserNotFound_ReturnsFailure()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var userService = new UserService(unitOfWork.UnitOfWork, new Passwordhasher(),new NullLogger<UserService>());

        var nonExistentId = Guid.NewGuid();
        var result = await userService.GetUserByIdAsync(nonExistentId);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Errortype);
        Assert.Contains("User not found", result.Error.Message);
    }

    [Fact]
    public async Task DeleteUser_HappyPath()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var userService = new UserService(unitOfWork.UnitOfWork, new Passwordhasher(),new NullLogger<UserService>());

        var user = await CreateAndSaveUserAsync(context);
        var result = await userService.DeleteUserAsync(new DeleteUserDto(user.Id), user.Id);

        Assert.True(result.IsSuccess);
        var userAfter = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        Assert.Null(userAfter);
    }

    [Fact]
    public async Task DeleteUser_WhenNotAuthorized_ReturnsFailure()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var userService = new UserService(unitOfWork.UnitOfWork, new Passwordhasher(),new NullLogger<UserService>());

        var user = await CreateAndSaveUserAsync(context);
        var anotherUserId = Guid.NewGuid();
        var result = await userService.DeleteUserAsync(new DeleteUserDto(user.Id), anotherUserId);

        Assert.True(result.IsFailure);
        Assert.Equal("Authorization", result.Error.Errortype);
    }

    [Fact]
    public async Task DeleteUser_WhenUserNotFound_ReturnsFailure()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var userService = new UserService(unitOfWork.UnitOfWork, new Passwordhasher(),new NullLogger<UserService>());

        var nonExistentId = Guid.NewGuid();
        var result = await userService.DeleteUserAsync(new DeleteUserDto(nonExistentId), nonExistentId);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Errortype);
        Assert.Contains("User not found", result.Error.Message);
    }

    [Fact]
    public async Task GetUserId_HappyPath()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var userService = new UserService(unitOfWork.UnitOfWork, new Passwordhasher(),new NullLogger<UserService>());

        var user = await CreateAndSaveUserAsync(context, "user", "valid@test.com", "validPassword12345");
        var dto = new LoginUserDto(user.Email.Value, "validPassword12345");

        var result = await userService.GetUserIdAsync(dto);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value);
    }

    [Fact]
    public async Task GetUserId_WhenUserNotFound_ReturnsFailure()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var userService = new UserService(unitOfWork.UnitOfWork, new Passwordhasher(),new NullLogger<UserService>());

        var dto = new LoginUserDto("notfound@gmail.com", "password");
        var result = await userService.GetUserIdAsync(dto);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound", result.Error.Errortype);
        Assert.Contains("not found", result.Error.Message);
    }

    public void Dispose()
    {
        // Ничего не делаем
    }
}