using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

// CS8509 / IDE0072: switch expressions in this file dispatch over the closed Result<T> hierarchy
// (Success | Failure). The compiler cannot prove exhaustiveness for class hierarchies, but the
// private-protected ctor makes the variant set sealed in practice. Suppressing here removes
// dead defensive arms that otherwise drag test coverage down.
#pragma warning disable CS8509
#pragma warning disable IDE0072

namespace Results;

/// <summary>
/// Discriminated result of a domain operation — either a successful <typeparamref name="T"/> value (<see cref="Result{T}.Success"/>) or a non-empty collection of <see cref="Error"/>s (<see cref="Result{T}.Failure"/>). Consume via <see cref="Match"/>, transform via <see cref="Map"/>, chain via <see cref="Bind"/> or <see cref="BindAsync"/>. Combine independent results via <see cref="Result.Apply{T, TResult}(Result{System.Func{T, TResult}}, Result{T})"/> (applicative — accumulates errors on the failure path) or the Unit-keyed <c>Result.Apply</c> overload (effect sequencing). The inheritance hierarchy is closed — <see cref="Result{T}.Success"/> and <see cref="Result{T}.Failure"/> are the only inhabitants.
/// </summary>
/// <typeparam name="T">The success value type.</typeparam>
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Discriminated-union shape: Success and Failure are inhabitants of Result<T>; nesting names the relationship in the type itself.")]
public abstract record Result<T>
{
    private protected Result() { }

    /// <summary>The successful inhabitant of <see cref="Result{T}"/>.</summary>
    public sealed record Success(T Value) : Result<T>;

    /// <summary>The failed inhabitant of <see cref="Result{T}"/>. <see cref="Errors"/> is always non-empty when constructed via the <c>Result.Failure</c> factories. Equality is structural over <see cref="Errors"/> (element-wise, order-sensitive) — <see cref="ImmutableArray{T}"/>'s default record equality would otherwise compare the underlying array reference.</summary>
    public sealed record Failure(ImmutableArray<Error> Errors) : Result<T>
    {
        public bool Equals(Failure? other) =>
            other is not null && Errors.AsSpan().SequenceEqual(other.Errors.AsSpan());

        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var error in Errors.AsSpan())
                hash.Add(error);
            return hash.ToHashCode();
        }
    }

    /// <summary>Pattern-match the result, producing a value on either path.</summary>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<ImmutableArray<Error>, TResult> onError) =>
        this switch
        {
            Success s => onSuccess(s.Value),
            Failure f => onError(f.Errors),
        };

    /// <summary>
    /// Functor map: transform the success value with <paramref name="fn"/>; pass the failure through unchanged.
    /// </summary>
    public Result<TResult> Map<TResult>(Func<T, TResult> fn) =>
        this switch
        {
            Success s => new Result<TResult>.Success(fn(s.Value)),
            Failure f => new Result<TResult>.Failure(f.Errors),
        };

    /// <summary>
    /// Monadic bind: chain a result-returning function after a successful result; short-circuit on failure.
    /// </summary>
    public Result<TResult> Bind<TResult>(Func<T, Result<TResult>> fn) =>
        this switch
        {
            Success s => fn(s.Value),
            Failure f => new Result<TResult>.Failure(f.Errors),
        };

    /// <summary>
    /// Async monadic bind: chain a result-returning async function after a successful result; short-circuit on failure. Cancellation is the responsibility of <paramref name="fn"/> — no <see cref="System.Threading.CancellationToken"/> is threaded through the API; callers needing cancellation pass a lambda that captures the token from its enclosing scope.
    /// </summary>
    public async Task<Result<TResult>> BindAsync<TResult>(Func<T, Task<Result<TResult>>> fn) =>
        this switch
        {
            Success s => await fn(s.Value).ConfigureAwait(false),
            Failure f => new Result<TResult>.Failure(f.Errors),
        };
}

/// <summary>
/// Non-generic factories and combinators for <see cref="Result{T}"/>. Use <c>Result.Success(value)</c> and <c>Result.Failure&lt;T&gt;(error)</c> when you want type inference at the construction site, and the <c>Result.Apply</c> overloads to combine independent results — accumulating all errors on the failure path.
/// </summary>
public static class Result
{
    /// <summary>Construct a successful result. Type parameter inferred from <paramref name="value"/>.</summary>
    public static Result<T> Success<T>(T value) => new Result<T>.Success(value);

