namespace Results;

/// <summary>
/// Discriminated result of a domain operation — either a successful <typeparamref name="T"/> value (<see cref="Success{T}"/>) or a named <see cref="Kingo.Error"/> (<see cref="Failure{T}"/>). Pattern match the result to handle both cases.
/// </summary>
/// <typeparam name="T">The success value type.</typeparam>
public abstract record Result<T>
{
    /// <summary>Lift a value into a successful result.</summary>
    public static implicit operator Result<T>(T value) => new Success<T>(value);

    /// <summary>Lift an error into a failed result.</summary>
    public static implicit operator Result<T>(Error error) => new Failure<T>(error);

    /// <summary>Pattern-match the result, producing a value on either path.</summary>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onError)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onError);
        return this switch
        {
            Success<T> s => onSuccess(s.Value),
            Failure<T> f => onError(f.Error),
            _ => throw new InvalidOperationException("Result<T> must be either Success<T> or Failure<T>."),
        };
    }

    /// <summary>
    /// Functor map: transform the success value with <paramref name="fn"/>; pass the failure through unchanged.
    /// </summary>
    public Result<TResult> Map<TResult>(Func<T, TResult> fn)
    {
        ArgumentNullException.ThrowIfNull(fn);
        return this switch
        {
            Success<T> s => new Success<TResult>(fn(s.Value)),
            Failure<T> f => new Failure<TResult>(f.Error),
            _ => throw new InvalidOperationException("Result<T> must be either Success<T> or Failure<T>."),
        };
    }

    /// <summary>
    /// Monadic bind: chain a result-returning function after a successful result; short-circuit on failure.
    /// </summary>
    public Result<TResult> Bind<TResult>(Func<T, Result<TResult>> fn)
    {
        ArgumentNullException.ThrowIfNull(fn);
        return this switch
        {
            Success<T> s => fn(s.Value),
            Failure<T> f => new Failure<TResult>(f.Error),
            _ => throw new InvalidOperationException("Result<T> must be either Success<T> or Failure<T>."),
        };
    }

    /// <summary>
    /// Async monadic bind: chain a result-returning async function after a successful result; short-circuit on failure.
    /// </summary>
    public async Task<Result<TResult>> BindAsync<TResult>(Func<T, Task<Result<TResult>>> fn)
    {
        ArgumentNullException.ThrowIfNull(fn);
        return this switch
        {
            Success<T> s => await fn(s.Value).ConfigureAwait(false),
            Failure<T> f => new Failure<TResult>(f.Error),
            _ => throw new InvalidOperationException("Result<T> must be either Success<T> or Failure<T>."),
        };
    }
}

/// <summary>The successful inhabitant of <see cref="Result{T}"/>.</summary>
public sealed record Success<T>(T Value) : Result<T>;

/// <summary>The failed inhabitant of <see cref="Result{T}"/>.</summary>
public sealed record Failure<T>(Error Error) : Result<T>;

/// <summary>
/// Non-generic factories and combinators for <see cref="Result{T}"/>. Use <c>Result.Success(value)</c> and <c>Result.Failure&lt;T&gt;(error)</c> when you want type inference at the construction site, and <c>Result.Apply</c> to lift functions over <see cref="Result{T}"/>-wrapped arguments.
/// </summary>
public static class Result
{
    /// <summary>Construct a successful result. Type parameter inferred from <paramref name="value"/>.</summary>
    public static Result<T> Success<T>(T value) => new Success<T>(value);

    /// <summary>Construct a failed result.</summary>
    public static Result<T> Failure<T>(Error error) => new Failure<T>(error);

    /// <summary>
    /// Canonical applicative application: feed a <see cref="Result{T}"/>-wrapped argument to a <see cref="Result{T}"/>-wrapped function. Multi-arity handled by currying — apply repeatedly to consume each argument. Short-circuits on the first failure, with the wrapped function checked before the wrapped argument.
    /// </summary>
    public static Result<TResult> Apply<T, TResult>(Result<Func<T, TResult>> resultFn, Result<T> resultArg)
    {
        ArgumentNullException.ThrowIfNull(resultFn);
        ArgumentNullException.ThrowIfNull(resultArg);
        return (resultFn, resultArg) switch
        {
            (Success<Func<T, TResult>> f, Success<T> a) => Success(f.Value(a.Value)),
            (Failure<Func<T, TResult>> f, _) => Failure<TResult>(f.Error),
            (_, Failure<T> a) => Failure<TResult>(a.Error),
            _ => throw new InvalidOperationException("Result<T> must be either Success<T> or Failure<T>."),
        };
    }
}
