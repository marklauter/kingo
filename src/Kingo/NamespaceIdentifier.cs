using Results;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Values;

namespace Kingo;

/// <summary>
/// Identifies a <see cref="Namespace"/> — the <c>&lt;namespace&gt;</c> terminal of the fact grammar (see [[domain-language]]). Case-insensitive:
/// <see cref="Parse"/> normalizes to lowercase, the canonical form.
/// </summary>
public readonly record struct NamespaceIdentifier
    : IValue<NamespaceIdentifier, string>
{
    /// <inheritdoc/>
    public string Value { get; }

    /// <inheritdoc/>
    public static NamespaceIdentifier Create(string value) => new(value);

    /// <inheritdoc/>
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "lowercase is the canonical form of the identifier; the value is compared and stored, never round-tripped through case conversion")]
    public static Result<NamespaceIdentifier> Parse(string s) =>
        string.IsNullOrWhiteSpace(s)
            ? Result.Failure<NamespaceIdentifier>(Error.Validation("namespace_id.empty", "namespace identifier cannot be empty or whitespace"))
            : !NamespaceIdentifierPatterns.Validation().IsMatch(s)
                ? Result.Failure<NamespaceIdentifier>(Error.Validation("namespace_id.invalid", $"namespace identifier '{s}' contains invalid characters; expected '^[A-Za-z_][A-Za-z0-9_]*$'"))
                : Result.Success(new NamespaceIdentifier(s.ToLowerInvariant()));

    private NamespaceIdentifier(string value) => Value = value;

    /// <summary>Canonical text form: the underlying string value, unquoted and undecorated.</summary>
    public override string ToString() => Value;

    /// <inheritdoc/>
    public int CompareTo(NamespaceIdentifier other) => string.CompareOrdinal(Value, other.Value);

    /// <inheritdoc/>
    public static bool operator <(NamespaceIdentifier left, NamespaceIdentifier right) => left.CompareTo(right) < 0;

    /// <inheritdoc/>
    public static bool operator <=(NamespaceIdentifier left, NamespaceIdentifier right) => left.CompareTo(right) <= 0;

    /// <inheritdoc/>
    public static bool operator >(NamespaceIdentifier left, NamespaceIdentifier right) => left.CompareTo(right) > 0;

    /// <inheritdoc/>
    public static bool operator >=(NamespaceIdentifier left, NamespaceIdentifier right) => left.CompareTo(right) >= 0;

}

/// <summary>Character rules for <see cref="NamespaceIdentifier"/> — the terminal owns its grammar ([[domain-language]]).</summary>
internal static partial class NamespaceIdentifierPatterns
{
    private const RegexOptions PatternOptions =
        RegexOptions.ExplicitCapture |
        RegexOptions.Singleline |
        RegexOptions.CultureInvariant;

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*$", PatternOptions)]
    public static partial Regex Validation();
}
