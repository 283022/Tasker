using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using src;
using src.Db;
using src.Domain.Entities;
using src.Domain.ValueObjects;
using src.Security;
using src.Service;

namespace TestProject1.Integration_tests;

public class TeamServiceTests : IClassFixture<TestDatabaseFixture>, IAsyncLifetime
{
    private readonly TestDatabaseFixture _fixture;

    public TeamServiceTests(TestDatabaseFixture fixture)
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

    private async Task<User> CreateAndSaveUserAsync(ApplicationContext context, string name = "User", string email = null,
        string password = "Password123")
    {
        email ??= $"user_{Guid.NewGuid():N}@test.com";
        var user = User.Create(name, Email.Create(email).Value, Password.Create(password, new Passwordhasher()).Value).Value;
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task CreateTeam_HappyPath()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var teamService = new TeamService(unitOfWork.UnitOfWork, new NullLogger<TeamService>());

        var user = await CreateAndSaveUserAsync(context);
        var dto = new CreateTeamDto("Test Team");

        // Act
        var result = await teamService.CreateAsync(user.Id, dto);

        // Assert
        Assert.True(result.IsSuccess);
        var team = await context.Teams.FirstOrDefaultAsync(t => t.Name == "Test Team");
        Assert.NotNull(team);
        var adminMember = await context.TeamMembers.FirstOrDefaultAsync(tm => tm.TeamId == team.Id && tm.UserId == user.Id);
        Assert.NotNull(adminMember);
        Assert.True(adminMember.IsAdmin);
    }

    [Fact]
    public async Task CreateTeam_WithEmptyName_ReturnsValidationError()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var teamService = new TeamService(unitOfWork.UnitOfWork, new NullLogger<TeamService>());

        var user = await CreateAndSaveUserAsync(context);
        var dto = new CreateTeamDto("");

