namespace Authorization2.Domain;

public class Result
{
    private Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None || !isSuccess && error == Error.None)
        {
            throw new ArgumentException("Invalid error", nameof(error));
            
        }

        IsSuccess = isSuccess;
        Error = error;
    }
    
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }
    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
}

public class Result<TValue>
{
    private readonly TValue? _value;
    public TValue Value => IsSuccess ? _value!:throw new InvalidOperationException("The value of a failure result");
    public Error Error { get; }
    
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    private Result(TValue value)
    {
        IsSuccess = true;
        _value = value;
        Error = Error.None;
    }

    private Result(Error error)
    {
        IsSuccess = false;
        _value = default;
        Error = error;
    }

    public static implicit operator Result<TValue>(TValue value) => new(value);
    public static implicit operator Result<TValue>(Error error) => new(error);
    
    
    public static Result<TValue> Success(TValue value) => new(value);
    public static Result<TValue> Failure(Error error) => new(error);
}

public class Error(string errortype, string message)
{
    public string Errortype { get; } = errortype;
    public string Message { get; } = message;

    public static readonly Error None = new(string.Empty,string.Empty);
}