using src;
using src.Mappers;

public class UserServiceTests
{
    private readonly UserRepository _realUserRepository;
    private readonly MapperUserFromDto _realMapper;
    private readonly UserService _sut;
    
    public UserServiceTests()
    {
        _realUserRepository = new UserRepository();
        _realMapper = new MapperUserFromDto();
        _sut = new UserService(_realUserRepository, _realMapper);
    }
    
    
    [Fact]
    public void Register_returns_success_when_user_is_valid()
    {
        // Arrange
        var request = new CreateUserDto("John", "user@example.com", "Password123");
        
        // Act
        var result = _sut.Register(request);
        
        // Assert
        Assert.True(result.IsSuccess);
        
        // Проверяем, что пользователь действительно сохранён
        var savedUser = _realUserRepository.GetUserByEmail("user@example.com");
        Assert.NotNull(savedUser);
        Assert.Equal("John", savedUser.Name);
        Assert.Equal("user@example.com", savedUser.Email.Value);
    }
    
    [Fact]
    public void Register_creates_user_with_correct_data()
    {
        // Arrange
        var request = new CreateUserDto("Jane Doe", "jane@example.com", "SecurePass123");
        
        // Act
        var result = _sut.Register(request);
        
        // Assert
        Assert.True(result.IsSuccess);
        
        var savedUser = _realUserRepository.GetUserByEmail("jane@example.com");
        Assert.NotNull(savedUser);
        Assert.Equal("Jane Doe", savedUser.Name);
        Assert.Equal("jane@example.com", savedUser.Email.Value);
        Assert.NotEqual(Guid.Empty, savedUser.ID);
    }
    
    [Fact]
    public void Register_accepts_email_with_uppercase_and_normalizes_it()
    {
        // Arrange
        var request = new CreateUserDto("John", "USER@Example.COM", "Password123");
        
        // Act
        var result = _sut.Register(request);
        
        // Assert
        Assert.True(result.IsSuccess);
        
        var savedUser = _realUserRepository.GetUserByEmail("user@example.com");
        Assert.NotNull(savedUser);
        Assert.Equal("user@example.com", savedUser.Email.Value);
    }
    
    [Fact]
    public void Register_accepts_email_with_whitespace_and_trims_it()
    {
        // Arrange
        var request = new CreateUserDto("John", "  user@example.com  ", "Password123");
        
        // Act
        var result = _sut.Register(request);
        
        // Assert
        Assert.True(result.IsSuccess);
        
        var savedUser = _realUserRepository.GetUserByEmail("user@example.com");
        Assert.NotNull(savedUser);
        Assert.Equal("user@example.com", savedUser.Email.Value);
    }
    
    [Fact]
    public void Register_accepts_complex_email_formats()
    {
        // Arrange
        var request = new CreateUserDto("John", "john.doe+test@company.co.uk", "Password123");
        
        // Act
        var result = _sut.Register(request);
        
        // Assert
        Assert.True(result.IsSuccess);
        
        var savedUser = _realUserRepository.GetUserByEmail("john.doe+test@company.co.uk");
        Assert.NotNull(savedUser);
    }
    
    [Fact]
    public void UserLogin_returns_user_when_credentials_are_valid()
    {
        // Arrange
        var registerRequest = new CreateUserDto("John", "user@example.com", "Password123");
        _sut.Register(registerRequest);
        
        var loginRequest = new LoginUserDto("user@example.com", "Password123");
        
        // Act
        var result = _sut.Userlogin(loginRequest);
        
        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("John", result.Value.Name);
        Assert.Equal("user@example.com", result.Value.Email.Value);
    }
    
    [Fact]
    public void UserLogin_is_case_insensitive_for_email()
    {
        // Arrange
        var registerRequest = new CreateUserDto("John", "user@example.com", "Password123");
        _sut.Register(registerRequest);
        var loginRequest = new LoginUserDto("USER@EXAMPLE.COM", "Password123");
        
        // Act
        var result = _sut.Userlogin(loginRequest);
        
        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("John", result.Value.Name);
    }
    
    [Fact]
    public void GetUserId_returns_user_id_when_user_exists()
    {
        // Arrange
        var registerRequest = new CreateUserDto("John", "user@example.com", "Password123");
        _sut.Register(registerRequest);
        
        var loginRequest = new LoginUserDto("user@example.com", "Password123");
        
        // Act
        var result = _sut.GetUserId(loginRequest);
        
        // Assert
        Assert.True(result.IsSuccess);
        
        var savedUser = _realUserRepository.GetUserByEmail("user@example.com");
        Assert.NotNull(savedUser);
        Assert.Equal(savedUser.ID, result.Value);
    }
    
    [Fact]
    public void Multiple_users_can_be_registered()
    {
        // Arrange
        var user1 = new CreateUserDto("John", "john@example.com", "Password123");
        var user2 = new CreateUserDto("Jane", "jane@example.com", "Password456");
        
        // Act
        var result1 = _sut.Register(user1);
        var result2 = _sut.Register(user2);
        
        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        
        var savedUser1 = _realUserRepository.GetUserByEmail("john@example.com");
        var savedUser2 = _realUserRepository.GetUserByEmail("jane@example.com");
        
        Assert.NotNull(savedUser1);
        Assert.NotNull(savedUser2);
        Assert.NotEqual(savedUser1.ID, savedUser2.ID);
    }
    
    
    [Fact]
    public void Register_returns_failure_when_request_is_null()
    {
        // Act
        var result = _sut.Register(null);
        
        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Request cannot be null", result.Error.Message);
        
        // Проверяем, что пользователь НЕ сохранён
        var allUsers = _realUserRepository.GetAllUsers();
        Assert.Empty(allUsers);
    }
    
