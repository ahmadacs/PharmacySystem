namespace Application.Common.Models;

/// <summary>
/// Generic Result type — represents success with a value or failure with an error message.
/// Use <c>Result.Success(value)</c> and <c>Result.Failure(error)</c> factories.
/// Preserves the centralized exception-handling semantics: Domain/Application code can
/// return failures without throwing; WebApi unwraps to the standard error envelope.
/// </summary>
public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
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

    /// <summary>Implicit conversion from value to success result — handy in handlers.</summary>
    public static implicit operator Result<T>(T value) => Success(value);

    public T GetValueOrThrow()
        => IsSuccess ? Value! : throw new InvalidOperationException(Error ?? "Result is failure.");

    public override string ToString() => IsSuccess ? $"Success({Value})" : $"Failure({Error})";
}

/// <summary>
/// Non-generic Result for void/command operations (e.g. update/delete).
/// </summary>
public sealed class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
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

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(string error, int statusCode = 400) => Result<T>.Failure(error, statusCode);

    public override string ToString() => IsSuccess ? "Success" : $"Failure({Error})";
}
