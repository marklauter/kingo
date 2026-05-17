using Results;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Values;

/// <summary>
/// Contract for a strongly-typed wrapper around a primitive <typeparamref name="TValue"/>. Distinguishes a trusted construction path (<see cref="Create"/>) from a validating, untrusted parse path (<see cref="Parse"/>), with a BCL-shaped <see cref="TryParse"/> adapter for boundary integration.
/// </summary>
/// <typeparam name="TSelf">The implementing wrapper type. Must be self-referential (CRTP) so static abstract members resolve through the type parameter at every call site.</typeparam>
/// <typeparam name="TValue">The underlying primitive type the wrapper carries (for example <see cref="string"/>, <see cref="int"/>, or <see cref="Guid"/>).</typeparam>
/// <remarks>
/// <list type="bullet">
///   <item><description><see cref="Create"/> performs no validation — it is the hot path for trusted sources (EF Core value converters, in-memory caches, internal code) where the value was validated on the way in.</description></item>
///   <item><description><see cref="Parse"/> performs full validation and returns a <see cref="Result{T}"/>. Use it at every untrusted boundary (request bodies, configuration files, user input).</description></item>
///   <item><description><see cref="TryParse"/> projects <see cref="Parse"/> into the BCL <c>bool</c>+<c>out</c> shape. Each implementor declares it on its own type — where ASP.NET Core's parameter binder and other reflection-based pipelines discover it — and delegates to <see cref="Value.TryParse{TSelf, TValue}"/>.</description></item>
/// </list>
/// <para>
/// Wrappers also implement <see cref="IComparable{TSelf}"/>, <see cref="IEquatable{TSelf}"/>, and <see cref="IComparisonOperators{TSelf, TSelf, bool}"/> so they participate in sorting, equality, and ordered comparisons without extra ceremony at the call site.
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
    /// Parses <paramref name="s"/> with full validation, returning a <see cref="Result{TSelf}"/> that carries either the wrapped value or the structured <see cref="Error"/>s describing what failed.
    /// </summary>
    /// <param name="s">The untrusted input string.</param>
    /// <returns>
    /// <see cref="Result{TSelf}.Success"/> wrapping the constructed value when <paramref name="s"/> is well-formed and satisfies every validation rule; otherwise <see cref="Result{TSelf}.Failure"/> carrying one or more validation <see cref="Error"/>s.
    /// </returns>
    static abstract Result<TSelf> Parse(string s);

    /// <summary>
    /// Parses <paramref name="s"/>, projecting <see cref="Parse"/> into the BCL <c>bool</c>+<c>out</c> shape. The contract is <see langword="static"/> <see langword="abstract"/> so each implementor declares <c>TryParse</c> on its own type, where ASP.NET Core's parameter binder and other reflection-based pipelines discover it. Implementors delegate to <see cref="Value.TryParse{TSelf, TValue}"/>.
    /// </summary>
    /// <param name="s">The untrusted input string.</param>
    /// <param name="parsed">When this method returns <see langword="true"/>, the parsed value; otherwise <see langword="default"/>.</param>
    /// <returns><see langword="true"/> when parsing succeeded; <see langword="false"/> otherwise.</returns>
    /// <example>
    /// The canonical one-line implementation on a wrapper:
    /// <code>
    /// public static bool TryParse(string s, [MaybeNullWhen(false)] out FilePath parsed)
    ///     =&gt; Value.TryParse&lt;FilePath, string&gt;(s, out parsed);
    /// </code>
    /// </example>
    static abstract bool TryParse(string s, [MaybeNullWhen(false)] out TSelf parsed);
}

/// <summary>
/// Static helpers shared by <see cref="IValue{TSelf, TValue}"/> implementations. Houses the canonical <c>TryParse</c> body so each implementor's <c>TryParse</c> declaration can be a one-line delegation, keeping the parse-to-<c>bool</c>+<c>out</c> projection in one place across every wrapper.
/// </summary>
public static class Value
{
    /// <summary>
    /// Canonical <c>TryParse</c> body for <see cref="IValue{TSelf, TValue}"/> implementors: invokes <typeparamref name="TSelf"/>'s <see cref="IValue{TSelf, TValue}.Parse"/> and projects the <see cref="Result{T}"/> into the BCL <c>bool</c>+<c>out</c> shape, discarding any accumulated <see cref="Error"/>s on failure.
    /// </summary>
    /// <typeparam name="TSelf">The wrapper type implementing <see cref="IValue{TSelf, TValue}"/>.</typeparam>
    /// <typeparam name="TValue">The underlying primitive type the wrapper carries.</typeparam>
    /// <param name="s">The untrusted input string.</param>
    /// <param name="parsed">When this method returns <see langword="true"/>, the parsed value; otherwise <see langword="default"/>.</param>
    /// <returns><see langword="true"/> when parsing succeeded; <see langword="false"/> otherwise.</returns>
    public static bool TryParse<TSelf, TValue>(
        string s,
        [MaybeNullWhen(false)] out TSelf parsed)
        where TSelf : IValue<TSelf, TValue>
    {
        if (TSelf.Parse(s) is Result<TSelf>.Success success)
        {
            parsed = success.Value;
            return true;
        }

        parsed = default;
        return false;
    }
}
