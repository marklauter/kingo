using System.Numerics;

namespace Values;

/// <summary>
/// Contract for a strongly-typed wrapper around a primitive <typeparamref name="TValue"/>. Distinguishes a trusted construction path (<see cref="Create"/>) from the validating, untrusted parse path it inherits from <see cref="IParse{TSelf}"/>.
/// </summary>
/// <typeparam name="TSelf">The implementing wrapper type. Must be a struct and self-referential (CRTP) so static abstract members resolve through the type parameter at every call site.</typeparam>
/// <typeparam name="TValue">The underlying primitive type the wrapper carries (for example <see cref="string"/>, <see cref="int"/>, or <see cref="Guid"/>).</typeparam>
/// <remarks>
/// <list type="bullet">
///   <item><description><see cref="Create"/> performs no validation — it is the hot path for trusted sources (EF Core value converters, in-memory caches, internal code) where the value was validated on the way in.</description></item>
///   <item><description><see cref="IParse{TSelf}.Parse"/> performs full validation and returns a <see cref="Results.Result{T}"/>. Use it at every untrusted boundary (request bodies, configuration files, user input).</description></item>
///   <item><description>The BCL <c>bool</c>+<c>out</c> <c>TryParse</c> shape is deliberately not part of this contract — it is a REST-binding concern. Types that cross the ASP.NET boundary opt in via <see cref="ITryParse{TSelf}"/>.</description></item>
/// </list>
/// <para>
/// Wrappers also implement <see cref="IComparable{TSelf}"/>, <see cref="IEquatable{TSelf}"/>, and <see cref="IComparisonOperators{TSelf, TSelf, bool}"/> so they participate in sorting, equality, and ordered comparisons without extra ceremony at the call site.
/// </para>
/// </remarks>
public interface IValue<TSelf, TValue>
    : IParse<TSelf>
    , IComparable<TSelf>
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
}
