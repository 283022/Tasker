using src;
using src.Db;
using src.Domain.Entities;
using src.Domain.ValueObjects;

namespace TestProject1.Integration_tests;


public class TeamServiceTests : IClassFixture<TestDatabaseFixture>, IDisposable
{
    private readonly TestDatabaseFixture _fixture;

    public TeamServiceTests(TestDatabaseFixture fixture)
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
    public void CreateTeam_HappyPath()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var teamService = new TeamService(unitOfWork.UnitOfWork);

        var user = CreateAndSaveUser(context);
        var dto = new CreateTeamDto("Test Team");

        var result = teamService.Create(user.Id, dto);

        Assert.True(result.IsSuccess);
        var team = context.Teams.FirstOrDefault(t => t.Name == "Test Team");
        Assert.NotNull(team);
        var adminMember = context.TeamMembers.FirstOrDefault(tm => tm.TeamId == team.Id && tm.UserId == user.Id);
        Assert.NotNull(adminMember);
        Assert.True(adminMember.IsAdmin);
    }

    [Fact]
    public void CreateTeam_WithEmptyName_ReturnsValidationError()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var teamService = new TeamService(unitOfWork.UnitOfWork);

        var user = CreateAndSaveUser(context);
        var dto = new CreateTeamDto("");

        var result = teamService.Create(user.Id, dto);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Errortype);
        Assert.Contains("Team name cannot be empty", result.Error.Message);
    }

    [Fact]
    public void DeleteTeam_AsAdmin_HappyPath()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var teamService = new TeamService(unitOfWork.UnitOfWork);

        var admin = CreateAndSaveUser(context, "Admin", "admin5@test.com");
        var createTeamResult = teamService.Create(admin.Id, new CreateTeamDto("Test Team"));
        Assert.True(createTeamResult.IsSuccess);

        var team = context.Teams.FirstOrDefault(t => t.Name == "Test Team");
        Assert.NotNull(team);

        var result = teamService.Delete(new DeleteTeamDto(team.Id), admin.Id);

        Assert.True(result.IsSuccess);
        var teamAfter = context.Teams.FirstOrDefault(t => t.Id == team.Id);
        Assert.Null(teamAfter);
    }

    [Fact]
    public void DeleteTeam_AsNonAdmin_ReturnsAuthorizationError()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var teamService = new TeamService(unitOfWork.UnitOfWork);

        var admin = CreateAndSaveUser(context, "Admin", "admin6@test.com");
        var createTeamResult = teamService.Create(admin.Id, new CreateTeamDto("Test Team"));
        Assert.True(createTeamResult.IsSuccess);

        var team = context.Teams.FirstOrDefault(t => t.Name == "Test Team");
        Assert.NotNull(team);

        var nonAdmin = CreateAndSaveUser(context, "NonAdmin", "nonadmin3@test.com");
        var addUserResult = teamService.AddUser(new AddUserInTeamDto(team.Id, nonAdmin.Id, false), admin.Id);
        Assert.True(addUserResult.IsSuccess);

        var result = teamService.Delete(new DeleteTeamDto(team.Id), nonAdmin.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Errortype);
        Assert.Contains("User not admin", result.Error.Message);
    }

    [Fact]
    public void AddUserToTeam_AsAdmin_HappyPath()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var teamService = new TeamService(unitOfWork.UnitOfWork);

        var admin = CreateAndSaveUser(context, "Admin", "admin7@test.com");
        var createTeamResult = teamService.Create(admin.Id, new CreateTeamDto("Test Team"));
        Assert.True(createTeamResult.IsSuccess);

        var team = context.Teams.FirstOrDefault(t => t.Name == "Test Team");
        Assert.NotNull(team);

        var user = CreateAndSaveUser(context, "User", "user5@test.com");
        var dto = new AddUserInTeamDto(team.Id, user.Id, false);

        var result = teamService.AddUser(dto, admin.Id);

        Assert.True(result.IsSuccess, $"{result.Error.Errortype}: {result.Error.Message}");
        var member = context.TeamMembers.FirstOrDefault(tm => tm.TeamId == team.Id && tm.UserId == user.Id);
        Assert.NotNull(member);
        Assert.False(member.IsAdmin);
    }

    [Fact]
    public void AddUserToTeam_WhenUserAlreadyInTeam_ReturnsValidationError()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var teamService = new TeamService(unitOfWork.UnitOfWork);

        var admin = CreateAndSaveUser(context, "Admin", "admin8@test.com");
        var createTeamResult = teamService.Create(admin.Id, new CreateTeamDto("Test Team"));
        Assert.True(createTeamResult.IsSuccess);

        var team = context.Teams.FirstOrDefault(t => t.Name == "Test Team");
        Assert.NotNull(team);

        var user = CreateAndSaveUser(context, "User", "user6@test.com");
        var addUserResult = teamService.AddUser(new AddUserInTeamDto(team.Id, user.Id, false), admin.Id);
        Assert.True(addUserResult.IsSuccess);

        var dto = new AddUserInTeamDto(team.Id, user.Id, false);
        var result = teamService.AddUser(dto, admin.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Errortype);
        Assert.Contains("User already in the team", result.Error.Message);
    }

    [Fact]
    public void DeleteUserFromTeam_AsAdmin_HappyPath()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var teamService = new TeamService(unitOfWork.UnitOfWork);

        var admin = CreateAndSaveUser(context, "Admin", "admin9@test.com");
        var createTeamResult = teamService.Create(admin.Id, new CreateTeamDto("Test Team"));
        Assert.True(createTeamResult.IsSuccess);

        var team = context.Teams.FirstOrDefault(t => t.Name == "Test Team");
        Assert.NotNull(team);

        var user = CreateAndSaveUser(context, "User", "user7@test.com");
        var addUserResult = teamService.AddUser(new AddUserInTeamDto(team.Id, user.Id, false), admin.Id);
        Assert.True(addUserResult.IsSuccess);

        var dto = new DeleteUserFromTeamDto(team.Id, user.Id);
        var result = teamService.DeleteUser(dto, admin.Id);

        Assert.True(result.IsSuccess);
        var member = context.TeamMembers.FirstOrDefault(tm => tm.TeamId == team.Id && tm.UserId == user.Id);
        Assert.Null(member);
    }

    [Fact]
    public void GetAllTeamWhereUserMember_HappyPath()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var teamService = new TeamService(unitOfWork.UnitOfWork);

        var admin1 = CreateAndSaveUser(context, "Admin1", "admin10@test.com");
        var admin2 = CreateAndSaveUser(context, "Admin2", "admin11@test.com");
        var user = CreateAndSaveUser(context, "User", "user8@test.com");

        var createTeamResult1 = teamService.Create(admin1.Id, new CreateTeamDto("Team 1"));
        Assert.True(createTeamResult1.IsSuccess);
        var createTeamResult2 = teamService.Create(admin2.Id, new CreateTeamDto("Team 2"));
        Assert.True(createTeamResult2.IsSuccess);

        var team1 = context.Teams.FirstOrDefault(t => t.Name == "Team 1");
        var team2 = context.Teams.FirstOrDefault(t => t.Name == "Team 2");
        Assert.NotNull(team1);
        Assert.NotNull(team2);

        var addUserResult1 = teamService.AddUser(new AddUserInTeamDto(team1.Id, user.Id, false), admin1.Id);
        Assert.True(addUserResult1.IsSuccess);
        var addUserResult2 = teamService.AddUser(new AddUserInTeamDto(team2.Id, user.Id, false), admin2.Id);
        Assert.True(addUserResult2.IsSuccess);

        var result = teamService.GetAllTeamWhereUserMember(user.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count());
    }

    [Fact]
    public void GetAllTeamWithUsers_HappyPath()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var teamService = new TeamService(unitOfWork.UnitOfWork);

        var admin = CreateAndSaveUser(context, "Admin", "admin12@test.com");
        var createTeamResult = teamService.Create(admin.Id, new CreateTeamDto("Test Team"));
        Assert.True(createTeamResult.IsSuccess);

        var team = context.Teams.FirstOrDefault(t => t.Name == "Test Team");
        Assert.NotNull(team);

        var dto = new GetTeamDto(team.Id);
        var result = teamService.GetAllTeamWithUsers(dto, admin.Id);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal(admin.Id, result.Value.First().UserId);
    }

    [Fact]
    public void GetAllTeamWithUsers_WhenUserNotMember_ReturnsAuthorizationError()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var teamService = new TeamService(unitOfWork.UnitOfWork);

        var admin = CreateAndSaveUser(context, "Admin", "admin13@test.com");
        var createTeamResult = teamService.Create(admin.Id, new CreateTeamDto("Test Team"));
        Assert.True(createTeamResult.IsSuccess);

        var team = context.Teams.FirstOrDefault(t => t.Name == "Test Team");
        Assert.NotNull(team);

        var otherUser = CreateAndSaveUser(context, "Other", "other@test.com");
        var dto = new GetTeamDto(team.Id);
        var result = teamService.GetAllTeamWithUsers(dto, otherUser.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("Authorization", result.Error.Errortype);
        Assert.Contains("not a member", result.Error.Message);
    }

    [Fact]
    public void UpdateMemberRole_HappyPath()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var teamService = new TeamService(unitOfWork.UnitOfWork);

        var admin = CreateAndSaveUser(context, "Admin", "admin14@test.com");
        var createTeamResult = teamService.Create(admin.Id, new CreateTeamDto("Test Team"));
        Assert.True(createTeamResult.IsSuccess);

        var team = context.Teams.FirstOrDefault(t => t.Name == "Test Team");
        Assert.NotNull(team);

        var user = CreateAndSaveUser(context, "User", "user9@test.com");
        var addUserResult = teamService.AddUser(new AddUserInTeamDto(team.Id, user.Id, false), admin.Id);
        Assert.True(addUserResult.IsSuccess);

        var dto = new UpdateTeamRoleDto(team.Id, user.Id);
        var result = teamService.UpdateMemberRole(dto, admin.Id);

        Assert.True(result.IsSuccess);
        var updatedMember = context.TeamMembers.FirstOrDefault(tm => tm.TeamId == team.Id && tm.UserId == user.Id);
        Assert.True(updatedMember.IsAdmin);
    }

    [Fact]
    public void UpdateMemberRole_WhenUserAlreadyAdmin_ReturnsBadRequest()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var teamService = new TeamService(unitOfWork.UnitOfWork);

        var admin = CreateAndSaveUser(context, "Admin", "admin15@test.com");
        var createTeamResult = teamService.Create(admin.Id, new CreateTeamDto("Test Team"));
        Assert.True(createTeamResult.IsSuccess);

        var team = context.Teams.FirstOrDefault(t => t.Name == "Test Team");
        Assert.NotNull(team);

        var otherAdmin = CreateAndSaveUser(context, "OtherAdmin", "otheradmin@test.com");
        var addUserResult = teamService.AddUser(new AddUserInTeamDto(team.Id, otherAdmin.Id, true), admin.Id);
        Assert.True(addUserResult.IsSuccess);

        var dto = new UpdateTeamRoleDto(team.Id, otherAdmin.Id);
        var result = teamService.UpdateMemberRole(dto, admin.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("BadRequest", result.Error.Errortype);
        Assert.Contains("already is admin", result.Error.Message);
    }

    [Fact]
    public void GetAllTasksTeam_HappyPath()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var teamService = new TeamService(unitOfWork.UnitOfWork);

        var admin = CreateAndSaveUser(context, "Admin", "admin16@test.com");
        var createTeamResult = teamService.Create(admin.Id, new CreateTeamDto("Test Team"));
        Assert.True(createTeamResult.IsSuccess);

        var team = context.Teams.FirstOrDefault(t => t.Name == "Test Team");
        Assert.NotNull(team);

        var user = CreateAndSaveUser(context, "User", "user10@test.com");
        var addUserResult = teamService.AddUser(new AddUserInTeamDto(team.Id, user.Id, false), admin.Id);
        Assert.True(addUserResult.IsSuccess);

        var task1 = UserTask.Create("Task 1", admin.Id, team.Id, user.Id).Value;
        var task2 = UserTask.Create("Task 2", admin.Id, team.Id, user.Id).Value;
        context.Tasks.AddRange(task1, task2);
        context.SaveChanges();

        var dto = new GetAllTeamTaskDto(team.Id);
        var result = teamService.GetAllTasksTeam(dto, user.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count());
    }

    [Fact]
    public void GetAllTasksTeam_WhenUserNotMember_ReturnsAuthorizationError()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var teamService = new TeamService(unitOfWork.UnitOfWork);

        var admin = CreateAndSaveUser(context, "Admin", "admin17@test.com");
        var createTeamResult = teamService.Create(admin.Id, new CreateTeamDto("Test Team"));
        Assert.True(createTeamResult.IsSuccess);

        var team = context.Teams.FirstOrDefault(t => t.Name == "Test Team");
        Assert.NotNull(team);

        var otherUser = CreateAndSaveUser(context, "Other", "other2@test.com");
        var dto = new GetAllTeamTaskDto(team.Id);
        var result = teamService.GetAllTasksTeam(dto, otherUser.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("Authorization", result.Error.Errortype);
        Assert.Contains("not a member", result.Error.Message);
    }

    public void Dispose()
    {
        // Ничего не делаем, фикстура сама удалит базу
    }
}