using src.Domain;

namespace src.InfrastructError;

public static class ResultExtensions
{
    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return Results.Ok(result.Value);

        var statusCode = result.Error.ToHttpStatusCode();
        return Results.Problem(
            detail: result.Error.Message,
            title: result.Error.Errortype,
            statusCode: statusCode,
            type: $"https://httpstatuses.com/{statusCode}"
        );
    }

    public static IResult ToHttpResult(this Result result)
    {
        if (result.IsSuccess)
            return Results.Ok();

        var statusCode = result.Error.ToHttpStatusCode();
        return Results.Problem(
            detail: result.Error.Message,
            title: result.Error.Errortype,
            statusCode: statusCode,
            type: $"https://httpstatuses.com/{statusCode}"
        );
    }
}