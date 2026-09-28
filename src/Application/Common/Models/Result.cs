namespace Application.Common.Models;

public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public int StatusCode { get; }

    private Result(bool isSuccess, T? value, string? error, int statusCode = 400)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        StatusCode = statusCode;
    }

    public static Result<T> Success(T value) => new(true, value, null, 200);

    public static Result<T> Failure(string error, int statusCode = 400)
    {
        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentException("Failure error message is required.", nameof(error));
        return new(false, default, error, statusCode);
    }
}

public sealed class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public int StatusCode { get; }

    private Result(bool isSuccess, string? error, int statusCode = 400)
    {
        IsSuccess = isSuccess;
        Error = error;
        StatusCode = statusCode;
    }

    public static Result Success() => new(true, null, 200);

    public static Result Failure(string error, int statusCode = 400)
    {
        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentException("Failure error message is required.", nameof(error));
        return new(false, error, statusCode);
    }
}