        // Act
        var result = await teamService.CreateAsync(user.Id, dto);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Errortype);
        Assert.Contains("Team name cannot be empty", result.Error.Message);
    }

    [Fact]
    public async Task DeleteTeam_AsAdmin_HappyPath()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var teamService = new TeamService(unitOfWork.UnitOfWork, new NullLogger<TeamService>());

        var admin = await CreateAndSaveUserAsync(context, "Admin", "admin5@test.com");
        var createTeamResult = await teamService.CreateAsync(admin.Id, new CreateTeamDto("Test Team"));
        Assert.True(createTeamResult.IsSuccess);

        var team = await context.Teams.FirstOrDefaultAsync(t => t.Name == "Test Team");
        Assert.NotNull(team);

        // Act
        var result = await teamService.DeleteAsync(new DeleteTeamDto(team.Id), admin.Id);

        // Assert
        Assert.True(result.IsSuccess);
        var teamAfter = await context.Teams.FirstOrDefaultAsync(t => t.Id == team.Id);
        Assert.Null(teamAfter);
    }

    [Fact]
    public async Task DeleteTeam_AsNonAdmin_ReturnsAuthorizationError()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var teamService = new TeamService(unitOfWork.UnitOfWork, new NullLogger<TeamService>());

        var admin = await CreateAndSaveUserAsync(context, "Admin", "admin6@test.com");
        var createTeamResult = await teamService.CreateAsync(admin.Id, new CreateTeamDto("Test Team"));
        Assert.True(createTeamResult.IsSuccess);

        var team = await context.Teams.FirstOrDefaultAsync(t => t.Name == "Test Team");
        Assert.NotNull(team);

        var nonAdmin = await CreateAndSaveUserAsync(context, "NonAdmin", "nonadmin3@test.com");
        var addUserResult = await teamService.AddUserAsync(new AddUserInTeamDto(team.Id, nonAdmin.Id, false), admin.Id);
        Assert.True(addUserResult.IsSuccess);

        // Act
        var result = await teamService.DeleteAsync(new DeleteTeamDto(team.Id), nonAdmin.Id);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Authorization", result.Error.Errortype);
    }

    [Fact]
    public async Task AddUserToTeam_AsAdmin_HappyPath()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var teamService = new TeamService(unitOfWork.UnitOfWork, new NullLogger<TeamService>());

        var admin = await CreateAndSaveUserAsync(context, "Admin", "admin7@test.com");
        var createTeamResult = await teamService.CreateAsync(admin.Id, new CreateTeamDto("Test Team"));
        Assert.True(createTeamResult.IsSuccess);

        var team = await context.Teams.FirstOrDefaultAsync(t => t.Name == "Test Team");
        Assert.NotNull(team);

        var user = await CreateAndSaveUserAsync(context, "User", "user5@test.com");
        var dto = new AddUserInTeamDto(team.Id, user.Id, false);

        // Act
        var result = await teamService.AddUserAsync(dto, admin.Id);

        // Assert
        Assert.True(result.IsSuccess, $"{result.Error.Errortype}: {result.Error.Message}");
        var member = await context.TeamMembers.FirstOrDefaultAsync(tm => tm.TeamId == team.Id && tm.UserId == user.Id);
        Assert.NotNull(member);
        Assert.False(member.IsAdmin);
    }

    [Fact]
    public async Task AddUserToTeam_WhenUserAlreadyInTeam_ReturnsValidationError()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var teamService = new TeamService(unitOfWork.UnitOfWork, new NullLogger<TeamService>());

        var admin = await CreateAndSaveUserAsync(context, "Admin", "admin8@test.com");
        var createTeamResult = await teamService.CreateAsync(admin.Id, new CreateTeamDto("Test Team"));
        Assert.True(createTeamResult.IsSuccess);

        var team = await context.Teams.FirstOrDefaultAsync(t => t.Name == "Test Team");
        Assert.NotNull(team);

        var user = await CreateAndSaveUserAsync(context, "User", "user6@test.com");
        var addUserResult = await teamService.AddUserAsync(new AddUserInTeamDto(team.Id, user.Id, false), admin.Id);
        Assert.True(addUserResult.IsSuccess);

        var dto = new AddUserInTeamDto(team.Id, user.Id, false);

        // Act
        var result = await teamService.AddUserAsync(dto, admin.Id);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Conflict", result.Error.Errortype);
    }

    [Fact]
    public async Task DeleteUserFromTeam_AsAdmin_HappyPath()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var teamService = new TeamService(unitOfWork.UnitOfWork, new NullLogger<TeamService>());

        var admin = await CreateAndSaveUserAsync(context, "Admin", "admin9@test.com");
        var createTeamResult = await teamService.CreateAsync(admin.Id, new CreateTeamDto("Test Team"));
        Assert.True(createTeamResult.IsSuccess);

        var team = await context.Teams.FirstOrDefaultAsync(t => t.Name == "Test Team");
        Assert.NotNull(team);

        var user = await CreateAndSaveUserAsync(context, "User", "user7@test.com");
        var addUserResult = await teamService.AddUserAsync(new AddUserInTeamDto(team.Id, user.Id, false), admin.Id);
        Assert.True(addUserResult.IsSuccess);

        var dto = new DeleteUserFromTeamDto(team.Id, user.Id);

        // Act
        var result = await teamService.DeleteUserAsync(dto, admin.Id);

        // Assert
        Assert.True(result.IsSuccess);
        var member = await context.TeamMembers.FirstOrDefaultAsync(tm => tm.TeamId == team.Id && tm.UserId == user.Id);
        Assert.Null(member);
    }

    [Fact]
    public async Task GetAllTeamWhereUserMember_HappyPath()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var teamService = new TeamService(unitOfWork.UnitOfWork, new NullLogger<TeamService>());

        var admin1 = await CreateAndSaveUserAsync(context, "Admin1", "admin10@test.com");
        var admin2 = await CreateAndSaveUserAsync(context, "Admin2", "admin11@test.com");
        var user = await CreateAndSaveUserAsync(context, "User", "user8@test.com");

        var createTeamResult1 = await teamService.CreateAsync(admin1.Id, new CreateTeamDto("Team 1"));
        Assert.True(createTeamResult1.IsSuccess);
        var createTeamResult2 = await teamService.CreateAsync(admin2.Id, new CreateTeamDto("Team 2"));
        Assert.True(createTeamResult2.IsSuccess);

        var team1 = await context.Teams.FirstOrDefaultAsync(t => t.Name == "Team 1");
        var team2 = await context.Teams.FirstOrDefaultAsync(t => t.Name == "Team 2");
        Assert.NotNull(team1);
        Assert.NotNull(team2);

        var addUserResult1 = await teamService.AddUserAsync(new AddUserInTeamDto(team1.Id, user.Id, false), admin1.Id);
        Assert.True(addUserResult1.IsSuccess);
        var addUserResult2 = await teamService.AddUserAsync(new AddUserInTeamDto(team2.Id, user.Id, false), admin2.Id);
        Assert.True(addUserResult2.IsSuccess);

        // Act
        var result = await teamService.GetAllTeamWhereUserMemberAsync(user.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count());
    }

    [Fact]
    public async Task GetAllTeamWithUsers_HappyPath()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var teamService = new TeamService(unitOfWork.UnitOfWork, new NullLogger<TeamService>());

        var admin = await CreateAndSaveUserAsync(context, "Admin", "admin12@test.com");
        var createTeamResult = await teamService.CreateAsync(admin.Id, new CreateTeamDto("Test Team"));
        Assert.True(createTeamResult.IsSuccess);

        var team = await context.Teams.FirstOrDefaultAsync(t => t.Name == "Test Team");
        Assert.NotNull(team);

        var dto = new GetTeamDto(team.Id);

        // Act
        var result = await teamService.GetAllTeamWithUsersAsync(dto, admin.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal(admin.Id, result.Value.First().UserId);
    }

    [Fact]
    public async Task GetAllTeamWithUsers_WhenUserNotMember_ReturnsAuthorizationError()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var teamService = new TeamService(unitOfWork.UnitOfWork, new NullLogger<TeamService>());

        var admin = await CreateAndSaveUserAsync(context, "Admin", "admin13@test.com");
        var createTeamResult = await teamService.CreateAsync(admin.Id, new CreateTeamDto("Test Team"));
        Assert.True(createTeamResult.IsSuccess);

        var team = await context.Teams.FirstOrDefaultAsync(t => t.Name == "Test Team");
        Assert.NotNull(team);

        var otherUser = await CreateAndSaveUserAsync(context, "Other", "other@test.com");
        var dto = new GetTeamDto(team.Id);

        // Act
        var result = await teamService.GetAllTeamWithUsersAsync(dto, otherUser.Id);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Authorization", result.Error.Errortype);
    }

    [Fact]
    public async Task UpdateMemberRole_HappyPath()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var teamService = new TeamService(unitOfWork.UnitOfWork, new NullLogger<TeamService>());

        var admin = await CreateAndSaveUserAsync(context, "Admin", "admin14@test.com");
        var createTeamResult = await teamService.CreateAsync(admin.Id, new CreateTeamDto("Test Team"));
        Assert.True(createTeamResult.IsSuccess);

        var team = await context.Teams.FirstOrDefaultAsync(t => t.Name == "Test Team");
        Assert.NotNull(team);

        var user = await CreateAndSaveUserAsync(context, "User", "user9@test.com");
        var addUserResult = await teamService.AddUserAsync(new AddUserInTeamDto(team.Id, user.Id, false), admin.Id);
        Assert.True(addUserResult.IsSuccess);

        var dto = new UpdateTeamRoleDto(team.Id, user.Id);

        // Act
        var result = await teamService.UpdateMemberRoleAsync(dto, admin.Id);

        // Assert
        Assert.True(result.IsSuccess);
        var updatedMember = await context.TeamMembers.FirstOrDefaultAsync(tm => tm.TeamId == team.Id && tm.UserId == user.Id);
        Assert.True(updatedMember.IsAdmin);
    }

    [Fact]
    public async Task UpdateMemberRole_WhenUserAlreadyAdmin_ReturnsBadRequest()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var teamService = new TeamService(unitOfWork.UnitOfWork, new NullLogger<TeamService>());

        var admin = await CreateAndSaveUserAsync(context, "Admin", "admin15@test.com");
        var createTeamResult = await teamService.CreateAsync(admin.Id, new CreateTeamDto("Test Team"));
        Assert.True(createTeamResult.IsSuccess);

        var team = await context.Teams.FirstOrDefaultAsync(t => t.Name == "Test Team");
        Assert.NotNull(team);

        var otherAdmin = await CreateAndSaveUserAsync(context, "OtherAdmin", "otheradmin@test.com");
        var addUserResult = await teamService.AddUserAsync(new AddUserInTeamDto(team.Id, otherAdmin.Id, true), admin.Id);
        Assert.True(addUserResult.IsSuccess);

        var dto = new UpdateTeamRoleDto(team.Id, otherAdmin.Id);

        // Act
        var result = await teamService.UpdateMemberRoleAsync(dto, admin.Id);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Conflict", result.Error.Errortype);
    }

    [Fact]
    public async Task GetAllTasksTeam_HappyPath()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var teamService = new TeamService(unitOfWork.UnitOfWork, new NullLogger<TeamService>());

        var admin = await CreateAndSaveUserAsync(context, "Admin", "admin16@test.com");
        var createTeamResult = await teamService.CreateAsync(admin.Id, new CreateTeamDto("Test Team"));
        Assert.True(createTeamResult.IsSuccess);

        var team = await context.Teams.FirstOrDefaultAsync(t => t.Name == "Test Team");
        Assert.NotNull(team);

        var user = await CreateAndSaveUserAsync(context, "User", "user10@test.com");
        var addUserResult = await teamService.AddUserAsync(new AddUserInTeamDto(team.Id, user.Id, false), admin.Id);
        Assert.True(addUserResult.IsSuccess);

        var task1 = UserTask.Create("Task 1", admin.Id, team.Id, user.Id).Value;
        var task2 = UserTask.Create("Task 2", admin.Id, team.Id, user.Id).Value;
        await context.Tasks.AddRangeAsync(task1, task2);
        await context.SaveChangesAsync();

        var dto = new GetAllTeamTaskDto(team.Id);

        // Act
        var result = await teamService.GetAllTasksTeamAsync(dto, user.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count());
    }

    [Fact]
    public async Task GetAllTasksTeam_WhenUserNotMember_ReturnsAuthorizationError()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var teamService = new TeamService(unitOfWork.UnitOfWork, new NullLogger<TeamService>());

        var admin = await CreateAndSaveUserAsync(context, "Admin", "admin17@test.com");
        var createTeamResult = await teamService.CreateAsync(admin.Id, new CreateTeamDto("Test Team"));
        Assert.True(createTeamResult.IsSuccess);

        var team = await context.Teams.FirstOrDefaultAsync(t => t.Name == "Test Team");
        Assert.NotNull(team);

        var otherUser = await CreateAndSaveUserAsync(context, "Other", "other2@test.com");
        var dto = new GetAllTeamTaskDto(team.Id);

        // Act
        var result = await teamService.GetAllTasksTeamAsync(dto, otherUser.Id);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Authorization", result.Error.Errortype);
    }
}