using Results;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Values;

namespace Kingo;

/// <summary>
/// A qualified reference to a namespace, the <c>&lt;namespace&gt;</c> production of the identifier grammar ([[identifiers]]): a spec name, <c>/</c>, a
/// namespace name, as in <c>io/file</c>. There is no namespace called <c>file</c>. The only qualified identifier Kingo holds, and it exists for the fact side
/// alone: a <c>Kingo.Graphs.Resource</c> points at a namespace it does not live inside, so the qualifier has to travel with the reference. The config side is a
/// tree, because a spec owns its namespaces, so containment supplies the qualification there and nothing in <c>Kingo.Schemas</c> holds one of these
/// ([[split-identities-at-ownership-boundaries]]). One string with one representation, ordered so a spec's namespaces are contiguous in the key space. The spec
/// and namespace segments are deliberately not projected off it: the value is stored, compared, and sorted whole, and nothing reads the halves. Deriving them
/// would cost a character scan (at construction if eager, per read if lazy) that no caller needs today, so it is deferred to an extension method if a use ever
/// arises. Case-insensitive: <see cref="Parse"/> normalizes to lowercase, the canonical form.
/// </summary>
public readonly record struct NamespacePath
    : IValue<NamespacePath, string>
{
    /// <inheritdoc/>
    public string Value { get; }

    /// <inheritdoc/>
    public static NamespacePath Unchecked(string value) => new(value);

    /// <inheritdoc/>
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "lowercase is the canonical form of the identifier; the value is compared and stored, never round-tripped through case conversion")]
    public static Result<NamespacePath> Parse(string s) =>
        string.IsNullOrWhiteSpace(s)
            ? Result.Failure<NamespacePath>(Error.Validation("namespace_path.empty", "namespace path cannot be empty or whitespace"))
            : !NamespacePathPatterns.Validation().IsMatch(s)
                ? Result.Failure<NamespacePath>(Error.Validation("namespace_path.invalid", $"namespace path '{s}' is malformed; expected '{IdentifierGrammar.NamespacePathPattern}'"))
                : Result.Success(new NamespacePath(s.ToLowerInvariant()));

    private NamespacePath(string value) => Value = value;

    /// <summary>Returns the canonical text form of the value.</summary>
    /// <returns>The underlying string, unquoted and undecorated.</returns>
    public override string ToString() => Value;

    /// <inheritdoc/>
    public int CompareTo(NamespacePath other) => string.CompareOrdinal(Value, other.Value);

    /// <inheritdoc/>
    public static bool operator <(NamespacePath left, NamespacePath right) => left.CompareTo(right) < 0;

    /// <inheritdoc/>
    public static bool operator <=(NamespacePath left, NamespacePath right) => left.CompareTo(right) <= 0;

    /// <inheritdoc/>
    public static bool operator >(NamespacePath left, NamespacePath right) => left.CompareTo(right) > 0;

    /// <inheritdoc/>
    public static bool operator >=(NamespacePath left, NamespacePath right) => left.CompareTo(right) >= 0;

}

/// <summary>Character rules for <see cref="NamespacePath"/>: two names around a <c>/</c>, composed from <see cref="IdentifierGrammar"/> ([[identifiers]]).</summary>
internal static partial class NamespacePathPatterns
{
    private const RegexOptions PatternOptions =
        RegexOptions.ExplicitCapture |
        RegexOptions.Singleline |
        RegexOptions.CultureInvariant;

    [GeneratedRegex(IdentifierGrammar.NamespacePathPattern, PatternOptions)]
    public static partial Regex Validation();
}
