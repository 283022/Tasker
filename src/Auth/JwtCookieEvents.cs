using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace src.Auth;

/// <summary>
/// Обработчики событий для JWT Bearer аутентификации.
/// Отвечают за чтение токена из cookie и кастомизацию ответов.
/// </summary>
public class JwtCookieEvents
{
    /// <summary>
    /// Обработчик события OnMessageReceived - читает токен из cookie.
    /// </summary>
    public static Task OnMessageReceived(MessageReceivedContext context)
    {
        if (context.Request.Cookies.TryGetValue(AuthConstants.AuthTokenCookieName, out var token))
        {
            context.Token = token;
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Обработчик события OnChallenge - кастомизация ответа при 401 Unauthorized.
    /// </summary>
    public static Task OnChallenge(JwtBearerChallengeContext context)
    {
        context.HandleResponse();

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = new
            {
                code = "unauthorized",
                message = "Authentication failed. Please provide a valid token."
            }
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    /// <summary>
    /// Обработчик события OnForbidden - кастомизация ответа при 403 Forbidden.
    /// </summary>
    public static Task OnForbidden(ForbiddenContext context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = new
            {
                code = "forbidden",
                message = "You do not have permission to access this resource."
            }
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}