using Microsoft.EntityFrameworkCore;
using src;
using src.Db;
using src.Domain.Entities;
using src.Domain.ValueObjects;
using src.Security;
using src.Service;

namespace TestProject1.Integration_tests;

public class TaskServiceTests : IClassFixture<TestDatabaseFixture>, IDisposable
{
    private readonly TestDatabaseFixture _fixture;

    public TaskServiceTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private User CreateAndSaveUser(ApplicationContext context, string name = "User", string email = null,
        string password = "Password123")
    {
        email ??= $"user_{Guid.NewGuid():N}@test.com";
        var user = User.Create(name, Email.Create(email).Value, Password.Create(password, new Passwordhasher()).Value).Value;
        context.Users.Add(user);
        context.SaveChanges();
        return user;
    }

    private Team CreateAndSaveTeam(ApplicationContext context, Guid adminId, string name = "Test Team")
    {
        var team = Team.Create(name).Value;
        context.Teams.Add(team);
        context.SaveChanges();

        var teamMember = TeamMember.Create(team.Id, adminId, true).Value;
        context.TeamMembers.Add(teamMember);
        context.SaveChanges();

        return team;
    }

    private void AddUserToTeam(ApplicationContext context, Guid userId, Guid teamId)
    {
        var teamMember = TeamMember.Create(teamId, userId, false).Value;
        context.TeamMembers.Add(teamMember);
        context.SaveChanges();
    }

    [Fact]
    public async Task CreateTask_PersonalTask_HappyPath()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var taskService = new TaskService(unitOfWork.UnitOfWork);

        var user = CreateAndSaveUser(context);
        var dto = new CreateTaskDto("My Personal Task", null, null);

        //  Асинхронный вызов
        var result = await taskService.CreateAsync(dto, user.Id);

