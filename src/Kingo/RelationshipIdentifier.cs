using Results;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Values;

namespace Kingo;

/// <summary>
/// Names a relationship — the <c>&lt;relationship&gt;</c> terminal of the tuple grammar (see docs/notes/domain-language.md). Case-insensitive: <see cref="Parse"/> normalizes to lowercase, the canonical form.
/// </summary>
public readonly record struct RelationshipIdentifier
    : IValue<RelationshipIdentifier, string>
{
    /// <inheritdoc/>
    public string Value { get; }

    /// <summary>The <c>...</c> sentinel — Zanzibar's tuple-grammar marker for an unspecified relationship (paper §2.1); a domain concept, not a PDL-ism.</summary>
    public static RelationshipIdentifier Nothing { get; } = Create("...");

    /// <inheritdoc/>
    public static RelationshipIdentifier Create(string value) => new(value);

    /// <inheritdoc/>
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "lowercase is the canonical form of the identifier; the value is compared and stored, never round-tripped through case conversion")]
    public static Result<RelationshipIdentifier> Parse(string s) =>
        string.IsNullOrWhiteSpace(s)
            ? Result.Failure<RelationshipIdentifier>(Error.Validation("relationship_id.empty", "relationship identifier cannot be empty or whitespace"))
            : !RelationshipIdentifierPatterns.Validation().IsMatch(s)
            ? Result.Failure<RelationshipIdentifier>(Error.Validation("relationship_id.invalid", $"relationship identifier '{s}' contains invalid characters; expected '^\\.\\.\\.$|^[A-Za-z_][A-Za-z0-9_]*$'"))
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

/// <summary>Character rules for <see cref="RelationshipIdentifier"/> — the terminal owns its grammar (docs/notes/domain-language.md).</summary>
internal static partial class RelationshipIdentifierPatterns
{
    private const RegexOptions PatternOptions =
        RegexOptions.ExplicitCapture |
        RegexOptions.Singleline |
        RegexOptions.CultureInvariant;

    // allows the ... literal used by RelationshipIdentifier.Nothing
    [GeneratedRegex(@"^\.\.\.$|^[A-Za-z_][A-Za-z0-9_]*$", PatternOptions)]
    public static partial Regex Validation();
}
