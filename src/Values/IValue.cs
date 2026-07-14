using Results;
using System.Numerics;

namespace Values;

/// <summary>
/// Contract for a strongly-typed wrapper around a primitive <typeparamref name="TValue"/>. Distinguishes a trusted construction path (<see cref="Create"/>) from a validating, untrusted parse path (<see cref="Parse"/>), with a BCL-shaped <see cref="TryParse"/> adapter for boundary integration.
/// </summary>
/// <typeparam name="TSelf">The implementing wrapper type. Must be a struct and self-referential (CRTP) so static abstract members resolve through the type parameter at every call site.</typeparam>
/// <typeparam name="TValue">The underlying primitive type the wrapper carries (for example <see cref="string"/>, <see cref="int"/>, or <see cref="Guid"/>).</typeparam>
/// <remarks>
/// <list type="bullet">
///   <item><description><see cref="Create"/> performs no validation — it is the hot path for trusted sources (EF Core value converters, in-memory caches, internal code) where the value was validated on the way in.</description></item>
///   <item><description><see cref="Parse"/> performs full validation and returns a <see cref="Result{T}"/>. Use it at every untrusted boundary (request bodies, configuration files, user input).</description></item>
///   <item><description><see cref="TryParse"/> projects <see cref="Parse"/> into the BCL <c>bool</c>+<c>out</c> shape. Each implementor declares it on its own type — where ASP.NET Core's parameter binder and other reflection-based pipelines discover it — and delegates to <see cref="ValueParser.TryParse{TSelf, TValue}"/>.</description></item>
/// </list>
/// <para>
/// Wrappers also implement <see cref="IComparable{TSelf}"/>, <see cref="IEquatable{TSelf}"/>, and <see cref="IComparisonOperators{TSelf, TSelf, bool}"/> so they participate in sorting, equality, and ordered comparisons without extra ceremony at the call site.
/// </para>
/// </remarks>
public interface IValue<TSelf, TValue>
    : IComparable<TSelf>
    , IEquatable<TSelf>
    , IComparisonOperators<TSelf, TSelf, bool>
    where TSelf : struct, IValue<TSelf, TValue>
{
    /// <summary>
    /// Gets the underlying primitive value the wrapper carries.
    /// </summary>
    TValue Value { get; }

    /// <summary>
    /// Constructs a <typeparamref name="TSelf"/> from <paramref name="value"/> without validation. The caller asserts the source is trusted; misuse is the caller's defect.
    /// </summary>
    /// <param name="value">The pre-validated, or trusted value to wrap.</param>
    /// <returns>A new <typeparamref name="TSelf"/> instance.</returns>
    static abstract TSelf Create(TValue value);

    /// <summary>
    /// Parses <paramref name="s"/> with full validation, returning a <see cref="Result{TSelf}"/> that carries either the wrapped value or the structured <see cref="Error"/>s describing what failed. All parse and validation logic lives here — <see cref="TryParse"/> contributes none. Whether <see langword="null"/> is valid input is a business rule of the implementing type: a type for which null is invalid checks and returns a validation failure; a type for which it is valid does not check. Reflection-based callers can deliver null at runtime regardless of the parameter's non-nullable annotation.
    /// </summary>
    /// <param name="s">The untrusted input string.</param>
    /// <returns>
    /// <see cref="Result{TSelf}.Success"/> wrapping the constructed value when <paramref name="s"/> is well-formed and satisfies every validation rule; otherwise <see cref="Result{TSelf}.Failure"/> carrying one or more validation <see cref="Error"/>s.
    /// </returns>
    static abstract Result<TSelf> Parse(string s);

    /// <summary>
    /// Projects <see cref="Parse"/> into the BCL <c>bool</c>+<c>out</c> shape. TryParse never does parse or validation work of its own — no null checks, no rules; <see cref="Parse"/> owns 100% of that per the implementing type's business rules. The contract is <see langword="static"/> <see langword="abstract"/> so each implementor declares <c>TryParse</c> on its own type, where ASP.NET Core's parameter binder and other reflection-based pipelines discover it. Implementors delegate to <see cref="ValueParser.TryParse{TSelf, TValue}"/>.
    /// </summary>
    /// <param name="s">The untrusted input string.</param>
    /// <param name="parsed">When this method returns <see langword="true"/>, the parsed value; otherwise <see langword="default"/>.</param>
    /// <returns><see langword="true"/> when parsing succeeded; <see langword="false"/> otherwise.</returns>
    /// <example>
    /// The canonical one-line implementation on a wrapper:
    /// <code>
    /// public static bool TryParse(string s, out FilePath parsed)
    ///     =&gt; ValueParser.TryParse&lt;FilePath, string&gt;(s, out parsed);
    /// </code>
    /// </example>
    static abstract bool TryParse(string s, out TSelf parsed);
}
