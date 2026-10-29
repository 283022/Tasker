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
        var authGroup = group.MapGroup("auth").WithTags("auth");
        var userGroup = group.MapGroup("users").WithTags("users");
        var taskGroup = group.MapGroup("tasks").WithTags("tasks");
        var teamGroup = group.MapGroup("teams").WithTags("teams");
        var searchGroup = group.MapGroup("search").WithTags("search");
        var accountGroup = group.MapGroup("accounts").WithTags("accounts");
        var notificationGroup = group.MapGroup("notifications").WithTags("notifications");
            
        ConfigureAuthEndpoints(authGroup);
        ConfigureUserEndpoints(userGroup);
        ConfigureTaskEndpoints(taskGroup);
        ConfigureTeamEndpoints(teamGroup);
        ConfigureSearchEndpoints(searchGroup);
        ConfigureAccountEndpoints(accountGroup);
        ConfigureNotificationEndpoints(notificationGroup);
        
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
        
        taskGroup.MapPatch("/{id:guid}/status", async (
                [FromRoute] Guid id,
                [FromBody] ChangeTaskStatusDto dto,
                HttpContext context,
                [FromServices] TaskService taskService,
                [FromServices] TokenService tokenService,
                CancellationToken cancellationToken) =>
            {
                var userIdResult = tokenService.GetUserIdFromContext(context);

                if (userIdResult.IsFailure)
                    return Results.Unauthorized();

                var result = await taskService.ChangeStatusAsync(
                    id,
                    userIdResult.Value,
                    dto.Status,
                    cancellationToken);

                return result.ToHttpResult();
            })
            .RequireAuthorization()
            .WithSummary("Change task status")
            .WithDescription("Changes the status of a task.");
        
        taskGroup.MapPatch("/{id:guid}/description", async (
                [FromRoute] Guid id,
                [FromBody] ChangeTaskMarkdownDto dto,
                HttpContext context,
                [FromServices] TaskService taskService,
                [FromServices] TokenService tokenService,
                CancellationToken cancellationToken) =>
            {
                var userIdResult = tokenService.GetUserIdFromContext(context);

                if (userIdResult.IsFailure)
                    return Results.Unauthorized();

                var result = await taskService.ChangeMarkdownTextAsync(
                    id,
                    userIdResult.Value,
                    dto.MarkdownText,
                    cancellationToken);

                return result.ToHttpResult();
            })
            .RequireAuthorization()
            .WithSummary("Change task description")
            .WithDescription("Updates the Markdown description of a task.");
        
        taskGroup.MapPatch("/{id:guid}/deadline", async (
                [FromRoute] Guid id,
                [FromBody] ChangeTaskDueAtDto dto,
                HttpContext context,
                [FromServices] TaskService taskService,
                [FromServices] TokenService tokenService,
                CancellationToken cancellationToken) =>
            {
                var userIdResult = tokenService.GetUserIdFromContext(context);

                if (userIdResult.IsFailure)
                    return Results.Unauthorized();

                var result = await taskService.ChangeDueAtAsync(
                    id,
                    userIdResult.Value,
                    dto.DueAt,
                    cancellationToken);

                return result.ToHttpResult();
            })
            .RequireAuthorization()
            .WithSummary("Change task deadline")
            .WithDescription("Sets, changes or removes the deadline of a task.");
        
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
            var result = await teamJoinRequestService.ApproveAsync(
                id,
                joinRequestId,
                userIdResult.Value
            );
            return result.ToHttpResult();
        });
        
        
        teamGroup.MapPatch("{id}/jounrequest/{joinRequestId}/Reject", async
        (HttpContext context, [FromRoute] Guid id, [FromRoute] Guid joinRequestId,
            [FromServices] TeamJoinRequestService teamJoinRequestService, [FromServices] TokenService tokenService) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();
            var result = await teamJoinRequestService.RejectAsync(
                id,
                joinRequestId,
                userIdResult.Value
            );
            return result.ToHttpResult();
        });
        
        teamGroup.MapPatch("{id}/changeIdName", async 
        ([FromRoute] Guid id,
            [FromBody] string displayName,
            [FromServices] TeamService teamService,
            [FromServices] TokenService tokenService,
            HttpContext context) =>
        {
            var userIdResult = tokenService.GetUserIdFromContext(context);
            if (userIdResult.IsFailure)
                return Results.Unauthorized();
            var result = await teamService.ChangeIdName(
                userIdResult.Value, id,
                displayName);

            return result.ToHttpResult();
        });
    }

    // SEARCH ENDPOINTS
    private static void ConfigureSearchEndpoints(RouteGroupBuilder searchGroup)
    {
        searchGroup.MapPost("/users", async
        (HttpContext context, [FromServices] SearchService searchService,
            [FromServices] TokenService tokenService, [FromBody] UserEndpointSearchDto dto) =>
        {
            var userId = tokenService.GetUserIdFromContext(context);
            if (userId.IsFailure)
                return Results.Unauthorized();

            var result = await searchService.SearchUsersAsync(dto);
            return result.ToHttpResult();
        });
        
        
        searchGroup.MapPost("/teams", async
        (HttpContext context, [FromServices] SearchService searchService,
            [FromServices] TokenService tokenService, [FromBody] TeamEndpointSearchDto dto) =>
        {
            var userId = tokenService.GetUserIdFromContext(context);
            if (userId.IsFailure)
                return Results.Unauthorized();

            var result = await searchService.SearchTeamAsync(dto);
            return result.ToHttpResult();
        });
    }


    private static void ConfigureAccountEndpoints(
        RouteGroupBuilder accountGroup)
    {
        accountGroup.MapGet("/", async (
            HttpContext httpContext,
            AccountService accountService,
            TokenService tokenService) =>
        {
            var userId =
                tokenService.GetUserIdFromContext(httpContext);

            if (userId.IsFailure)
                return Results.Unauthorized();

            var result =
                await accountService.GetProfileAsync(userId.Value);

            return result.ToHttpResult();
        });

        accountGroup.MapPatch("/display-name", async (
            HttpContext httpContext,
            ChangeDisplayNameDto dto,
            AccountService accountService,
            TokenService tokenService) =>
        {
            var userId =
                tokenService.GetUserIdFromContext(httpContext);

            if (userId.IsFailure)
                return Results.Unauthorized();

            var result =
                await accountService.ChangeDisplayNameAsync(
                    userId.Value,
                    dto.DisplayName);

            return result.ToHttpResult();
        });

        accountGroup.MapPatch("/visibility", async (
            HttpContext httpContext,
            ChangeVisibilityDto dto,
            AccountService accountService,
            TokenService tokenService) =>
        {
            var userId =
                tokenService.GetUserIdFromContext(httpContext);

            if (userId.IsFailure)
                return Results.Unauthorized();

            var result =
                await accountService.ChangeVisibilityAsync(
                    userId.Value,
                    dto.Visible);

            return result.ToHttpResult();
        });

        accountGroup.MapPatch("/password", 
            async (TokenService tokenService, HttpContext context,
                AccountService accountService, ChangePasswordDto dto) =>
            {
                var userId = tokenService.GetUserIdFromContext(context);
                
                if (userId.IsFailure)
                    return Results.Unauthorized();

                var result = await accountService.ChangePasswordAsync(dto, userId.Value);
                
                return result.ToHttpResult();
            });

        accountGroup.MapPatch("/email", async 
            (TokenService tokenService, HttpContext context,
                AccountService accountService, ChangeEmailDto dto) =>
        {
            var userId = tokenService.GetUserIdFromContext(context);
            
            if (userId.IsFailure)
                return  Results.Unauthorized();
            
            var result = await accountService.ChangeEmailAsync(dto, userId.Value);
            
            return result.ToHttpResult();
        });

        accountGroup.MapPatch("/name", async
            (TokenService tokenService, HttpContext context,
                AccountService accountService, ChangeUserNameDto dto) =>
        {
            var userId = tokenService.GetUserIdFromContext(context);
            
            if (userId.IsFailure)
                return  Results.Unauthorized();

            var result = await accountService.ChangeUserNameAsync(dto,userId.Value);
            return result.ToHttpResult();
        });
        
        accountGroup.MapDelete("/{id}", async (
                HttpContext context,
                [FromRoute] Guid id,
                [FromServices] AccountService accountService,
                [FromServices] TokenService tokenService) =>
            {
                //такая форма запроса с повоторяющимся id нужна в случае добавления администратора на площадку
                var userIdResult = tokenService.GetUserIdFromContext(context);
                if (userIdResult.IsFailure)
                    return Results.Unauthorized();

                var result = await accountService.DeleteUserAsync(new DeleteUserDto(id), userIdResult.Value);
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
    
    
    private static void ConfigureNotificationEndpoints(RouteGroupBuilder notificationGroup)
    {
        // Получить страницу уведомлений текущего пользователя.
        notificationGroup.MapGet("/", async (
            [AsParameters] PaginationDto paginationDto,
            HttpContext httpContext,
            NotificationService notificationService,
            TokenService tokenService) =>
        {
            var userId =
                tokenService.GetUserIdFromContext(httpContext);

            if (userId.IsFailure)
                return Results.Unauthorized();

            var result = await notificationService.GetAllUserNotifyAsync(
                userId.Value,
                paginationDto,
                httpContext.RequestAborted);

            return result.ToHttpResult();
        });

        // Получить общее количество уведомлений.
        notificationGroup.MapGet("/count", async (
            HttpContext httpContext,
            NotificationService notificationService,
            TokenService tokenService) =>
        {
            var userId =
                tokenService.GetUserIdFromContext(httpContext);

            if (userId.IsFailure)
                return Results.Unauthorized();

            var result = await notificationService.GetTotalCountAsync(
                userId.Value,
                httpContext.RequestAborted);

            return result.ToHttpResult();
        });

        // Получить количество непрочитанных уведомлений.
        notificationGroup.MapGet("/unread-count", async (
            HttpContext httpContext,
            NotificationService notificationService,
            TokenService tokenService) =>
        {
            var userId =
                tokenService.GetUserIdFromContext(httpContext);

            if (userId.IsFailure)
                return Results.Unauthorized();

            var result = await notificationService.GetUnreadCountAsync(
                userId.Value,
                httpContext.RequestAborted);

            return result.ToHttpResult();
        });

        // Отметить уведомление прочитанным.
        notificationGroup.MapPatch("/{notificationId:guid}/read", async (
            Guid notificationId,
            HttpContext httpContext,
            NotificationService notificationService,
            TokenService tokenService) =>
        {
            var userId =
                tokenService.GetUserIdFromContext(httpContext);

            if (userId.IsFailure)
                return Results.Unauthorized();

            var result = await notificationService.MarkAsReadAsync(
                notificationId,
                userId.Value,
                httpContext.RequestAborted);

            return result.ToHttpResult();
        });
    }
    
}