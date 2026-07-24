using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Authorization2.Auth;
using Microsoft.IdentityModel.Tokens;

namespace Authorization2;

public static class Endpoints
{
    public static IEndpointRouteBuilder AddAllEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/Task", () =>
        {
            return Results.Ok("hello user");
        }).RequireAuthorization();

        app.MapPost("/Task", (CreateTaskDto request, TaskService taskService) =>
        {
            if (taskService is null) Results.InternalServerError();
            var id = taskService.Create(request);
            
            return Results.Ok(id.Value.ToString("D"));
        }).RequireAuthorization();
        
        app.MapGet("/Task/{id}", (Guid id, TaskService taskService) =>
        {
            if (taskService is null) Results.InternalServerError();
            return Results.Ok();
        }).RequireAuthorization(AllNameOfPolicy.SameAuthorRequirement);

        app.MapPost("/register", (CreateUserDto request, UserService userService) =>
        {
            if (userService is null)
                return Results.Problem("Service unavailable");

            var result = userService.Register(request);
    
            if (result.IsFailure)
                return Results.BadRequest(new { error = result.Error.Message });

            return Results.Created();
        });
        
        app.MapPost("/login", (LoginUserDto request, HttpContext context, UserService userService) =>
        {
            if (userService is null)
                return Results.Problem("Service unavailable");
            
            var loginResult = userService.Userlogin(request);
            if (loginResult.IsFailure)
                return Results.Unauthorized();

            var user = loginResult.Value;
    
            // Генерация JWT
            var claims = new List<Claim> { new Claim("id", user.ID.ToString()) };
            var jwt = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(2),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes("qwertyuiop[]asdfghjkl;'zxcvbnm,./")),
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
        
        return app;
        
    }
}