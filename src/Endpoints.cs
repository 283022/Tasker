using Microsoft.AspNetCore.Mvc;
using src.Auth;

namespace src;

public static class Endpoints
{
    public static IEndpointRouteBuilder AddAllEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/register",  (
            [FromBody] CreateUserDto request,
            [FromServices] UserService userService) =>
        {
            var result = userService.Register(request);
            return result.IsFailure
                ? Results.BadRequest(new { error = result.Error.Message })
                : Results.Created();
        });

        app.MapPost("/auth/login",  (
            [FromBody] LoginUserDto request,
            HttpContext context,
            [FromServices] UserService userService,
            [FromServices] TokenService tokenService) =>
        {
            var loginResult = userService.Userlogin(request);
            if (loginResult.IsFailure)
                return Results.Unauthorized();

            var user = loginResult.Value;

            var (token, cookieOptions) = tokenService.GenerateTokenWithCookie(user.Id);
            context.Response.Cookies.Append(AuthConstants.AuthTokenCookieName, token, cookieOptions);

            return Results.Ok(new { message = "Login successful" });
        });


        app.MapGet("/users/me",  (
            HttpContext context,
            [FromServices] UserService userService,
            [FromServices] TokenService tokenService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var user = userService.GetUserById(userIdResult.Value);
            return user.IsFailure
                ? Results.BadRequest(new { error = user.Error.Message })
                : Results.Ok(user.Value);
        }).RequireAuthorization();

        app.MapDelete("/users/{id}",  (
            HttpContext context,
            [FromRoute] Guid id,
            [FromServices] UserService userService,
            [FromServices] TokenService tokenService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var result = userService.DeleteUser(new DeleteUserDto(id), userIdResult.Value);
            return result.IsFailure
                ? Results.BadRequest(new { error = result.Error.Message })
                : Results.Ok();
        }).RequireAuthorization();


        app.MapGet("/tasks",  (
            HttpContext context,
            [FromServices] TaskService taskService,
            [FromServices] TokenService tokenService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var tasks = taskService.GetTasks(userIdResult.Value);
            return tasks.IsFailure
                ? Results.BadRequest(new { error = tasks.Error.Message })
                : Results.Ok(tasks.Value);
        }).RequireAuthorization();

        app.MapGet("/tasks/{id}",  (
            HttpContext context,
            [FromRoute] Guid id,
            [FromServices] TaskService taskService,
            [FromServices] TokenService tokenService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var task = taskService.GetTaskById(id, userIdResult.Value);
            return task.IsFailure
                ? Results.BadRequest(new { error = task.Error.Message })
                : Results.Ok(task.Value);
        }).RequireAuthorization();

        app.MapPost("/tasks",  (
            [FromBody] CreateTaskDto createTaskDto,
            HttpContext context,
            [FromServices] TaskService taskService,
            [FromServices] TokenService tokenService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var task = taskService.Create(createTaskDto, userIdResult.Value);
            return task.IsFailure
                ? Results.BadRequest(new { error = task.Error.Message })
                : Results.Ok(task.Value);
        }).RequireAuthorization();

        app.MapPatch("/tasks/{id}/name",  (
            [FromRoute] Guid id,
            [FromBody] RenameTaskDto dto,
            HttpContext context,
            [FromServices] TaskService taskService,
            [FromServices] TokenService tokenService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var result = taskService.RenameTask(new RenameTaskDto(id, dto.Name), userIdResult.Value);
            return result.IsFailure
                ? Results.BadRequest(new { error = result.Error.Message })
                : Results.Ok();
        }).RequireAuthorization();

        app.MapPatch("/tasks/{id}/assign",  (
            [FromRoute] Guid id,
            [FromBody] AssignTaskDto dto,
            HttpContext context,
            [FromServices] TaskService taskService,
            [FromServices] TokenService tokenService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var result = taskService.AssignTask(new AssignTaskDto(id, dto.AssigneeToUserId), userIdResult.Value);
            return result.IsFailure
                ? Results.BadRequest(new { error = result.Error.Message })
                : Results.Ok();
        }).RequireAuthorization();

        app.MapDelete("/tasks/{id}",  (
            HttpContext context,
            [FromRoute] Guid id,
            [FromServices] TaskService taskService,
            [FromServices] TokenService tokenService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var result = taskService.Delete(new DeleteTaskDto(id), userIdResult.Value);
            return result.IsFailure
                ? Results.BadRequest(new { error = result.Error.Message })
                : Results.Ok();
        }).RequireAuthorization();


        app.MapPost("/teams",  (
            [FromBody] CreateTeamDto createTeamDto,
            HttpContext context,
            [FromServices] TeamService teamService,
            [FromServices] TokenService tokenService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var result = teamService.Create(userIdResult.Value, createTeamDto);
            return result.IsFailure
                ? Results.BadRequest(new { error = result.Error.Message })
                : Results.Ok();
        }).RequireAuthorization();

        app.MapGet("/teams",  (
            HttpContext context,
            [FromServices] TeamService teamService,
            [FromServices] TokenService tokenService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var teams = teamService.GetAllTeamWhereUserMember(userIdResult.Value);
            return teams.IsFailure
                ? Results.BadRequest(new { error = teams.Error.Message })
                : Results.Ok(teams.Value);
        }).RequireAuthorization();

        app.MapGet("/teams/{id}",  (
            HttpContext context,
            [FromRoute] Guid id,
            [FromServices] TeamService teamService,
            [FromServices] TokenService tokenService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var team = teamService.GetAllTeamWithUsers(new GetTeamDto(id), userIdResult.Value);
            return team.IsFailure
                ? Results.BadRequest(new { error = team.Error.Message })
                : Results.Ok(team.Value);
        }).RequireAuthorization();

        app.MapDelete("/teams/{id}",  (
            HttpContext context,
            [FromRoute] Guid id,
            [FromServices] TeamService teamService,
            [FromServices] TokenService tokenService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var result = teamService.Delete(new DeleteTeamDto(id), userIdResult.Value);
            return result.IsFailure
                ? Results.BadRequest(new { error = result.Error.Message })
                : Results.Ok();
        }).RequireAuthorization();

        app.MapGet("/teams/{id}/members",  (
            HttpContext context,
            [FromRoute] Guid id,
            [FromServices] TeamService teamService,
            [FromServices] TokenService tokenService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var members = teamService.GetAllTeamWithUsers(new GetTeamDto(id), userIdResult.Value);
            return members.IsFailure
                ? Results.BadRequest(new { error = members.Error.Message })
                : Results.Ok(members.Value);
        }).RequireAuthorization();

        app.MapPost("/teams/{id}/members",  (
            [FromRoute] Guid id,
            [FromBody] AddUserInTeamDto dto,
            HttpContext context,
            [FromServices] TeamService teamService,
            [FromServices] TokenService tokenService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var result = teamService.AddUser(dto, userIdResult.Value);
            return result.IsFailure
                ? Results.BadRequest(new { error = result.Error.Message })
                : Results.Ok();
        }).RequireAuthorization();

        app.MapDelete("/teams/{id}/members/{userId}",  (
            HttpContext context,
            [FromRoute] Guid id,
            [FromRoute] Guid userId,
            [FromServices] TeamService teamService,
            [FromServices] TokenService tokenService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var result = teamService.DeleteUser(new DeleteUserFromTeamDto(id, userId), userIdResult.Value);
            return result.IsFailure
                ? Results.BadRequest(new { error = result.Error.Message })
                : Results.Ok();
        }).RequireAuthorization();

        app.MapPatch("/teams/{id}/members/{userId}/role",  (
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

            var result = teamService.UpdateMemberRole(dto, userIdResult.Value);
            return result.IsFailure
                ? Results.BadRequest(new { error = result.Error.Message })
                : Results.Ok();
        }).RequireAuthorization();

        app.MapGet("/teams/{id}/tasks",  (
            HttpContext context,
            [FromRoute] Guid id,
            [FromServices] TeamService teamService,
            [FromServices] TokenService tokenService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var tasks = teamService.GetAllTasksTeam(new GetAllTeamTaskDto(id), userIdResult.Value);
            return tasks.IsFailure
                ? Results.BadRequest(new { error = tasks.Error.Message })
                : Results.Ok(tasks.Value);
        }).RequireAuthorization();

        return app;
    }
}