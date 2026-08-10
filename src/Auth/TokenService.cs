using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using src.Domain;

namespace src.Auth;

public class TokenService
{
    private readonly IConfiguration _configuration;
    private readonly TokenValidationParameters _validationParameters;
    private readonly SymmetricSecurityKey _securityKey;
    private readonly int _expiryMinutes;
    private readonly string? _issuer;
    private readonly string? _audience;

    public TokenService(
        IConfiguration configuration,
        TokenValidationParameters validationParameters)
    {
        _configuration = configuration;
        _validationParameters = validationParameters;

        var secretKey = configuration[AuthConstants.JwtSecretKeyConfigPath]
                        ?? throw new InvalidOperationException(AuthConstants.ErrorSecretNotConfigured);

        _securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        _expiryMinutes = configuration.GetValue<int>(AuthConstants.JwtExpiryMinutesConfigPath, 15);
        _issuer = configuration.GetValue<string?>(AuthConstants.JwtIssuerConfigPath);
        _audience = configuration.GetValue<string?>(AuthConstants.JwtAudienceConfigPath);
    }

    public string GenerateToken(Guid userId)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(AuthConstants.UserIdClaimType, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_expiryMinutes),
            SigningCredentials = new SigningCredentials(_securityKey, SecurityAlgorithms.HmacSha256),
            Issuer = _issuer,
            Audience = _audience
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public Result<Guid> ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Result<Guid>.Failure(new Error("Auth", AuthConstants.ErrorTokenEmpty));

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(
                token,
                _validationParameters,
                out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase))
            {
                return Result<Guid>.Failure(new Error("Auth", AuthConstants.ErrorInvalidAlgorithm));
            }

            var userIdClaim = principal.FindFirst(AuthConstants.UserIdClaimType)?.Value
                              ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Result<Guid>.Failure(new Error("Auth", AuthConstants.ErrorUserIdNotFound));
            }

            return Result<Guid>.Success(userId);
        }
        catch (SecurityTokenExpiredException)
        {
            return Result<Guid>.Failure(new Error("Auth", AuthConstants.ErrorTokenExpired));
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            return Result<Guid>.Failure(new Error("Auth", AuthConstants.ErrorInvalidSignature));
        }
        catch (SecurityTokenInvalidIssuerException)
        {
            return Result<Guid>.Failure(new Error("Auth", AuthConstants.ErrorInvalidIssuer));
        }
        catch (SecurityTokenInvalidAudienceException)
        {
            return Result<Guid>.Failure(new Error("Auth", AuthConstants.ErrorInvalidAudience));
        }
        catch (Exception)
        {
            return Result<Guid>.Failure(new Error("Auth", AuthConstants.ErrorInvalidToken));
        }
    }

    public Result<Guid> GetUserIdFromContext(HttpContext context)
    {
        var token = context.Request.Cookies.TryGetValue(AuthConstants.AuthTokenCookieName, out var cookieToken)
            ? cookieToken
            : null;

        if (string.IsNullOrEmpty(token))
        {
            var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = authHeader.Substring("Bearer ".Length).Trim();
            }
        }

        if (string.IsNullOrEmpty(token))
            return Result<Guid>.Failure(new Error("Auth", AuthConstants.ErrorNoTokenProvided));

        return ValidateToken(token);
    }

    public int GetTokenExpiryMinutes() => _expiryMinutes;

    public (string Token, CookieOptions Options) GenerateTokenWithCookie(Guid userId)
    {
        var token = GenerateToken(userId);

        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddMinutes(_expiryMinutes),
            Path = "/"
        };

        return (token, options);
    }
}