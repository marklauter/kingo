using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Results;

/// <summary>
/// Discriminated result of a domain operation — either a successful <typeparamref name="T"/> value (<see cref="Result{T}.Success"/>) or a non-empty collection of <see cref="Error"/>s (<see cref="Result{T}.Failure"/>). Consume via <see cref="Match"/>, transform via <see cref="Map"/>, chain via <see cref="Bind"/> or <see cref="BindAsync"/>; <see cref="Select"/> and the <see cref="SelectMany{TIntermediate, TResult}(Func{T, Result{TIntermediate}}, Func{T, TIntermediate, TResult})"/> shapes are LINQ-named aliases of the same operations. Combine independent results via <see cref="Result.Apply{T, TResult}(Result{System.Func{T, TResult}}, Result{T})"/> (applicative — accumulates errors on the failure path) or the Unit-keyed <c>Result.Apply</c> overload (effect sequencing). The inheritance hierarchy is closed — <see cref="Result{T}.Success"/> and <see cref="Result{T}.Failure"/> are the only inhabitants — and dispatch is virtual: each inhabitant implements the combinators, so exhaustiveness is a compile-time fact (a new inhabitant cannot build without them) rather than a runtime switch.
/// </summary>
/// <typeparam name="T">The success value type.</typeparam>
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Discriminated-union shape: Success and Failure are inhabitants of Result<T>; nesting names the relationship in the type itself.")]
public abstract record Result<T>
{
    private protected Result() { }

    /// <summary>The successful inhabitant of <see cref="Result{T}"/>.</summary>
    public sealed record Success(T Value) : Result<T>
    {
        /// <inheritdoc/>
        public override TResult Match<TResult>(Func<T, TResult> onSuccess, Func<ImmutableArray<Error>, TResult> onError) => onSuccess(Value);

        /// <inheritdoc/>
        public override Result<TResult> Map<TResult>(Func<T, TResult> fn) => new Result<TResult>.Success(fn(Value));

        /// <inheritdoc/>
        public override Result<TResult> Bind<TResult>(Func<T, Result<TResult>> fn) => fn(Value);

        /// <inheritdoc/>
        public override Task<Result<TResult>> BindAsync<TResult>(Func<T, Task<Result<TResult>>> fn) => fn(Value);
    }

    /// <summary>The failed inhabitant of <see cref="Result{T}"/>. <see cref="Errors"/> is always non-empty: the constructor is internal, so external construction goes through the <c>Result.Failure</c> factories, which enforce it. Equality is structural over <see cref="Errors"/> (element-wise, order-sensitive) — <see cref="ImmutableArray{T}"/>'s default record equality would otherwise compare the underlying array reference.</summary>
    public sealed record Failure : Result<T>
    {
        /// <summary>The errors carried by this failure; never empty.</summary>
        public ImmutableArray<Error> Errors { get; }

        internal Failure(ImmutableArray<Error> errors) => Errors = errors;

        /// <inheritdoc/>
        public override TResult Match<TResult>(Func<T, TResult> onSuccess, Func<ImmutableArray<Error>, TResult> onError) => onError(Errors);

        /// <inheritdoc/>
        public override Result<TResult> Map<TResult>(Func<T, TResult> fn) => new Result<TResult>.Failure(Errors);

        /// <inheritdoc/>
        public override Result<TResult> Bind<TResult>(Func<T, Result<TResult>> fn) => new Result<TResult>.Failure(Errors);

        /// <inheritdoc/>
        public override Task<Result<TResult>> BindAsync<TResult>(Func<T, Task<Result<TResult>>> fn) => Task.FromResult<Result<TResult>>(new Result<TResult>.Failure(Errors));

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
    public abstract TResult Match<TResult>(Func<T, TResult> onSuccess, Func<ImmutableArray<Error>, TResult> onError);

    /// <summary>
    /// Functor map: transform the success value with <paramref name="fn"/>; pass the failure through unchanged.
    /// </summary>
    public abstract Result<TResult> Map<TResult>(Func<T, TResult> fn);

    /// <summary>
    /// Monadic bind: chain a result-returning function after a successful result; short-circuit on failure.
    /// </summary>
    public abstract Result<TResult> Bind<TResult>(Func<T, Result<TResult>> fn);

    /// <summary>
    /// Async monadic bind: chain a result-returning async function after a successful result; short-circuit on failure. Cancellation is the responsibility of <paramref name="fn"/> — no <see cref="System.Threading.CancellationToken"/> is threaded through the API; callers needing cancellation pass a lambda that captures the token from its enclosing scope.
    /// </summary>
    public abstract Task<Result<TResult>> BindAsync<TResult>(Func<T, Task<Result<TResult>>> fn);

    /// <summary>LINQ-named alias of <see cref="Map"/>: transform the success value with <paramref name="selector"/>; pass the failure through unchanged.</summary>
    public Result<TResult> Select<TResult>(Func<T, TResult> selector) => Map(selector);

