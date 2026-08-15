using src.Domain;

namespace src.InfrastructError;

public static class ErrorMapper
{
    public static int ToHttpStatusCode(this Error error)
    {
        return error.Errortype switch
        {
            "Validation" => StatusCodes.Status400BadRequest,
            "Unauthorized" => StatusCodes.Status401Unauthorized,
            "Authorization" => StatusCodes.Status403Forbidden,
            "NotFound" => StatusCodes.Status404NotFound,
            "Conflict" => StatusCodes.Status409Conflict,
            "ServerError" => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status400BadRequest
        };
    }
}