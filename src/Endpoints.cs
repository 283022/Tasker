using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using src.Domain;

namespace src;

public static class Endpoints
{
    public static IEndpointRouteBuilder AddAllEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/register", (CreateUserDto request, UserService userService) =>
        {
            if (userService is null)
                return Results.Problem("Service unavailable");

            var result = userService.Register(request);

            if (result.IsFailure)
                return Results.BadRequest(new { error = result.Error.Message });

            return Results.Created();
        });

        app.MapPost("/auth/login", (LoginUserDto request, HttpContext context, UserService userService) =>
        {
            if (userService is null)
                return Results.Problem("Service unavailable");

            var loginResult = userService.Userlogin(request);
            if (loginResult.IsFailure)
                return Results.Unauthorized();

            var user = loginResult.Value;

            // Генерация JWT
            var claims = new List<Claim> { new Claim("id", user.Id.ToString()) };
            var jwt = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(2),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes()),
                    SecurityAlgorithms.HmacSha256));

            var token = new JwtSecurityTokenHandler().WriteToken(jwt);
            context.Response.Cookies.Append("zxc", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(2)
            });

            return Results.Ok(new { message = "Login successful" });
        });

        app.MapGet("/users/me", (HttpContext context, UserService userService) =>
        {
            var userIdResult = GetuserId(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var userId = userIdResult.Value;

            var user = userService.GetUserById(userId);

            return user.IsFailure
                ? Results.BadRequest(new { error = user.Error.Message })
                : Results.Ok(user);
        }).RequireAuthorization();

        /*
        app.MapPatch("/users/me/name", UpdateName).;
        app.MapPatch("/users/me/password", ChangePassword);
        app.MapPatch("/users/me/email", ChangeEmail);*/

        app.MapDelete("/users/{id}", (HttpContext context, Guid id, UserService userService) =>
        {
            var userIdResult = GetuserId(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var userId = userIdResult.Value;
            var user = userService.DeleteUser(new DeleteUserDto(id), userId);

            if (user.IsFailure)
                return Results.BadRequest(new Error("Validation", "zxcnax"));
            return Results.Ok(user);
        }).RequireAuthorization();

        app.MapGet("/tasks", (HttpContext context, TaskService taskService) =>
        {
            var userIdResult = GetuserId(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var userId = userIdResult.Value;
            var tasks = taskService.GetTasks(userId);
            if (tasks.IsFailure)
                return Results.BadRequest(new Error(tasks.Error.Errortype, tasks.Error.Message));
            return Results.Ok(tasks.Value);
        }).RequireAuthorization();

        app.MapGet("/tasks/{id}", (HttpContext context, 
            TaskService taskService, Guid id) =>
        {
            var userIdResult = GetuserId(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var userId = userIdResult.Value;

            var task = taskService.GetTaskById(id,userId);
            if (task.IsFailure)
                return Results.BadRequest(new Error("Validation", "zxcnax"));

            return Results.Ok(task.Value);
        }).RequireAuthorization();

        app.MapPost("/tasks", (CreateTaskDto createTaskDto,
            HttpContext context, TaskService taskService) =>
        {
            var userIdResult = GetuserId(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var userId = userIdResult.Value;
            var task = taskService.Create(createTaskDto, userId);
            if (task.IsFailure)
                return Results.BadRequest(new Error(task.Error.Errortype, task.Error.Message));
            return Results.Ok(task.Value);
        }).RequireAuthorization().RequireAuthorization();

        /*
        app.MapPut("/tasks/{id}", UpdateTask); // полное обновление
        app.MapPatch("/tasks/{id}", PatchTask); // частичное обновление
        */


        app.MapDelete("/tasks/{id}", (HttpContext context,
            Guid id, TaskService taskService) =>
        {
            var userIdResult = GetuserId(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var userId = userIdResult.Value;
            var task = taskService.Delete(new DeleteTaskDto(id), userId);

            return task.IsFailure
                ? Results.BadRequest(new Error(task.Error.Errortype, task.Error.Message))
                : Results.Ok();
        }).RequireAuthorization();

        app.MapPost("/teams", (CreateTeamDto createTeamDto,
            HttpContext context, TeamService teamService) =>
        {
            var userIdResult = GetuserId(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var userId = userIdResult.Value;
            var team = teamService.Create(userId, createTeamDto);

            if (team.IsFailure)
                return Results.BadRequest(new Error(team.Error.Errortype, team.Error.Message));

            return Results.Ok();
        }).RequireAuthorization();

        //GetUserTeams
        app.MapGet("/teams", (HttpContext context, TeamService teamService) =>
        {
            var userIdResult = GetuserId(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var userId = userIdResult.Value;
            var teams = teamService.GetAllTeamWhereUserMember(userId);
            if (teams.IsFailure)
                return Results.BadRequest(new Error(teams.Error.Errortype, teams.Error.Message));
            return Results.Ok(teams.Value);
        }).RequireAuthorization();
        //GetTeamById
        app.MapGet("/teams/{id}", (HttpContext context,
            TeamService teamService, GetTeamDto dto) =>
        {
            var userIdResult = GetuserId(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var userId = userIdResult.Value;

            var team = teamService.GetAllTeamWithUsers(dto, userId);

            if (team.IsFailure)
                return Results.BadRequest(new Error(team.Error.Errortype, team.Error.Message));
            return Results.Ok(team.Value);
        }).RequireAuthorization();
        
        
        /*app.MapPut("/teams/{id}", UpdateTeam);
        */

        //DeleteTeam
        app.MapDelete("/teams/{id}", (HttpContext context,
            TeamService teamService, DeleteTeamDto dto) =>
        {
            var userIdResult = GetuserId(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var userId = userIdResult.Value;

            var team = teamService.Delete(dto, userId);

            if (team.IsFailure)
                return Results.BadRequest(new Error(team.Error.Errortype, team.Error.Message));

            return Results.Ok();
        }).RequireAuthorization();

        //GetTeamMembers
        app.MapGet("/teams/{id}/members", (HttpContext context,
            TeamService teamService) =>
        {
            var userIdResult = GetuserId(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var userId = userIdResult.Value;
            var teams = teamService.GetAllTeamWhereUserMember(userId);
            if (teams.IsFailure)
                return Results.BadRequest(new Error(teams.Error.Errortype, teams.Error.Message));
            return Results.Ok(teams.Value);
        }).RequireAuthorization();
        //AddTeamMember
        app.MapPost("/teams/{id}/members", (HttpContext context,
            TeamService teamService, AddUserInTeamDto dto) =>
        {
            var userIdResult = GetuserId(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var userId = userIdResult.Value;
            var team = teamService.AddUser(dto, userId);

            if (team.IsFailure)
                return Results.BadRequest(new Error(team.Error.Errortype, team.Error.Message));
            return Results.Ok();
        }).RequireAuthorization();

        //RemoveTeamMember
        app.MapDelete("/teams/{id}/members/{userId}", (HttpContext context,
            TeamService teamService, DeleteUserFromTeamDto dto) =>
        {
            var userIdResult = GetuserId(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var userId = userIdResult.Value;
            var team = teamService.DeleteUser(dto, userId);

            if (team.IsFailure)
                return Results.BadRequest(new Error(team.Error.Errortype, team.Error.Message));
            return Results.Ok();
        }).RequireAuthorization();

        //UpdateMemberRole
        app.MapPatch("/teams/{id}/members/{userId}/role", (HttpContext context,
            TeamService teamService, UpdateTeamRoleDto dto) =>
        {
            var userIdResult = GetuserId(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var userId = userIdResult.Value;
            var team = teamService.UpdateMemberRole(dto, userId);

            if (team.IsFailure)
                return Results.BadRequest(new Error(team.Error.Errortype, team.Error.Message));
            return Results.Ok();
        }).RequireAuthorization();

        //GetTeamTasks
        app.MapGet("/teams/{id}/tasks", (HttpContext context,
            TeamService teamService, GetAllTeamTaskDto dto) =>
        {
            var userIdResult = GetuserId(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var userId = userIdResult.Value;
            var team = teamService.GetAllTasksTeam(dto, userId);

            if (team.IsFailure)
                return Results.BadRequest(new Error(team.Error.Errortype, team.Error.Message));

            return Results.Ok();
        }).RequireAuthorization();


        return app;
    }


    private static Result<Guid> GetuserId(HttpContext context)
    {
        var userIdClaim = context.Request.Cookies["zxc"] ??
                          context.User.FindFirst("id")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return new Error("Auth", "User not authenticated");

        return Result<Guid>.Success(userId);
    }
}