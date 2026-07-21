using Results;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Values;

namespace Kingo;

/// <summary>
/// Names a relationship — the <c>&lt;relationship&gt;</c> terminal of the tuple grammar (see [[domain-language]]). Case-insensitive:
/// <see cref="Parse"/> normalizes to lowercase, the canonical form. The grammar is name-only: <c>...</c> is not a relationship — it is the
/// <c>#...</c> marker of the <c>Fact.ResourceFact</c> member production (tuple-grammar punctuation, not a relationship concept), so it fails to parse here.
/// </summary>
public readonly record struct RelationshipIdentifier
    : IValue<RelationshipIdentifier, string>
{
    /// <inheritdoc/>
    public string Value { get; }

    /// <inheritdoc/>
    public static RelationshipIdentifier Create(string value) => new(value);

    /// <inheritdoc/>
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "lowercase is the canonical form of the identifier; the value is compared and stored, never round-tripped through case conversion")]
    public static Result<RelationshipIdentifier> Parse(string s) =>
        string.IsNullOrWhiteSpace(s)
            ? Result.Failure<RelationshipIdentifier>(Error.Validation("relationship_id.empty", "relationship identifier cannot be empty or whitespace"))
            : !RelationshipIdentifierPatterns.Validation().IsMatch(s)
                ? Result.Failure<RelationshipIdentifier>(Error.Validation("relationship_id.invalid", $"relationship identifier '{s}' contains invalid characters; expected '^[A-Za-z_][A-Za-z0-9_]*$'"))
                : Result.Success(new RelationshipIdentifier(s.ToLowerInvariant()));

    private RelationshipIdentifier(string value) => Value = value;

    /// <summary>Canonical text form: the underlying string value, unquoted and undecorated.</summary>
    public override string ToString() => Value;

    /// <inheritdoc/>
    public int CompareTo(RelationshipIdentifier other) => string.CompareOrdinal(Value, other.Value);

    /// <inheritdoc/>
    public static bool operator <(RelationshipIdentifier left, RelationshipIdentifier right) => left.CompareTo(right) < 0;

    /// <inheritdoc/>
    public static bool operator <=(RelationshipIdentifier left, RelationshipIdentifier right) => left.CompareTo(right) <= 0;

    /// <inheritdoc/>
    public static bool operator >(RelationshipIdentifier left, RelationshipIdentifier right) => left.CompareTo(right) > 0;

    /// <inheritdoc/>
    public static bool operator >=(RelationshipIdentifier left, RelationshipIdentifier right) => left.CompareTo(right) >= 0;

}

/// <summary>Character rules for <see cref="RelationshipIdentifier"/> — the terminal owns its grammar ([[domain-language]]).</summary>
internal static partial class RelationshipIdentifierPatterns
{
    private const RegexOptions PatternOptions =
        RegexOptions.ExplicitCapture |
        RegexOptions.Singleline |
        RegexOptions.CultureInvariant;

    // name-only: '...' is not a relationship — it is the '#...' marker of the ResourceFact member production ([[domain-language]])
    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*$", PatternOptions)]
    public static partial Regex Validation();
}
