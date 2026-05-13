using System.Numerics;

namespace Kingo;

/// <summary>
/// Contract for a strongly-typed wrapper around a primitive <typeparamref name="TValue"/>. Distinguishes a fast, trusted construction path (<see cref="Create"/>) from a validating, untrusted parse path (<see cref="Parse"/>), with a BCL-shaped <see cref="TryParse"/> adapter for boundary integration.
/// </summary>
/// <typeparam name="TSelf">The implementing wrapper type. Must be self-referential (CRTP) so static abstract members resolve through the type parameter at every call site.</typeparam>
/// <typeparam name="TValue">The underlying primitive type the wrapper carries (for example <see cref="string"/>, <see cref="int"/>, or <see cref="Guid"/>).</typeparam>
/// <remarks>
/// <para>
/// The two entry points encode different trust assumptions in the type system:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="Create"/> performs no validation — it is the hot path for trusted sources (EF Core value converters, in-memory caches, internal code) where the value was validated on the way in.</description></item>
///   <item><description><see cref="Parse"/> performs full validation and returns a <see cref="Result{T}"/>. Use it at every untrusted boundary (request bodies, configuration files, user input).</description></item>
///   <item><description><see cref="TryParse"/> projects <see cref="Parse"/> into the BCL <c>bool</c>+<c>out</c> shape so ASP.NET Core's parameter binder and other reflection-based pipelines can discover the type and consume it directly.</description></item>
/// </list>
/// <para>
/// Wrappers also inherit <see cref="IComparable{T}"/>, <see cref="IEquatable{T}"/>, and <see cref="IComparisonOperators{TSelf, TOther, TResult}"/> so they participate in sorting, equality, and ordered comparisons without extra ceremony at the call site.
/// </para>
/// </remarks>
public interface IValue<TSelf, TValue>
    : IComparable<TSelf>
    , IEquatable<TSelf>
    , IComparisonOperators<TSelf, TSelf, bool>
    where TSelf : IValue<TSelf, TValue>
{
    /// <summary>
    /// Gets the underlying primitive value the wrapper carries.
    /// </summary>
    TValue Value { get; }

    /// <summary>
    /// Constructs a <typeparamref name="TSelf"/> from <paramref name="value"/> without validation. The caller asserts the source is trusted; misuse is the caller's defect.
    /// </summary>
    /// <param name="value">The pre-validated value to wrap.</param>
    /// <returns>A new <typeparamref name="TSelf"/> instance.</returns>
    static abstract TSelf Create(TValue value);

    /// <summary>
    /// Parses <paramref name="s"/> with full validation, returning a <see cref="Result{T}"/> that carries either the wrapped value or the structured <see cref="Error"/> describing what failed.
    /// </summary>
    /// <param name="s">The untrusted input string.</param>
    /// <returns>
    /// <see cref="Success{TSelf}"/> wrapping the constructed value when <paramref name="s"/> is well-formed and satisfies every validation rule; otherwise <see cref="Failure{TSelf}"/> carrying the validation <see cref="Error"/>.
    /// </returns>
    static abstract Result<TSelf> Parse(string s);

    /// <summary>
    /// Attempts to parse <paramref name="s"/>, projecting <see cref="Parse"/> into the BCL <c>bool</c>+<c>out</c> shape. Default implementation delegates to <see cref="Parse"/> and discards the <see cref="Error"/> on failure; implementors may override to provide a hand-tuned hot path.
    /// </summary>
    /// <param name="s">The untrusted input string.</param>
    /// <param name="parsed">When this method returns <see langword="true"/>, the parsed value; otherwise <see langword="default"/>.</param>
    /// <returns><see langword="true"/> when parsing succeeded; <see langword="false"/> otherwise.</returns>
    static virtual bool TryParse(string s, out TSelf parsed)
    {
        if (TSelf.Parse(s) is Success<TSelf> success)
        {
            parsed = success.Value;
            return true;
        }
        parsed = default!;
        return false;
    }
}
