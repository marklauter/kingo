using Results;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Values;

namespace Kingo;

/// <summary>
/// The name of a <see cref="Schemas.Namespace"/> within its spec, one segment of the identifier grammar ([[identifiers]]): <c>file</c>. Bare, because the
/// config side is a tree. A namespace lives inside the spec that owns it, so containment supplies the qualification and nothing on that side ever holds a
/// qualified path. The fact side is the other case: a fact points at a namespace it does not live inside, so its reference carries the qualifier as a
/// <see cref="NamespacePath"/>. Case-insensitive: <see cref="Parse"/> normalizes to lowercase, the canonical form.
/// </summary>
public readonly record struct NamespaceName
    : IValue<NamespaceName, string>
{
    /// <inheritdoc/>
    public string Value { get; }

    /// <inheritdoc/>
    public static NamespaceName Unchecked(string value) => new(value);

    /// <inheritdoc/>
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "lowercase is the canonical form of the identifier; the value is compared and stored, never round-tripped through case conversion")]
    public static Result<NamespaceName> Parse(string s) =>
        string.IsNullOrWhiteSpace(s)
            ? Result.Failure<NamespaceName>(Error.Validation("namespace_name.empty", "namespace name cannot be empty or whitespace"))
            : !NamespaceNamePatterns.Validation().IsMatch(s)
                ? Result.Failure<NamespaceName>(Error.Validation("namespace_name.invalid", $"namespace name '{s}' is malformed; expected '{IdentifierGrammar.NamePattern}'"))
                : Result.Success(new NamespaceName(s.ToLowerInvariant()));

    private NamespaceName(string value) => Value = value;

    /// <summary>Returns the canonical text form of the value.</summary>
    /// <returns>The underlying string, unquoted and undecorated.</returns>
    public override string ToString() => Value;

    /// <inheritdoc/>
    public int CompareTo(NamespaceName other) => string.CompareOrdinal(Value, other.Value);

    /// <inheritdoc/>
    public static bool operator <(NamespaceName left, NamespaceName right) => left.CompareTo(right) < 0;

    /// <inheritdoc/>
    public static bool operator <=(NamespaceName left, NamespaceName right) => left.CompareTo(right) <= 0;

    /// <inheritdoc/>
    public static bool operator >(NamespaceName left, NamespaceName right) => left.CompareTo(right) > 0;

    /// <inheritdoc/>
    public static bool operator >=(NamespaceName left, NamespaceName right) => left.CompareTo(right) >= 0;

}

/// <summary>Character rules for <see cref="NamespaceName"/>: one name, composed from <see cref="IdentifierGrammar"/> ([[identifiers]]).</summary>
internal static partial class NamespaceNamePatterns
{
    private const RegexOptions PatternOptions =
        RegexOptions.ExplicitCapture |
        RegexOptions.Singleline |
        RegexOptions.CultureInvariant;

    [GeneratedRegex(IdentifierGrammar.NamePattern, PatternOptions)]
    public static partial Regex Validation();
}
