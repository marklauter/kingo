namespace Kingo;

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

    /// <summary>Pattern-match the result for side effects, producing no value.</summary>
    public void Switch(Action<T> onSuccess, Action<Error> onError)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onError);
        switch (this)
        {
            case Success<T> s: onSuccess(s.Value); break;
            case Failure<T> f: onError(f.Error); break;
            default: throw new InvalidOperationException("Result<T> must be either Success<T> or Failure<T>.");
        }
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
    /// Lift a unary function over a <see cref="Result{T}"/>-wrapped argument. The function runs only when the argument is <see cref="Success{T}"/>; otherwise the failure passes through unchanged.
    /// </summary>
    public static Result<TResult> Apply<T, TResult>(Func<T, TResult> fn, Result<T> result)
    {
        ArgumentNullException.ThrowIfNull(fn);
        ArgumentNullException.ThrowIfNull(result);
        return result switch
        {
            Success<T> s => Success(fn(s.Value)),
            Failure<T> f => Failure<TResult>(f.Error),
            _ => throw new InvalidOperationException("Result<T> must be either Success<T> or Failure<T>."),
        };
    }

    /// <summary>
    /// Lift a binary function over two <see cref="Result{T}"/>-wrapped arguments. Short-circuits on the first failure — <paramref name="r1"/> is checked before <paramref name="r2"/>.
    /// </summary>
    public static Result<TResult> Apply<T1, T2, TResult>(Func<T1, T2, TResult> fn, Result<T1> r1, Result<T2> r2)
    {
        ArgumentNullException.ThrowIfNull(fn);
        ArgumentNullException.ThrowIfNull(r1);
        ArgumentNullException.ThrowIfNull(r2);
        return (r1, r2) switch
        {
            (Success<T1> s1, Success<T2> s2) => Success(fn(s1.Value, s2.Value)),
            (Failure<T1> f, _) => Failure<TResult>(f.Error),
            (_, Failure<T2> f) => Failure<TResult>(f.Error),
            _ => throw new InvalidOperationException("Result<T> must be either Success<T> or Failure<T>."),
        };
    }

    /// <summary>
    /// Lift a ternary function over three <see cref="Result{T}"/>-wrapped arguments. Short-circuits on the first failure.
    /// </summary>
    public static Result<TResult> Apply<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> fn, Result<T1> r1, Result<T2> r2, Result<T3> r3)
    {
        ArgumentNullException.ThrowIfNull(fn);
        ArgumentNullException.ThrowIfNull(r1);
        ArgumentNullException.ThrowIfNull(r2);
        ArgumentNullException.ThrowIfNull(r3);
        return (r1, r2, r3) switch
        {
            (Success<T1> s1, Success<T2> s2, Success<T3> s3) => Success(fn(s1.Value, s2.Value, s3.Value)),
            (Failure<T1> f, _, _) => Failure<TResult>(f.Error),
            (_, Failure<T2> f, _) => Failure<TResult>(f.Error),
            (_, _, Failure<T3> f) => Failure<TResult>(f.Error),
            _ => throw new InvalidOperationException("Result<T> must be either Success<T> or Failure<T>."),
        };
    }
}
