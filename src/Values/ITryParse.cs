using System.Diagnostics.CodeAnalysis;

namespace Values;

/// <summary>
/// The REST-boundary opt-in: the BCL <c>bool</c>+<c>out</c> shape that ASP.NET Core's parameter binder and other reflection-based pipelines discover on the
/// concrete type. Only types that cross that boundary implement this. It is deliberately <b>not</b> part of <see cref="IValue{TSelf, TValue}"/>, so
/// value types that never bind from a route or query string don't carry transport concerns. Extends <see cref="IParse{TSelf}"/> because <see cref="TryParse"/>
/// is its lossy projection: the accumulated <see cref="Results.Error"/>s are discarded.
/// </summary>
/// <typeparam name="TSelf">The implementing type. Self-referential (CRTP) so the static abstract member resolves through the type parameter at every call
/// site.</typeparam>
public interface ITryParse<TSelf> : IParse<TSelf>
    where TSelf : ITryParse<TSelf>
{
    /// <summary>
    /// Projects <see cref="IParse{TSelf}.Parse"/> into the BCL <c>bool</c>+<c>out</c> shape. TryParse does no parse or validation work of its own: no null
    /// checks, no rules. <see cref="IParse{TSelf}.Parse"/> owns all of that per the implementing type's business rules. The contract is
    /// <see langword="static"/> <see langword="abstract"/> so each implementor declares <c>TryParse</c> on its own type, where reflection-based pipelines
    /// discover it. An inherited member would not be found. Implementors delegate to <see cref="ValueParser.TryParse{TSelf}"/>.
    /// </summary>
    /// <param name="s">The untrusted input string.</param>
    /// <param name="parsed">When this method returns <see langword="true"/>, the parsed value; otherwise <see langword="default"/>.</param>
    /// <returns><see langword="true"/> when parsing succeeded; <see langword="false"/> otherwise.</returns>
    /// <example>
    /// The canonical one-line implementation:
    /// <code>
    /// public static bool TryParse(string s, out FilePath parsed)
    ///     =&gt; ValueParser.TryParse(s, out parsed);
    /// </code>
    /// </example>
    static abstract bool TryParse(string s, [MaybeNullWhen(false)] out TSelf parsed);
}
