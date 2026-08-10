namespace src.Auth;

public static class AuthConstants
{
    public const string AuthTokenCookieName = "auth_token";
    public const string JwtSecretKeyConfigPath = "JwtBearer:TokenSecurity";
    public const string JwtExpiryMinutesConfigPath = "JwtBearer:TokenExpiryMinutes";
    public const string JwtIssuerConfigPath = "JwtBearer:Issuer";
    public const string JwtAudienceConfigPath = "JwtBearer:Audience";
    public const string UserIdClaimType = "id";
    
    public const string AuthScheme = "Bearer";
    public const string AuthSchemeDisplayName = "Bearer";
    
    // Сообщения об ошибках
    public const string ErrorTokenEmpty = "Token is empty";
    public const string ErrorTokenExpired = "Token has expired";
    public const string ErrorInvalidSignature = "Invalid token signature";
    public const string ErrorInvalidIssuer = "Invalid token issuer";
    public const string ErrorInvalidAudience = "Invalid token audience";
    public const string ErrorInvalidAlgorithm = "Invalid token algorithm";
    public const string ErrorUserIdNotFound = "User ID not found in token";
    public const string ErrorInvalidToken = "Invalid token";
    public const string ErrorNoTokenProvided = "No token provided";
    public const string ErrorSecretNotConfigured = "JWT secret key is not configured";
}