        Assert.True(result.IsSuccess);
        var task = await context.Tasks.FirstOrDefaultAsync(t => t.Name == "My Personal Task");
        Assert.NotNull(task);
        Assert.Equal(user.Id, task.CreateByUserID);
        Assert.Equal(user.Id, task.AssignedToUserID);
        Assert.Null(task.TeamID);
    }

    [Fact]
    public async Task CreateTask_TeamTask_AsAdmin_HappyPath()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var taskService = new TaskService(unitOfWork.UnitOfWork);

        var admin = CreateAndSaveUser(context, "Admin", "admin@test.com");
        var team = CreateAndSaveTeam(context, admin.Id, "Test Team");
        var user = CreateAndSaveUser(context, "User", "user@test.com");
        AddUserToTeam(context, user.Id, team.Id);

        var dto = new CreateTaskDto("Team Task", user.Id, team.Id);
        var result = await taskService.CreateAsync(dto, admin.Id);

        Assert.True(result.IsSuccess);
        var task = await context.Tasks.FirstOrDefaultAsync(t => t.Name == "Team Task");
        Assert.NotNull(task);
        Assert.Equal(admin.Id, task.CreateByUserID);
        Assert.Equal(user.Id, task.AssignedToUserID);
        Assert.Equal(team.Id, task.TeamID);
    }

    [Fact]
    public async Task CreateTask_TeamTask_AsNonAdmin_ReturnsAuthorizationError()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var taskService = new TaskService(unitOfWork.UnitOfWork);

        var admin = CreateAndSaveUser(context, "Admin", "admin2@test.com");
        var team = CreateAndSaveTeam(context, admin.Id, "Test Team");
        var nonAdmin = CreateAndSaveUser(context, "NonAdmin", "nonadmin@test.com");
        AddUserToTeam(context, nonAdmin.Id, team.Id);
        var user = CreateAndSaveUser(context, "User", "user2@test.com");
        AddUserToTeam(context, user.Id, team.Id);

        var dto = new CreateTaskDto("Team Task", user.Id, team.Id);
        var result = await taskService.CreateAsync(dto, nonAdmin.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("Authorization", result.Error.Errortype);
        Assert.Contains("admin", result.Error.Message);
    }

    [Fact]
    public async Task CreateTask_WithInvalidCombination_ReturnsValidationError()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var taskService = new TaskService(unitOfWork.UnitOfWork);

        var user = CreateAndSaveUser(context);
        var team = CreateAndSaveTeam(context, user.Id);

        var dto = new CreateTaskDto("Task", null, team.Id);
        var result = await taskService.CreateAsync(dto, user.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Errortype);
        Assert.Contains("AssignedToUserID and TeamID", result.Error.Message);
    }

    [Fact]
    public async Task DeleteTask_AsAuthor_HappyPath()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var taskService = new TaskService(unitOfWork.UnitOfWork);

        var user = CreateAndSaveUser(context);
        var task = UserTask.Create("Task to delete", user.Id).Value;
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        var result = await taskService.DeleteAsync(new DeleteTaskDto(task.Id), user.Id);

        Assert.True(result.IsSuccess);
        var taskAfter = await context.Tasks.FirstOrDefaultAsync(t => t.Id == task.Id);
        Assert.Null(taskAfter);
    }

    [Fact]
    public async Task DeleteTask_AsNonAuthor_ReturnsAuthorizationError()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var taskService = new TaskService(unitOfWork.UnitOfWork);

        var author = CreateAndSaveUser(context, "Author");
        var otherUser = CreateAndSaveUser(context, "OtherUser");
        var task = UserTask.Create("Task to delete", author.Id).Value;
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        var result = await taskService.DeleteAsync(new DeleteTaskDto(task.Id), otherUser.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("Authorization", result.Error.Errortype);
        Assert.Contains("Can't delete", result.Error.Message);
    }

    [Fact]
    public async Task GetTasks_ReturnsUserTasks()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var taskService = new TaskService(unitOfWork.UnitOfWork);

        var user = CreateAndSaveUser(context);
        var task1 = UserTask.Create("Task 1", user.Id).Value;
        var task2 = UserTask.Create("Task 2", user.Id).Value;
        context.Tasks.AddRange(task1, task2);
        await context.SaveChangesAsync();

        var result = await taskService.GetTasksAsync(user.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count());
    }

    [Fact]
    public async Task GetTaskById_HappyPath()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var taskService = new TaskService(unitOfWork.UnitOfWork);

        var user = CreateAndSaveUser(context);
        var task = UserTask.Create("Task", user.Id).Value;
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        var result = await taskService.GetTaskByIdAsync(task.Id, user.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(task.Id, result.Value.Id);
        Assert.Equal("Task", result.Value.Name);
    }

    [Fact]
    public async Task GetTaskById_WhenUserCannotManage_ReturnsAuthorizationError()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var taskService = new TaskService(unitOfWork.UnitOfWork);

        var author = CreateAndSaveUser(context, "Author");
        var otherUser = CreateAndSaveUser(context, "OtherUser");
        var task = UserTask.Create("Task", author.Id).Value;
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        var result = await taskService.GetTaskByIdAsync(task.Id, otherUser.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Errortype);
        Assert.Contains("Can't manage", result.Error.Message);
    }

    [Fact]
    public async Task RenameTask_HappyPath()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var taskService = new TaskService(unitOfWork.UnitOfWork);

        var user = CreateAndSaveUser(context);
        var task = UserTask.Create("Old Name", user.Id).Value;
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        var dto = new RenameTaskDto(task.Id, "New Name");
        var result = await taskService.RenameTaskAsync(dto, user.Id);

        Assert.True(result.IsSuccess);
        var updatedTask = await context.Tasks.FirstOrDefaultAsync(t => t.Id == task.Id);
        Assert.Equal("New Name", updatedTask.Name);
    }

    [Fact]
    public async Task AssignTask_AsAdmin_HappyPath()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var taskService = new TaskService(unitOfWork.UnitOfWork);

        var admin = CreateAndSaveUser(context, "Admin", "admin3@test.com");
        var team = CreateAndSaveTeam(context, admin.Id, "Test Team");
        var user = CreateAndSaveUser(context, "User", "user3@test.com");
        AddUserToTeam(context, user.Id, team.Id);

        var task = UserTask.Create("Task", admin.Id, team.Id, null).Value;
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        var dto = new AssignTaskDto(task.Id, user.Id);
        var result = await taskService.AssignTaskAsync(dto, admin.Id);

        Assert.True(result.IsSuccess);
        var updatedTask = await context.Tasks.FirstOrDefaultAsync(t => t.Id == task.Id);
        Assert.Equal(user.Id, updatedTask.AssignedToUserID);
    }

    [Fact]
    public async Task AssignTask_AsNonAdmin_ReturnsAuthorizationError()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var taskService = new TaskService(unitOfWork.UnitOfWork);

        var admin = CreateAndSaveUser(context, "Admin", "admin4@test.com");
        var team = CreateAndSaveTeam(context, admin.Id, "Test Team");
        var nonAdmin = CreateAndSaveUser(context, "NonAdmin", "nonadmin2@test.com");
        AddUserToTeam(context, nonAdmin.Id, team.Id);
        var user = CreateAndSaveUser(context, "User", "user4@test.com");
        AddUserToTeam(context, user.Id, team.Id);

        var task = UserTask.Create("Task", nonAdmin.Id, team.Id, null).Value;
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        var dto = new AssignTaskDto(task.Id, user.Id);
        var result = await taskService.AssignTaskAsync(dto, nonAdmin.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("Authorization", result.Error.Errortype);
        Assert.Contains("admin", result.Error.Message);
    }

    [Fact]
    public async Task AssignTask_UserNotInTeam_ReturnsValidationError()
    {
        using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWorkEFcoreTest(context);
        var taskService = new TaskService(unitOfWork.UnitOfWork);

        var admin = CreateAndSaveUser(context, "Admin", "admin5@test.com");
        var team = CreateAndSaveTeam(context, admin.Id, "Test Team");
        var userNotInTeam = CreateAndSaveUser(context, "UserNotInTeam", "user5@test.com");

        var task = UserTask.Create("Task", admin.Id, team.Id, null).Value;
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        var dto = new AssignTaskDto(task.Id, userNotInTeam.Id);
        var result = await taskService.AssignTaskAsync(dto, admin.Id);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Errortype);
        Assert.Contains("member", result.Error.Message);
    }

    public void Dispose()
    {
        // Ничего не делаем
    }
}