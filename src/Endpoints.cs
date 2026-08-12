using Microsoft.AspNetCore.Mvc;
using src.Auth;

namespace src;

public static class Endpoints
{
    public static IEndpointRouteBuilder AddAllEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/register", async (
            [FromBody] CreateUserDto request,
            [FromServices] UserService userService) =>
        {
            var result = await userService.RegisterAsync(request);
            return result.IsFailure
                ? Results.BadRequest(new { error = result.Error.Message })
                : Results.Created();
        });

        app.MapPost("/auth/login", async (
            [FromBody] LoginUserDto request,
            HttpContext context,
            [FromServices] UserService userService,
            [FromServices] TokenService tokenService) =>
        {
            var loginResult = await userService.UserLoginAsync(request);
            if (loginResult.IsFailure)
                return Results.Unauthorized();

            var user = loginResult.Value;

            var (token, cookieOptions) = tokenService.GenerateTokenWithCookie(user.Id);
            context.Response.Cookies.Append(AuthConstants.AuthTokenCookieName, token, cookieOptions);

            return Results.Ok(new { message = "Login successful" });
        });

        app.MapGet("/users/me", async (
            HttpContext context,
            [FromServices] UserService userService,
            [FromServices] TokenService tokenService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var user = await userService.GetUserByIdAsync(userIdResult.Value);
            return user.IsFailure
                ? Results.BadRequest(new { error = user.Error.Message })
                : Results.Ok(user.Value);
        }).RequireAuthorization();

        app.MapDelete("/users/{id}", async (
            HttpContext context,
            [FromRoute] Guid id,
            [FromServices] UserService userService,
            [FromServices] TokenService tokenService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var result = await userService.DeleteUserAsync(new DeleteUserDto(id), userIdResult.Value);
            return result.IsFailure
                ? Results.BadRequest(new { error = result.Error.Message })
                : Results.Ok();
        }).RequireAuthorization();

        app.MapGet("/tasks", async (
            HttpContext context,
            [FromServices] TaskService taskService,
            [FromServices] TokenService tokenService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var tasks = await taskService.GetTasksAsync(userIdResult.Value);
            return tasks.IsFailure
                ? Results.BadRequest(new { error = tasks.Error.Message })
                : Results.Ok(tasks.Value);
        }).RequireAuthorization();

        app.MapGet("/tasks/{id}", async (
            HttpContext context,
            [FromRoute] Guid id,
            [FromServices] TaskService taskService,
            [FromServices] TokenService tokenService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var task = await taskService.GetTaskByIdAsync(id, userIdResult.Value);
            return task.IsFailure
                ? Results.BadRequest(new { error = task.Error.Message })
                : Results.Ok(task.Value);
        }).RequireAuthorization();

        app.MapPost("/tasks", async (
            [FromBody] CreateTaskDto createTaskDto,
            HttpContext context,
            [FromServices] TaskService taskService,
            [FromServices] TokenService tokenService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var task = await taskService.CreateAsync(createTaskDto, userIdResult.Value);
            return task.IsFailure
                ? Results.BadRequest(new { error = task.Error.Message })
                : Results.Ok(task.Value);
        }).RequireAuthorization();

        app.MapPatch("/tasks/{id}/name", async (
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
            return result.IsFailure
                ? Results.BadRequest(new { error = result.Error.Message })
                : Results.Ok();
        }).RequireAuthorization();

        app.MapPatch("/tasks/{id}/assign", async (
            [FromRoute] Guid id,
            [FromBody] AssignTaskDto dto,
            HttpContext context,
            [FromServices] TaskService taskService,
            [FromServices] TokenService tokenService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var result = await taskService.AssignTaskAsync(new AssignTaskDto(id, dto.AssigneeToUserId), userIdResult.Value);
            return result.IsFailure
                ? Results.BadRequest(new { error = result.Error.Message })
                : Results.Ok();
        }).RequireAuthorization();

        app.MapDelete("/tasks/{id}", async (
            HttpContext context,
            [FromRoute] Guid id,
            [FromServices] TaskService taskService,
            [FromServices] TokenService tokenService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var result = await taskService.DeleteAsync(new DeleteTaskDto(id), userIdResult.Value);
            return result.IsFailure
                ? Results.BadRequest(new { error = result.Error.Message })
                : Results.Ok();
        }).RequireAuthorization();

        app.MapPost("/teams", async (
            [FromBody] CreateTeamDto createTeamDto,
            HttpContext context,
            [FromServices] TeamService teamService,
            [FromServices] TokenService tokenService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var result = await teamService.CreateAsync(userIdResult.Value, createTeamDto);
            return result.IsFailure
                ? Results.BadRequest(new { error = result.Error.Message })
                : Results.Ok();
        }).RequireAuthorization();

        app.MapGet("/teams", async (
            HttpContext context,
            [FromServices] TeamService teamService,
            [FromServices] TokenService tokenService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var teams = await teamService.GetAllTeamWhereUserMemberAsync(userIdResult.Value);
            return teams.IsFailure
                ? Results.BadRequest(new { error = teams.Error.Message })
                : Results.Ok(teams.Value);
        }).RequireAuthorization();

        app.MapGet("/teams/{id}", async (
            HttpContext context,
            [FromRoute] Guid id,
            [FromServices] TeamService teamService,
            [FromServices] TokenService tokenService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var team = await teamService.GetAllTeamWithUsersAsync(new GetTeamDto(id), userIdResult.Value);
            return team.IsFailure
                ? Results.BadRequest(new { error = team.Error.Message })
                : Results.Ok(team.Value);
        }).RequireAuthorization();

        app.MapDelete("/teams/{id}", async (
            HttpContext context,
            [FromRoute] Guid id,
            [FromServices] TeamService teamService,
            [FromServices] TokenService tokenService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var result = await teamService.DeleteAsync(new DeleteTeamDto(id), userIdResult.Value);
            return result.IsFailure
                ? Results.BadRequest(new { error = result.Error.Message })
                : Results.Ok();
        }).RequireAuthorization();

        app.MapGet("/teams/{id}/members", async (
            HttpContext context,
            [FromRoute] Guid id,
            [FromServices] TeamService teamService,
            [FromServices] TokenService tokenService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var members = await teamService.GetAllTeamWithUsersAsync(new GetTeamDto(id), userIdResult.Value);
            return members.IsFailure
                ? Results.BadRequest(new { error = members.Error.Message })
                : Results.Ok(members.Value);
        }).RequireAuthorization();

        app.MapPost("/teams/{id}/members", async (
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
            return result.IsFailure
                ? Results.BadRequest(new { error = result.Error.Message })
                : Results.Ok();
        }).RequireAuthorization();

        app.MapDelete("/teams/{id}/members/{userId}", async (
            HttpContext context,
            [FromRoute] Guid id,
            [FromRoute] Guid userId,
            [FromServices] TeamService teamService,
            [FromServices] TokenService tokenService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var result = await teamService.DeleteUserAsync(new DeleteUserFromTeamDto(id, userId), userIdResult.Value);
            return result.IsFailure
                ? Results.BadRequest(new { error = result.Error.Message })
                : Results.Ok();
        }).RequireAuthorization();

        app.MapPatch("/teams/{id}/members/{userId}/role", async (
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
            return result.IsFailure
                ? Results.BadRequest(new { error = result.Error.Message })
                : Results.Ok();
        }).RequireAuthorization();

        app.MapGet("/teams/{id}/tasks", async (
            HttpContext context,
            [FromRoute] Guid id,
            [FromServices] TeamService teamService,
            [FromServices] TokenService tokenService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var tasks = await teamService.GetAllTasksTeamAsync(new GetAllTeamTaskDto(id), userIdResult.Value);
            return tasks.IsFailure
                ? Results.BadRequest(new { error = tasks.Error.Message })
                : Results.Ok(tasks.Value);
        }).RequireAuthorization();

        return app;
    }
}