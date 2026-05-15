using System.Diagnostics.CodeAnalysis;

namespace Results;

/// <summary>
/// Discriminated result of a domain operation — either a successful <typeparamref name="T"/> value (<see cref="Result{T}.Success"/>) or a named <see cref="Error"/> (<see cref="Result{T}.Failure"/>). Consume via <see cref="Match"/>, transform via <see cref="Map"/>, chain via <see cref="Bind"/> or <see cref="BindAsync"/>. The inheritance hierarchy is closed — <see cref="Result{T}.Success"/> and <see cref="Result{T}.Failure"/> are the only inhabitants.
/// </summary>
/// <typeparam name="T">The success value type.</typeparam>
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Discriminated-union shape: Success and Failure are inhabitants of Result<T>; nesting names the relationship in the type itself.")]
public abstract record Result<T>
{
    private protected Result() { }

    /// <summary>The successful inhabitant of <see cref="Result{T}"/>.</summary>
    public sealed record Success(T Value) : Result<T>;

    /// <summary>The failed inhabitant of <see cref="Result{T}"/>.</summary>
    public sealed record Failure(Error Error) : Result<T>;

    /// <summary>Pattern-match the result, producing a value on either path.</summary>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onError)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onError);
        return this switch
        {
            Success s => onSuccess(s.Value),
            Failure f => onError(f.Error),
            _ => throw new InvalidOperationException($"Match: unknown Result<T> variant '{GetType()}'; expected Success or Failure.")
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
            Success s => new Result<TResult>.Success(fn(s.Value)),
            Failure f => new Result<TResult>.Failure(f.Error),
            _ => throw new InvalidOperationException($"Map: unknown Result<T> variant '{GetType()}'; expected Success or Failure.")
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
            Success s => fn(s.Value),
            Failure f => new Result<TResult>.Failure(f.Error),
            _ => throw new InvalidOperationException($"Bind: unknown Result<T> variant '{GetType()}'; expected Success or Failure.")
        };
    }

    /// <summary>
    /// Async monadic bind: chain a result-returning async function after a successful result; short-circuit on failure. Cancellation is the responsibility of <paramref name="fn"/> — no <see cref="System.Threading.CancellationToken"/> is threaded through the API; callers needing cancellation pass a lambda that captures the token from its enclosing scope.
    /// </summary>
    public async Task<Result<TResult>> BindAsync<TResult>(Func<T, Task<Result<TResult>>> fn)
    {
        ArgumentNullException.ThrowIfNull(fn);
        return this switch
        {
            Success s => await fn(s.Value).ConfigureAwait(false),
            Failure f => new Result<TResult>.Failure(f.Error),
            _ => throw new InvalidOperationException($"BindAsync: unknown Result<T> variant '{GetType()}'; expected Success or Failure.")
        };
    }
}

/// <summary>
/// Non-generic factories and combinators for <see cref="Result{T}"/>. Use <c>Result.Success(value)</c> and <c>Result.Failure&lt;T&gt;(error)</c> when you want type inference at the construction site, and <c>Result.Apply</c> to lift functions over <see cref="Result{T}"/>-wrapped arguments.
/// </summary>
public static class Result
{
    /// <summary>Construct a successful result. Type parameter inferred from <paramref name="value"/>.</summary>
    public static Result<T> Success<T>(T value) => new Result<T>.Success(value);

    /// <summary>Construct a failed result. The success type <typeparamref name="T"/> must be supplied explicitly — it cannot be inferred from <paramref name="error"/>.</summary>
    public static Result<T> Failure<T>(Error error) => new Result<T>.Failure(error);

    /// <summary>
    /// Applicative application: feed a <see cref="Result{T}"/>-wrapped argument to a <see cref="Result{T}"/>-wrapped function. Multi-arity handled by currying — apply repeatedly to consume each argument. Short-circuits on the first failure, with the wrapped function checked before the wrapped argument.
    /// </summary>
    public static Result<TResult> Apply<T, TResult>(Result<Func<T, TResult>> resultFn, Result<T> resultArg)
    {
        ArgumentNullException.ThrowIfNull(resultFn);
        ArgumentNullException.ThrowIfNull(resultArg);
        return (resultFn, resultArg) switch
        {
            (Result<Func<T, TResult>>.Success f, Result<T>.Success a) => Success(f.Value(a.Value)),
            (Result<Func<T, TResult>>.Failure f, _) => Failure<TResult>(f.Error),
            (_, Result<T>.Failure a) => Failure<TResult>(a.Error),
            _ => throw new InvalidOperationException($"Apply: unknown Result<T> variants '({resultFn.GetType()}, {resultArg.GetType()})'; expected Success or Failure for both.")
        };
    }
}
