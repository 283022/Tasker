using Castle.Core.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using src.Db;
using src.Domain.Entities;
using src.Domain.ValueObjects;
using src.Security;
using src.Service;

namespace TestProject1.Integration_tests;

public class UserServiceTests : IClassFixture<TestDatabaseFixture>, IAsyncLifetime
{
    private readonly TestDatabaseFixture _fixture;

    public UserServiceTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    // Вызывается перед каждым тестом
    public async Task InitializeAsync()
    {
        await _fixture.CleanDatabaseAsync();
    }

    // Вызывается после каждого теста
    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    private async Task<User> CreateAndSaveUserAsync(ApplicationContext context, string name = "User",
        string? email = null,
        string password = "Password123")
    {
        if (email is null)
        {
            email ??= $"user_{Guid.NewGuid():N}@test.com";
        }
        
        var user = User.Create(name, Email.Create(email).Value, Password.Create(password, new Passwordhasher()).Value)
            .Value;
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task Register_User_HappyPath()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var hasher = new Passwordhasher();
        var userService = new UserService(unitOfWork.UnitOfWork, hasher, new NullLogger<UserService>());
        var dto = new CreateUserDto("Username", "string231@gmail.com", "1234567890");

        // Act
        var result = await userService.RegisterAsync(dto);

        // Assert
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
    [InlineData("               ", "valid@email.com", "ValidPass123")]
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
        // Arrange
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var hasher = new Passwordhasher();
        var userService = new UserService(unitOfWork.UnitOfWork, hasher, new NullLogger<UserService>());

        // Act
         var dto = new CreateUserDto(name, email, password);
        var result = await userService.RegisterAsync(dto);

        // Assert
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
        // Arrange
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var hashser = new Passwordhasher();
        var userService = new UserService(unitOfWork.UnitOfWork, hashser, new NullLogger<UserService>());
        var dto = new CreateUserDto("User", "existing@email.com", "ValidPass123");

        // Act
        var firstResult = await userService.RegisterAsync(dto);
        var secondResult = await userService.RegisterAsync(dto);

        // Assert
        Assert.True(firstResult.IsSuccess);
        Assert.True(secondResult.IsFailure);
        Assert.Equal("Conflict", secondResult.Error.Errortype);
        Assert.Contains("already exists", secondResult.Error.Message);
    }

    [Fact]
    public async Task User_Login_HappyPath()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var userService = new UserService(unitOfWork.UnitOfWork, new Passwordhasher(), new NullLogger<UserService>());

        var user = await CreateAndSaveUserAsync(context, "user", "valid@gmail.com", "1234567890");
        var dto = new LoginUserDto("valid@gmail.com", "1234567890");

        // Act
        var result = await userService.UserLoginAsync(dto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("valid@gmail.com", result.Value.Email.Value);
    }

    [Fact]
    public async Task User_Login_UserNotFound_ReturnsFailure()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var userService = new UserService(unitOfWork.UnitOfWork, new Passwordhasher(), new NullLogger<UserService>());
        var dto = new LoginUserDto("notfound@gmail.com", "1234567890");

        // Act
        var result = await userService.UserLoginAsync(dto);

        // Assert
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task User_Login_PasswordIsIncorrect_ReturnsUnauthorized()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var userService = new UserService(unitOfWork.UnitOfWork, new Passwordhasher(), new NullLogger<UserService>());

        var user = await CreateAndSaveUserAsync(context, "user", "valid@gmail.com", "1234567890");
        var dto = new LoginUserDto("valid@gmail.com", "wrongPassword");

        // Act
        var result = await userService.UserLoginAsync(dto);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Unauthorized", result.Error.Errortype);
    }

    [Fact]
    public async Task GetUserById_HappyPath()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var userService = new UserService(unitOfWork.UnitOfWork, new Passwordhasher(), new NullLogger<UserService>());

        var user = await CreateAndSaveUserAsync(context);

        // Act
        var result = await userService.GetUserByIdAsync(user.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value.Id);
        Assert.Equal(user.Name, result.Value.Name);
        Assert.Equal(user.Email.Value, result.Value.Email.Value);
    }

    [Fact]
    public async Task GetUserById_WhenUserNotFound_ReturnsFailure()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var userService = new UserService(unitOfWork.UnitOfWork, new Passwordhasher(), new NullLogger<UserService>());

        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await userService.GetUserByIdAsync(nonExistentId);

        // Assert
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task DeleteUser_HappyPath()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var userService = new UserService(unitOfWork.UnitOfWork, new Passwordhasher(), new NullLogger<UserService>());

        var user = await CreateAndSaveUserAsync(context);

        // Act
        var result = await userService.DeleteUserAsync(new DeleteUserDto(user.Id), user.Id);

        // Assert
        Assert.True(result.IsSuccess);
        var userAfter = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        Assert.Null(userAfter);
    }

    [Fact]
    public async Task DeleteUser_WhenNotAuthorized_ReturnsFailure()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var userService = new UserService(unitOfWork.UnitOfWork, new Passwordhasher(), new NullLogger<UserService>());

        var user = await CreateAndSaveUserAsync(context);
        var anotherUserId = Guid.NewGuid();

        // Act
        var result = await userService.DeleteUserAsync(new DeleteUserDto(user.Id), anotherUserId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Authorization", result.Error.Errortype);
    }

    [Fact]
    public async Task DeleteUser_WhenUserNotFound_ReturnsFailure()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var userService = new UserService(unitOfWork.UnitOfWork, new Passwordhasher(), new NullLogger<UserService>());

        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await userService.DeleteUserAsync(new DeleteUserDto(nonExistentId), nonExistentId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("NotFound", result.Error.Errortype);
    }

    [Fact]
    public async Task GetUserId_HappyPath()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var userService = new UserService(unitOfWork.UnitOfWork, new Passwordhasher(), new NullLogger<UserService>());

        var user = await CreateAndSaveUserAsync(context, "user", "valid@test.com", "validPassword12345");
        var dto = new LoginUserDto(user.Email.Value, "validPassword12345");

        // Act
        var result = await userService.GetUserIdAsync(dto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value);
    }

    [Fact]
    public async Task GetUserId_WhenUserNotFound_ReturnsFailure()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var userService = new UserService(unitOfWork.UnitOfWork, new Passwordhasher(), new NullLogger<UserService>());

        var dto = new LoginUserDto("notfound@gmail.com", "password");

        // Act
        var result = await userService.GetUserIdAsync(dto);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("NotFound", result.Error.Errortype);
        Assert.Contains("not found", result.Error.Message);
    }
}