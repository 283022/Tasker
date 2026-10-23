using Microsoft.AspNetCore.Mvc;
using src.Auth;
using src.Domain;
using src.InfrastructError;
using src.Service;

namespace src;


public static class Endpoints
{

    public static IEndpointRouteBuilder AddAllEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1");
        var authGroup = group.MapGroup("auth").WithTags("auth");
        var userGroup = group.MapGroup("users").WithTags("users");
        var taskGroup = group.MapGroup("tasks").WithTags("tasks");
        var teamGroup = group.MapGroup("teams").WithTags("teams");
        
        ConfigureAuthEndpoints(authGroup);
        ConfigureUserEndpoints(userGroup);
        ConfigureTaskEndpoints(taskGroup);
        ConfigureTeamEndpoints(teamGroup);
        
        return app;
    }

    
    // AUTH ENDPOINTS


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
            .WithDescription(
                "Creates a new user account with email and password. Password must be at least 8 characters long " +
                "and contain letters and digits.")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(409)
            .ProducesProblem(500);

       
        
        authGroup.MapPost("login", async (
                [FromBody] LoginUserDto request,
                HttpContext context,
                [FromServices] UserService userService,
                [FromServices] SessionService sessionService,
                [FromServices] TokenService tokenService,
                CancellationToken cancellationToken) =>
            {
                var loginResult = await userService.UserLoginAsync(request);

                if (loginResult.IsFailure)
                    return loginResult.ToHttpResult();

                var user = loginResult.Value;

                var sessionResult = await sessionService.CreateAsync(
                    user.Id,
                    cancellationToken);

                if (sessionResult.IsFailure)
                    return sessionResult.ToHttpResult();

                var tokens = sessionResult.Value;
                var cookieOptions = CreateCookieOptions(context);

                context.Response.Cookies.Append(
                    AuthConstants.AuthTokenCookieName,
                    tokens.AccessToken,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = context.Request.IsHttps,
                        SameSite = SameSiteMode.Lax,
                        Path = "/",
                        Expires = DateTimeOffset.UtcNow.AddMinutes(
                            tokenService.GetTokenExpiryMinutes())
                    });

                context.Response.Cookies.Append(
                    AuthConstants.RefreshTokenCookieName,
                    tokens.RefreshToken,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = context.Request.IsHttps,
                        SameSite = SameSiteMode.Lax,
                        Path = "/",
                        Expires = tokens.RefreshTokenExpiresAt
                    });

                return Results.Ok(new { message = "Login successful" });
            })
            .WithSummary("Login user")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(401);
        
        authGroup.MapPost("refresh", async (
                HttpContext context,
                [FromServices] SessionService sessionService,
                [FromServices] TokenService tokenService,
                CancellationToken cancellationToken) =>
            {
                context.Request.Cookies.TryGetValue(AuthConstants.RefreshTokenCookieName, out var refreshToken);
             
                if (string.IsNullOrWhiteSpace(refreshToken))
                    return Results.Unauthorized();

                var refreshResult = await sessionService.RefreshAsync(
                    refreshToken,
                    cancellationToken);

                if (refreshResult.IsFailure)
                    return refreshResult.ToHttpResult();

                var tokens = refreshResult.Value;

                context.Response.Cookies.Append(
                    AuthConstants.AuthTokenCookieName,
                    tokens.AccessToken,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = context.Request.IsHttps,
                        SameSite = SameSiteMode.Lax,
                        Path = "/",
                        Expires = DateTimeOffset.UtcNow.AddMinutes(
                            tokenService.GetTokenExpiryMinutes())
                    });

                context.Response.Cookies.Append(
                    AuthConstants.RefreshTokenCookieName,
                    tokens.RefreshToken,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = context.Request.IsHttps,
                        SameSite = SameSiteMode.Lax,
                        Path = "/",
                        Expires = tokens.RefreshTokenExpiresAt
                    });

                return Results.NoContent();
            })
            .WithSummary("Refresh authentication tokens")
            .Produces(204)
            .Produces(401);

        authGroup.MapPost("logout", async (
                HttpContext context,
                [FromServices] SessionService sessionService,
                CancellationToken cancellationToken) =>
            {
                context.Request.Cookies.TryGetValue(AuthConstants.RefreshTokenCookieName, out var refreshToken);
                
                var result = await sessionService.LogoutAsync(
                    refreshToken,
                    cancellationToken);

                if (result.IsFailure)
                    return result.ToHttpResult();

                var cookieOptions = CreateCookieOptions(context);

                context.Response.Cookies.Delete(
                    AuthConstants.AuthTokenCookieName,
                    cookieOptions);

                context.Response.Cookies.Delete(
                    AuthConstants.RefreshTokenCookieName,
                    cookieOptions);

                return Results.NoContent();
            })
            .WithSummary("Logout user")
            .Produces(204);
        
        
    }
    
    private static CookieOptions CreateCookieOptions(
        HttpContext context)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        };
    }
    
    
    // USER ENDPOINTS
    

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
            .WithDescription("Returns the authenticated user's profile. Requires authentication.");

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
        
        userGroup.MapPatch("/changeDisplayName", async (
            HttpContext context,
            string displayName,
            UserService userService,
            [FromServices] TokenService tokenService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();
            
            var result = await userService.ChangeUserDisplayName(
                userIdResult.Value,
                displayName);

            return result.ToHttpResult();
        });
    }

    
    // TASK ENDPOINTS
    

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

    
    // TEAM ENDPOINTS
    

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

                var result = await teamService.AddUserAsync(dto,id, userIdResult.Value);
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
                    await teamService.DeleteUserAsync(new DeleteUserFromTeamDto(userId), id, userIdResult.Value);
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
                HttpContext context,
                [FromServices] TeamService teamService,
                [FromServices] TokenService tokenService) =>
            {
                var userIdResult = tokenService.GetUserIdFromContext(context);
                if (userIdResult.IsFailure)
                    return Results.Unauthorized();

                var result = await teamService.UpdateMemberRoleAsync(id,userId, userIdResult.Value);
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

        teamGroup.MapPost("/{id}/joinInTeam", async
        (HttpContext context, [FromRoute] Guid id,
            [FromServices] TokenService tokenService, [FromServices] TeamJoinRequestService teamJoinService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();

            var result = await teamJoinService.CreateAsync(id, userIdResult.Value);
            return result.ToHttpResult();
        });

        teamGroup.MapGet("/{id}/joinrequest", 
            async ([FromRoute] Guid id, HttpContext context, 
                [FromServices] TeamJoinRequestService teamJoinRequestService, [FromServices] TokenService tokenService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();
            var result = await teamJoinRequestService.GetAllRequest(id,userIdResult.Value);
            return result.ToHttpResult();
        });

        teamGroup.MapPatch("{id}/jounrequest/{joinRequestId}/Approve", async
        (HttpContext context, [FromRoute] Guid id, [FromRoute] Guid joinRequestId,
            [FromServices] TeamJoinRequestService teamJoinRequestService, [FromServices] TokenService tokenService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();
            var result = await teamJoinRequestService.ApproveAsync(id, userIdResult.Value);
            return result.ToHttpResult();
        });
        
        
        teamGroup.MapPatch("{id}/jounrequest/{joinRequestId}/Reject", async
        (HttpContext context, [FromRoute] Guid id, [FromRoute] Guid joinRequestId,
            [FromServices] TeamJoinRequestService teamJoinRequestService, [FromServices] TokenService tokenService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();
            var result = await teamJoinRequestService.RejectAsync(id, userIdResult.Value);
            return result.ToHttpResult();
        });
        
        teamGroup.MapPatch("{id}/changeDisplayName", async 
        ([FromRoute] Guid id,
            [FromBody] string DisplayName,
            [FromServices] TeamService teamService,
            [FromServices] TokenService tokenService,
            HttpContext context) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();
            var result = await teamService.ChangeIdName(
                userIdResult.Value, id,
                DisplayName);

            return result.ToHttpResult();
        });
    }
    
}