using System.Numerics;

namespace Values;

/// <summary>
/// Contract for a strongly-typed wrapper embedding a primitive <typeparamref name="TValue"/> in the domain. Two arrows in, one arrow out. The fallible lift
/// <see cref="IParse{TSelf}.Parse"/> (<c>string → Result&lt;TSelf&gt;</c>) is inherited, and validation lives there and only there. The unchecked embedding
/// <see cref="Unchecked"/> (<c>TValue → TSelf</c>) is pure assignment, safe only on the valid subset the caller vouches for. The projection
/// <see cref="Value"/> (<c>TSelf → TValue</c>) goes back to the primitive.
/// </summary>
/// <typeparam name="TSelf">The implementing wrapper type. Must be a struct and self-referential (CRTP) so static abstract members resolve through the type
/// parameter at every call site.</typeparam>
/// <typeparam name="TValue">The underlying primitive type the wrapper carries (for example <see cref="string"/>, <see cref="int"/>, or
/// <see cref="Guid"/>).</typeparam>
/// <remarks>
/// <para>
/// The BCL <c>bool</c>+<c>out</c> <c>TryParse</c> shape is deliberately not part of this contract: it is a REST-binding concern. Types that cross the
/// ASP.NET boundary opt in via <see cref="ITryParse{TSelf}"/>.
/// </para>
/// <para>
/// Wrappers also implement <see cref="IComparable{TSelf}"/>, <see cref="IEquatable{TSelf}"/>, and <see cref="IComparisonOperators{TSelf, TSelf, bool}"/> so
/// they participate in sorting, equality, and ordered comparisons.
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
    /// Unchecked embedding <c>TValue → TSelf</c>. Assigns <paramref name="value"/> directly: no validation, no normalization. Total over the primitive but
    /// lawful only on its valid subset. The caller asserts the source is trusted, so <paramref name="value"/> is valid and already in canonical form. Misuse is
    /// the caller's defect.
    /// </summary>
    /// <param name="value">The trusted value to wrap, already pre-validated and canonical.</param>
    /// <returns>A new <typeparamref name="TSelf"/> instance.</returns>
    static abstract TSelf Unchecked(TValue value);
}
