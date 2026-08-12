namespace Finanzuebersicht.Application.Results;

/// <summary>
/// Lightweight success/failure result for use cases. Prefer returning this over throwing for expected domain errors.
/// </summary>
public readonly struct UseCaseResult
{
    private UseCaseResult(UseCaseError? error)
    {
        Error = error;
    }

    public bool IsSuccess => Error is null;
    public UseCaseError? Error { get; }

    public static UseCaseResult Ok() => new(null);

    public static UseCaseResult Fail(UseCaseErrorCode code, params object[] formatArgs)
        => new(new UseCaseError(code, formatArgs));

    public static UseCaseResult<T> Ok<T>(T value) => UseCaseResult<T>.Ok(value);

    public static UseCaseResult<T> Fail<T>(UseCaseErrorCode code, params object[] formatArgs)
        => UseCaseResult<T>.Fail(code, formatArgs);
}

/// <summary>
/// Lightweight success/failure result with a value on success.
/// </summary>
public readonly struct UseCaseResult<T>
{
    private UseCaseResult(T? value, UseCaseError? error)
    {
        Value = value;
        Error = error;
    }

    public bool IsSuccess => Error is null;
    public T? Value { get; }
    public UseCaseError? Error { get; }

    public static UseCaseResult<T> Ok(T value) => new(value, null);

    public static UseCaseResult<T> Fail(UseCaseErrorCode code, params object[] formatArgs)
        => new(default, new UseCaseError(code, formatArgs));
}
