using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Authorization2.Auth;
using Microsoft.AspNetCore.Mvc;
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
            
            return Results.Ok(id.ToString("D"));
        }).RequireAuthorization();
        
        app.MapGet("/Task/{id}", (Guid id, TaskService taskService) =>
        {
            if (taskService is null) Results.InternalServerError();
            return Results.Ok();
        }).RequireAuthorization(AllNameOfPolicy.SameAuthorRequirement);

        app.MapPost("/register", (CreateUserDto request, UserService userService) =>
        {
            if (userService is null) Results.InternalServerError();
            userService.Register(request);
            return Results.Created();
        });

        app.MapPost("/login", (LoginUserDto request, HttpContext context, UserService userService) =>
        {
            if (userService is null) Results.InternalServerError();
            userService.Userlogin(request);
            var id = userService.GetUseId(request);
            var claims = new List<Claim> {new Claim("id",id.ToString() ) };
            var jwt = new JwtSecurityToken(
                claims: claims ,
                expires: DateTime.UtcNow.Add(TimeSpan.FromMinutes(2)),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes("qwertyuiop[]asdfghjkl;'zxcvbnm,./")),
                    SecurityAlgorithms.HmacSha256));
            var token = new JwtSecurityTokenHandler().WriteToken(jwt);
            if (token is null) Results.InternalServerError();
            context.Response.Cookies.Append("zxc", $"{token}");
            return  Results.Ok();
        });
        
        return app;
        
    }
}