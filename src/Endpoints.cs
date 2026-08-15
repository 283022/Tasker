using Microsoft.AspNetCore.Mvc;
using src.Auth;
using src.InfrastructError;
using src.Service;

namespace src;

public static class Endpoints
{
    public static IEndpointRouteBuilder AddAllEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1");
        var authGroup = group.MapGroup("auth");
        var userGroup = group.MapGroup("users");
        var taskGroup = group.MapGroup("tasks");
        var teamGroup = group.MapGroup("teams");

        ConfigureAuthEndpoints(authGroup);
        ConfigureUserEndpoints(userGroup);
        ConfigureTaskEndpoints(taskGroup);
        ConfigureTeamEndpoints(teamGroup);

        return app;
    }


    // AUTH


    private static void ConfigureAuthEndpoints(RouteGroupBuilder authGroup)
    {
        authGroup.MapPost("register", async (
                [FromBody] CreateUserDto request,
                [FromServices] UserService userService) =>
            {
                var result = await userService.RegisterAsync(request);
                return result.ToHttpResult();
            })
            .WithSummary("Register a new user")
            .WithDescription("Creates a new user account with email and password.");

        authGroup.MapPost("login", async (
                [FromBody] LoginUserDto request,
                HttpContext context,
                [FromServices] UserService userService,
                [FromServices] TokenService tokenService) =>
            {
                var loginResult = await userService.UserLoginAsync(request);
                if (loginResult.IsFailure)
                    return loginResult.ToHttpResult();

                var user = loginResult.Value;
                var (token, cookieOptions) = tokenService.GenerateTokenWithCookie(user.Id);
                context.Response.Cookies.Append(AuthConstants.AuthTokenCookieName, token, cookieOptions);

                return Results.Ok(new { message = "Login successful" });
            })
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(404)
            .ProducesProblem(500)
            .WithSummary("Login user")
            .WithDescription("Authenticates user and sets JWT cookie.");
    }


    // USERS


    private static void ConfigureUserEndpoints(RouteGroupBuilder userGroup)
    {
        userGroup.MapGet("/me", async (
                HttpContext context,
                [FromServices] UserService userService,
                [FromServices] TokenService tokenService) =>
            {
                var userIdResult = tokenService.GetUserIdFromContext(context);
                if (userIdResult.IsFailure)
                    return Results.Unauthorized();

                var user = await userService.GetUserByIdAsync(userIdResult.Value);
                return user.ToHttpResult();
            })
            .RequireAuthorization()
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(404)
            .ProducesProblem(500)
            .WithSummary("Get current user")
            .WithDescription("Returns the authenticated user's profile.");

        userGroup.MapDelete("/{id}", async (
                HttpContext context,
                [FromRoute] Guid id,
                [FromServices] UserService userService,
                [FromServices] TokenService tokenService) =>
            {
                var userIdResult = tokenService.GetUserIdFromContext(context);
                if (userIdResult.IsFailure)
                    return Results.Unauthorized();

                var result = await userService.DeleteUserAsync(new DeleteUserDto(id), userIdResult.Value);
                return result.ToHttpResult();
            })
            .RequireAuthorization()
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(500)
            .WithSummary("Delete user")
            .WithDescription("Deletes a user account. Users can only delete themselves.");
    }


    // TASKS


    private static void ConfigureTaskEndpoints(RouteGroupBuilder taskGroup)
    {
        taskGroup.MapGet("/", async (
                HttpContext context,
                [FromServices] TaskService taskService,
                [FromServices] TokenService tokenService) =>
            {
                var userIdResult = tokenService.GetUserIdFromContext(context);
                if (userIdResult.IsFailure)
                    return Results.Unauthorized();

                var tasks = await taskService.GetTasksAsync(userIdResult.Value);
                return tasks.ToHttpResult();
            })
            .RequireAuthorization()
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(404)
            .ProducesProblem(500)
            .WithSummary("Get user tasks")
            .WithDescription("Returns all tasks assigned to or created by the authenticated user.");

        taskGroup.MapGet("/{id}", async (
                HttpContext context,
                [FromRoute] Guid id,
                [FromServices] TaskService taskService,
                [FromServices] TokenService tokenService) =>
            {
                var userIdResult = tokenService.GetUserIdFromContext(context);
                if (userIdResult.IsFailure)
                    return Results.Unauthorized();

                var task = await taskService.GetTaskByIdAsync(id, userIdResult.Value);
                return task.ToHttpResult();
            })
            .RequireAuthorization()
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(500)
            .WithSummary("Get task by ID")
            .WithDescription("Returns a specific task if the user has permission.");

        taskGroup.MapPost("/", async (
                [FromBody] CreateTaskDto createTaskDto,
                HttpContext context,
                [FromServices] TaskService taskService,
                [FromServices] TokenService tokenService) =>
            {
                var userIdResult = tokenService.GetUserIdFromContext(context);
                if (userIdResult.IsFailure)
                    return Results.Unauthorized();

                var task = await taskService.CreateAsync(createTaskDto, userIdResult.Value);
                return task.ToHttpResult();
            })
            .RequireAuthorization()
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(409)
            .ProducesProblem(500)
            .WithSummary("Create a task")
            .WithDescription("Creates a personal or team task.");

        taskGroup.MapPatch("/{id}/name", async (
                [FromRoute] Guid id,
                [FromBody] RenameTaskDto dto,
                HttpContext context,
                [FromServices] TaskService taskService,
                [FromServices] TokenService tokenService) =>
            {
                var userIdResult = tokenService.GetUserIdFromContext(context);
                if (userIdResult.IsFailure)
                    return Results.Unauthorized();

                var result = await taskService.RenameTaskAsync(new RenameTaskDto(id, dto.Name), userIdResult.Value);
                return result.ToHttpResult();
            })
            .RequireAuthorization()
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(500)
            .WithSummary("Rename task")
            .WithDescription("Changes the name of an existing task.");

        taskGroup.MapPatch("/{id}/assign", async (
                [FromRoute] Guid id,
                [FromBody] AssignTaskDto dto,
                HttpContext context,
                [FromServices] TaskService taskService,
                [FromServices] TokenService tokenService) =>
            {
                var userIdResult = tokenService.GetUserIdFromContext(context);
                if (userIdResult.IsFailure)
                    return Results.Unauthorized();

                var result =
                    await taskService.AssignTaskAsync(new AssignTaskDto(id, dto.AssigneeToUserId), userIdResult.Value);
                return result.ToHttpResult();
            })
            .RequireAuthorization()
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(409)
            .ProducesProblem(500)
            .WithSummary("Assign task")
            .WithDescription("Assigns a team task to a user.");

        taskGroup.MapDelete("/{id}", async (
                HttpContext context,
                [FromRoute] Guid id,
                [FromServices] TaskService taskService,
                [FromServices] TokenService tokenService) =>
            {
                var userIdResult = tokenService.GetUserIdFromContext(context);
                if (userIdResult.IsFailure)
                    return Results.Unauthorized();

                var result = await taskService.DeleteAsync(new DeleteTaskDto(id), userIdResult.Value);
                return result.ToHttpResult();
            })
            .RequireAuthorization()
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(500)
            .WithSummary("Delete task")
            .WithDescription("Deletes a task. Only the author or a team admin can delete it.");
    }


    // TEAMS


    private static void ConfigureTeamEndpoints(RouteGroupBuilder teamGroup)
    {
        teamGroup.MapPost("/", async (
                [FromBody] CreateTeamDto createTeamDto,
                HttpContext context,
                [FromServices] TeamService teamService,
                [FromServices] TokenService tokenService) =>
            {
                var userIdResult = tokenService.GetUserIdFromContext(context);
                if (userIdResult.IsFailure)
                    return Results.Unauthorized();

                var result = await teamService.CreateAsync(userIdResult.Value, createTeamDto);
                return result.ToHttpResult();
            })
            .RequireAuthorization()
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(500)
            .WithSummary("Create a team")
            .WithDescription("Creates a new team and sets the creator as admin.");

        teamGroup.MapGet("/", async (
                HttpContext context,
                [FromServices] TeamService teamService,
                [FromServices] TokenService tokenService) =>
            {
                var userIdResult = tokenService.GetUserIdFromContext(context);
                if (userIdResult.IsFailure)
                    return Results.Unauthorized();

                var teams = await teamService.GetAllTeamWhereUserMemberAsync(userIdResult.Value);
                return teams.ToHttpResult();
            })
            .RequireAuthorization()
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(500)
            .WithSummary("Get user's teams")
            .WithDescription("Returns all teams where the user is a member.");

        teamGroup.MapGet("/{id}", async (
                HttpContext context,
                [FromRoute] Guid id,
                [FromServices] TeamService teamService,
                [FromServices] TokenService tokenService) =>
            {
                var userIdResult = tokenService.GetUserIdFromContext(context);
                if (userIdResult.IsFailure)
                    return Results.Unauthorized();

                var team = await teamService.GetAllTeamWithUsersAsync(new GetTeamDto(id), userIdResult.Value);
                return team.ToHttpResult();
            })
            .RequireAuthorization()
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(500)
            .WithSummary("Get team details")
            .WithDescription("Returns team information with members.");

        teamGroup.MapDelete("/{id}", async (
                HttpContext context,
                [FromRoute] Guid id,
                [FromServices] TeamService teamService,
                [FromServices] TokenService tokenService) =>
            {
                var userIdResult = tokenService.GetUserIdFromContext(context);
                if (userIdResult.IsFailure)
                    return Results.Unauthorized();

                var result = await teamService.DeleteAsync(new DeleteTeamDto(id), userIdResult.Value);
                return result.ToHttpResult();
            })
            .RequireAuthorization()
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(500)
            .WithSummary("Delete team")
            .WithDescription("Deletes a team. Only team admins can delete it.");

        teamGroup.MapGet("/{id}/members", async (
                HttpContext context,
                [FromRoute] Guid id,
                [FromServices] TeamService teamService,
                [FromServices] TokenService tokenService) =>
            {
                var userIdResult = tokenService.GetUserIdFromContext(context);
                if (userIdResult.IsFailure)
                    return Results.Unauthorized();

                var members = await teamService.GetAllTeamWithUsersAsync(new GetTeamDto(id), userIdResult.Value);
                return members.ToHttpResult();
            })
            .RequireAuthorization()
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(500)
            .WithSummary("Get team members")
            .WithDescription("Returns all members of a team.");

        teamGroup.MapPost("/{id}/members", async (
                [FromRoute] Guid id,
                [FromBody] AddUserInTeamDto dto,
                HttpContext context,
                [FromServices] TeamService teamService,
                [FromServices] TokenService tokenService) =>
            {
                var userIdResult = tokenService.GetUserIdFromContext(context);
                if (userIdResult.IsFailure)
                    return Results.Unauthorized();

                var result = await teamService.AddUserAsync(dto, userIdResult.Value);
                return result.ToHttpResult();
            })
            .RequireAuthorization()
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(409)
            .ProducesProblem(500)
            .WithSummary("Add user to team")
            .WithDescription("Adds a user to a team. Only team admins can do this.");

        teamGroup.MapDelete("/{id}/members/{userId}", async (
                HttpContext context,
                [FromRoute] Guid id,
                [FromRoute] Guid userId,
                [FromServices] TeamService teamService,
                [FromServices] TokenService tokenService) =>
            {
                var userIdResult = tokenService.GetUserIdFromContext(context);
                if (userIdResult.IsFailure)
                    return Results.Unauthorized();

                var result =
                    await teamService.DeleteUserAsync(new DeleteUserFromTeamDto(id, userId), userIdResult.Value);
                return result.ToHttpResult();
            })
            .RequireAuthorization()
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(500)
            .WithSummary("Remove user from team")
            .WithDescription("Removes a user from a team. Only team admins can do this.");

        teamGroup.MapPatch("/{id}/members/{userId}/role", async (
                [FromRoute] Guid id,
                [FromRoute] Guid userId,
                [FromBody] UpdateTeamRoleDto dto,
                HttpContext context,
                [FromServices] TeamService teamService,
                [FromServices] TokenService tokenService) =>
            {
                var userIdResult = tokenService.GetUserIdFromContext(context);
                if (userIdResult.IsFailure)
                    return Results.Unauthorized();

                var result = await teamService.UpdateMemberRoleAsync(dto, userIdResult.Value);
                return result.ToHttpResult();
            })
            .RequireAuthorization()
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(409)
            .ProducesProblem(500)
            .WithSummary("Update member role")
            .WithDescription("Makes a user a team admin. Only team admins can do this.");

        teamGroup.MapGet("/{id}/tasks", async (
                HttpContext context,
                [FromRoute] Guid id,
                [FromServices] TeamService teamService,
                [FromServices] TokenService tokenService) =>
            {
                var userIdResult = tokenService.GetUserIdFromContext(context);
                if (userIdResult.IsFailure)
                    return Results.Unauthorized();

                var tasks = await teamService.GetAllTasksTeamAsync(new GetAllTeamTaskDto(id), userIdResult.Value);
                return tasks.ToHttpResult();
            })
            .RequireAuthorization()
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(500)
            .WithSummary("Get team tasks")
            .WithDescription("Returns all tasks associated with a team.");
    }
}