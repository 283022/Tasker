using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace src.Auth;

public static class AuthServiceExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var secretKey = configuration[AuthConstants.JwtSecretKeyConfigPath]
                        ?? throw new InvalidOperationException(AuthConstants.ErrorSecretNotConfigured);

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = configuration.GetValue<bool>("JwtBearer:ValidateIssuer", false),
            ValidateAudience = configuration.GetValue<bool>("JwtBearer:ValidateAudience", false),
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };

        services.AddSingleton(tokenValidationParameters);
        services.AddSingleton<TokenService>();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = tokenValidationParameters;
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = JwtCookieEvents.OnMessageReceived,
                    OnChallenge = JwtCookieEvents.OnChallenge,
                    OnForbidden = JwtCookieEvents.OnForbidden
                };
            });

        return services;
    }
}