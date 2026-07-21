using System.Numerics;

namespace Values;

/// <summary>
/// Contract for a strongly-typed wrapper embedding a primitive <typeparamref name="TValue"/> in the domain. Two arrows in, one arrow out: the fallible lift
/// <see cref="IParse{TSelf}.Parse"/> (<c>string → Result&lt;TSelf&gt;</c>, inherited — validation lives there and only there), the unchecked embedding
/// <see cref="Unchecked"/> (<c>TValue → TSelf</c> — pure assignment, safe only on the valid subset the caller vouches for), and the projection
/// <see cref="Value"/> (<c>TSelf → TValue</c>) back to the primitive.
/// </summary>
/// <typeparam name="TSelf">The implementing wrapper type. Must be a struct and self-referential (CRTP) so static abstract members resolve through the type
/// parameter at every call site.</typeparam>
/// <typeparam name="TValue">The underlying primitive type the wrapper carries (for example <see cref="string"/>, <see cref="int"/>, or
/// <see cref="Guid"/>).</typeparam>
/// <remarks>
/// <para>
/// The BCL <c>bool</c>+<c>out</c> <c>TryParse</c> shape is deliberately not part of this contract — it is a REST-binding concern. Types that cross the
/// ASP.NET boundary opt in via <see cref="ITryParse{TSelf}"/>.
/// </para>
/// <para>
/// Wrappers also implement <see cref="IComparable{TSelf}"/>, <see cref="IEquatable{TSelf}"/>, and <see cref="IComparisonOperators{TSelf, TSelf, bool}"/> so
/// they participate in sorting, equality, and ordered comparisons without extra ceremony at the call site.
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
    /// The projection back to the primitive: the underlying <typeparamref name="TValue"/> the wrapper carries.
    /// </summary>
    TValue Value { get; }

    /// <summary>
    /// The unchecked embedding <c>TValue → TSelf</c>: pure assignment — no validation, no normalization — total over the primitive but lawful only on its
    /// valid subset. The caller asserts the source is trusted: <paramref name="value"/> is valid and already in canonical form. Misuse is the caller's defect.
    /// </summary>
    /// <param name="value">The trusted value to wrap — pre-validated and canonical.</param>
    /// <returns>A new <typeparamref name="TSelf"/> instance.</returns>
    static abstract TSelf Unchecked(TValue value);
}