    /// <summary>LINQ-named alias of <see cref="Bind"/>: chain a result-returning function after a successful result; short-circuit on failure.</summary>
    public Result<TResult> SelectMany<TResult>(Func<T, Result<TResult>> selector) => Bind(selector);

    /// <summary>
    /// Monadic bind with projection — the shape the compiler binds for chained <c>from</c> clauses in query syntax. Equivalent to <c>Bind(x =&gt; selector(x).Map(y =&gt; resultSelector(x, y)))</c>: short-circuits on the first failure, so errors do not accumulate across clauses (bind is sequential; use <c>Result.Apply</c> when independent failures should all be reported).
    /// </summary>
    public Result<TResult> SelectMany<TIntermediate, TResult>(
        Func<T, Result<TIntermediate>> selector,
        Func<T, TIntermediate, TResult> resultSelector) =>
        Bind(x => selector(x).Map(y => resultSelector(x, y)));
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

    /// <summary>Construct a failed result from one or more <see cref="Error"/>s. Throws <see cref="ArgumentException"/> when <paramref name="errors"/> is empty — a failure must carry at least one error. <c>params ReadOnlySpan</c> keeps varargs argument lists off the heap; collection expressions and pre-built <see cref="ImmutableArray{T}"/>s bind to the <see cref="ImmutableArray{T}"/> overload instead (see its <see cref="OverloadResolutionPriorityAttribute"/>).</summary>
    public static Result<T> Failure<T>(params ReadOnlySpan<Error> errors) => errors.Length == 0
        ? throw new ArgumentException("Failure requires at least one error.", nameof(errors))
        : new Result<T>.Failure([.. errors]);

    /// <summary>Construct a failed result from a pre-built <see cref="ImmutableArray{T}"/> of <see cref="Error"/>s. Cheapest factory — the array is stored directly with no copy, and collection expressions (<c>Failure&lt;T&gt;([a, b])</c>) build it directly. Throws <see cref="ArgumentException"/> when <paramref name="errors"/> is default or empty. The priority attribute breaks the tie with the <see cref="ReadOnlySpan{T}"/> overload that <see cref="ImmutableArray{T}"/>'s implicit span conversion would otherwise cause.</summary>
    [OverloadResolutionPriority(1)]
    public static Result<T> Failure<T>(ImmutableArray<Error> errors) => errors.IsDefaultOrEmpty
        ? throw new ArgumentException("Failure requires at least one error.", nameof(errors))
        : new Result<T>.Failure(errors);

    /// <summary>Construct a failed result from a list of <see cref="Error"/>s. Throws <see cref="ArgumentException"/> when <paramref name="errors"/> is empty — a failure must carry at least one error.</summary>
    public static Result<T> Failure<T>(IReadOnlyList<Error> errors) => errors.Count == 0
        ? throw new ArgumentException("Failure requires at least one error.", nameof(errors))
        : new Result<T>.Failure([.. errors]);

    /// <summary>
    /// Lift a boolean check into a <see cref="Result{T}"/> of <see cref="Unit"/>: success when <paramref name="condition"/> holds, otherwise a failure carrying <paramref name="error"/>. The entry point for applicative accumulation — lift each independent check, then combine with the variadic <see cref="Apply(ReadOnlySpan{Result{Unit}})"/> overload so every violated condition is reported, not just the first.
    /// </summary>
    public static Result<Unit> Validate(bool condition, Error error) =>
        condition ? Success(Unit.Value) : new Result<Unit>.Failure([error]);

    /// <summary>
    /// Applicative application: feed a <see cref="Result{T}"/>-wrapped argument to a <see cref="Result{T}"/>-wrapped function. Multi-arity handled by currying — apply repeatedly to consume each argument. When both <paramref name="resultFn"/> and <paramref name="resultArg"/> fail, their errors are accumulated (function errors first, then argument errors); this is the Validation-applicative behaviour rather than fail-fast.
    /// </summary>
    public static Result<TResult> Apply<T, TResult>(Result<Func<T, TResult>> resultFn, Result<T> resultArg) =>
        (resultFn, resultArg) switch
        {
            (Result<Func<T, TResult>>.Success f, Result<T>.Success a) => Success(f.Value(a.Value)),
            (Result<Func<T, TResult>>.Failure f, Result<T>.Success _) => new Result<TResult>.Failure(f.Errors),
            (Result<Func<T, TResult>>.Success _, Result<T>.Failure a) => new Result<TResult>.Failure(a.Errors),
            // fourth row of the truth table, (Failure f, Failure a) => Failure([.. f.Errors, .. a.Errors]),
            // written as a discard: a final type-pattern would make the compiler synthesize unreachable
            // type-test/default branches under the switch, which coverlet counts and the branch-coverage
            // ratchet then fails.
            _ => new Result<TResult>.Failure(
                [.. ((Result<Func<T, TResult>>.Failure)resultFn).Errors, .. ((Result<T>.Failure)resultArg).Errors]),
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
}