    /// <summary>Construct a failed result from a single <see cref="Error"/>. The success type <typeparamref name="T"/> must be supplied explicitly — it cannot be inferred from <paramref name="error"/>.</summary>
    public static Result<T> Failure<T>(Error error) => new Result<T>.Failure([error]);

    /// <summary>Construct a failed result from one or more <see cref="Error"/>s. Throws <see cref="ArgumentException"/> when <paramref name="errors"/> is empty — a failure must carry at least one error.</summary>
    public static Result<T> Failure<T>(params Error[] errors) => errors.Length == 0
        ? throw new ArgumentException("Failure requires at least one error.", nameof(errors))
        : new Result<T>.Failure(ImmutableArray.Create(errors));

    /// <summary>Construct a failed result from a pre-built <see cref="ImmutableArray{T}"/> of <see cref="Error"/>s. Cheapest factory — the array is stored directly with no copy. Throws <see cref="ArgumentException"/> when <paramref name="errors"/> is default or empty.</summary>
    public static Result<T> Failure<T>(ImmutableArray<Error> errors) => errors.IsDefaultOrEmpty
        ? throw new ArgumentException("Failure requires at least one error.", nameof(errors))
        : new Result<T>.Failure(errors);

    /// <summary>Construct a failed result from a list of <see cref="Error"/>s. Throws <see cref="ArgumentException"/> when <paramref name="errors"/> is empty — a failure must carry at least one error.</summary>
    public static Result<T> Failure<T>(IReadOnlyList<Error> errors)
    {
        if (errors.Count == 0)
        {
            throw new ArgumentException("Failure requires at least one error.", nameof(errors));
        }

        var builder = ImmutableArray.CreateBuilder<Error>(errors.Count);
        for (var i = 0; i < errors.Count; i++)
        {
            builder.Add(errors[i]);
        }

        return new Result<T>.Failure(builder.MoveToImmutable());
    }

    /// <summary>
    /// Applicative application: feed a <see cref="Result{T}"/>-wrapped argument to a <see cref="Result{T}"/>-wrapped function. Multi-arity handled by currying — apply repeatedly to consume each argument. When both <paramref name="resultFn"/> and <paramref name="resultArg"/> fail, their errors are accumulated (function errors first, then argument errors); this is the Validation-applicative behaviour rather than fail-fast.
    /// </summary>
    public static Result<TResult> Apply<T, TResult>(Result<Func<T, TResult>> resultFn, Result<T> resultArg) =>
        (resultFn, resultArg) switch
        {
            (Result<Func<T, TResult>>.Success f, Result<T>.Success a) => Success(f.Value(a.Value)),
            (Result<Func<T, TResult>>.Failure f, Result<T>.Success _) => new Result<TResult>.Failure(f.Errors),
            (Result<Func<T, TResult>>.Success _, Result<T>.Failure a) => new Result<TResult>.Failure(a.Errors),
            (Result<Func<T, TResult>>.Failure f, Result<T>.Failure a) => new Result<TResult>.Failure(Concat(f.Errors, a.Errors)),
        };

    /// <summary>
    /// Variadic effect sequencing: combine any number of <see cref="Result{T}"/>s of <see cref="Unit"/>. Succeeds when every input succeeds (and when <paramref name="results"/> is empty — the identity element); otherwise returns a <see cref="Result{T}.Failure"/> whose errors are accumulated across all failed inputs in input order. <c>params ReadOnlySpan</c> keeps the argument list off the heap at every arity, and the single-failure path returns the failing input unchanged rather than copying its errors.
    /// </summary>
    public static Result<Unit> Apply(params ReadOnlySpan<Result<Unit>> results)
    {
        var failures = 0;
        var total = 0;
        var lastFailure = -1;
        for (var i = 0; i < results.Length; i++)
        {
            if (results[i] is Result<Unit>.Failure f)
            {
                failures++;
                total += f.Errors.Length;
                lastFailure = i;
            }
        }

        if (failures == 0)
        {
            return Success(Unit.Value);
        }

        if (failures == 1)
        {
            return results[lastFailure];
        }

        var builder = ImmutableArray.CreateBuilder<Error>(total);
        for (var i = 0; i < results.Length; i++)
        {
            if (results[i] is Result<Unit>.Failure f)
            {
                builder.AddRange(f.Errors);
            }
        }

        return new Result<Unit>.Failure(builder.MoveToImmutable());
    }

    private static ImmutableArray<Error> Concat(ImmutableArray<Error> a, ImmutableArray<Error> b) => [.. a, .. b];
}