    [Fact]
    public void Register_returns_failure_when_name_is_empty()
    {
        // Arrange
        var request = new CreateUserDto("", "user@example.com", "Password123");
        
        // Act
        var result = _sut.Register(request);
        
        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Name cannot be null or empty", result.Error.Message);
        
        var savedUser = _realUserRepository.GetUserByEmail("user@example.com");
        Assert.Null(savedUser);
    }
    
    [Fact]
    public void Register_returns_failure_when_name_is_whitespace()
    {
        // Arrange
        var request = new CreateUserDto("   ", "user@example.com", "Password123");
        
        // Act
        var result = _sut.Register(request);
        
        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Name cannot be null or empty", result.Error.Message);
        
        var savedUser = _realUserRepository.GetUserByEmail("user@example.com");
        Assert.Null(savedUser);
    }
    
    [Fact]
    public void Register_returns_failure_when_email_is_invalid()
    {
        // Arrange
        var request = new CreateUserDto("John", "invalid-email", "Password123");
        
        // Act
        var result = _sut.Register(request);
        
        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Invalid email format", result.Error.Message);
        
        var savedUser = _realUserRepository.GetUserByEmail("invalid-email");
        Assert.Null(savedUser);
    }
    
    [Fact]
    public void Register_returns_failure_when_email_has_spaces_inside()
    {
        // Arrange
        var request = new CreateUserDto("John", "user@example.com with spaces", "Password123");
        
        // Act
        var result = _sut.Register(request);
        
        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Email cannot contain spaces", result.Error.Message);
    }
    
    [Fact]
    public void Register_returns_failure_when_email_is_too_long()
    {
        // Arrange
        var localPart = new string('a', 250);
        var email = $"{localPart}@example.com"; // > 254 символов
        var request = new CreateUserDto("John", email, "Password123");
        
        // Act
        var result = _sut.Register(request);
        
        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Email is too long (max 254 characters)", result.Error.Message);
    }
    
    [Fact]
    public void Register_returns_failure_when_email_local_part_is_too_long()
    {
        // Arrange
        var localPart = new string('a', 65);
        var email = $"{localPart}@example.com";
        var request = new CreateUserDto("John", email, "Password123");
        
        // Act
        var result = _sut.Register(request);
        
        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Local part of email is too long (max 64 characters)", result.Error.Message);
    }
    
    [Fact]
    public void Register_returns_failure_when_password_is_too_short()
    {
        // Arrange
        var request = new CreateUserDto("John", "user@example.com", "1234567");
        
        // Act
        var result = _sut.Register(request);
        
        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Password must contain at least 8 characters", result.Error.Message);
        
        var savedUser = _realUserRepository.GetUserByEmail("user@example.com");
        Assert.Null(savedUser);
    }
    
    [Fact]
    public void Register_returns_failure_when_password_is_empty()
    {
        // Arrange
        var request = new CreateUserDto("John", "user@example.com", "");
        
        // Act
        var result = _sut.Register(request);
        
        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Password cannot be null or empty", result.Error.Message);
    }
    
    [Fact]
    public void Register_returns_failure_when_password_has_spaces()
    {
        // Arrange
        var request = new CreateUserDto("John", "user@example.com", "Password 123");
        
        // Act
        var result = _sut.Register(request);
        
        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Password cannot contain spaces", result.Error.Message);
    }
    
    [Fact]
    public void Register_returns_failure_when_user_already_exists()
    {
        // Arrange
        var request = new CreateUserDto("John", "user@example.com", "Password123");
        _sut.Register(request);
        
        // Act
        var result = _sut.Register(request);
        
        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal($"User with email {request.Email} already exists", result.Error.Message);
        
        // Проверяем, что дубликат НЕ создан
        var allUsers = _realUserRepository.GetAllUsers();
        Assert.Single(allUsers);
    }
    
    [Fact]
    public void Register_returns_failure_when_user_with_same_email_exists_case_insensitive()
    {
        // Arrange
        var request1 = new CreateUserDto("John", "user@example.com", "Password123");
        _sut.Register(request1);
        
        var request2 = new CreateUserDto("Jane", "USER@EXAMPLE.COM", "Password456");
        
        // Act
        var result = _sut.Register(request2);
        
        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal($"User with email {request2.Email} already exists", result.Error.Message);
    }
    
    [Fact]
    public void UserLogin_returns_failure_when_request_is_null()
    {
        // Act
        var result = _sut.Userlogin(null);
        
        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Request cannot be null", result.Error.Message);
    }
    
    [Fact]
    public void UserLogin_returns_failure_when_user_not_found()
    {
        // Arrange
        var request = new LoginUserDto("nonexistent@example.com", "Password123");
        
        // Act
        var result = _sut.Userlogin(request);
        
        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("nonexistent@example.com not found", result.Error.Message);
    }
    
    [Fact]
    public void UserLogin_returns_failure_when_password_is_incorrect()
    {
        // Arrange
        var registerRequest = new CreateUserDto("John", "user@example.com", "Password123");
        _sut.Register(registerRequest);
        
        var loginRequest = new LoginUserDto("user@example.com", "WrongPassword");
        
        // Act
        var result = _sut.Userlogin(loginRequest);
        
        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Invalid credentials", result.Error.Message);
    }
    
    [Fact]
    public void GetUserId_returns_failure_when_request_is_null()
    {
        // Act
        var result = _sut.GetUserId(null);
        
        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Request cannot be null", result.Error.Message);
    }
    
    [Fact]
    public void GetUserId_returns_failure_when_user_not_found()
    {
        // Arrange
        var request = new LoginUserDto("nonexistent@example.com", "Password123");
        
        // Act
        var result = _sut.GetUserId(request);
        
        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("nonexistent@example.comnot found", result.Error.Message);
    }
}