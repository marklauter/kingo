using Results;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Values;

namespace Kingo;

/// <summary>
/// Names a relationship — the <c>&lt;relationship&gt;</c> terminal of the fact grammar (see [[domain-language]]). Case-insensitive:
/// <see cref="Parse"/> normalizes to lowercase, the canonical form. The grammar is name-only: <c>...</c> is not a relationship — it is the
/// <c>#...</c> marker of the <c>Fact.ResourceFact</c> member production (fact-grammar punctuation, not a relationship concept), so it fails to parse here.
/// </summary>
public readonly record struct RelationshipPath
    : IValue<RelationshipPath, string>
{
    /// <inheritdoc/>
    public string Value { get; }

    /// <inheritdoc/>
    public static RelationshipPath Unchecked(string value) => new(value);

    /// <inheritdoc/>
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "lowercase is the canonical form of the identifier; the value is compared and stored, never round-tripped through case conversion")]
    public static Result<RelationshipPath> Parse(string s) =>
        string.IsNullOrWhiteSpace(s)
            ? Result.Failure<RelationshipPath>(Error.Validation("relationship_path.empty", "relationship path cannot be empty or whitespace"))
            : !RelationshipPathPatterns.Validation().IsMatch(s)
                ? Result.Failure<RelationshipPath>(Error.Validation("relationship_path.invalid", $"relationship path '{s}' contains invalid characters; expected '^[A-Za-z_][A-Za-z0-9_]*$'"))
                : Result.Success(new RelationshipPath(s.ToLowerInvariant()));

    private RelationshipPath(string value) => Value = value;

    /// <summary>Canonical text form: the underlying string value, unquoted and undecorated.</summary>
    public override string ToString() => Value;

    /// <inheritdoc/>
    public int CompareTo(RelationshipPath other) => string.CompareOrdinal(Value, other.Value);

    /// <inheritdoc/>
    public static bool operator <(RelationshipPath left, RelationshipPath right) => left.CompareTo(right) < 0;

    /// <inheritdoc/>
    public static bool operator <=(RelationshipPath left, RelationshipPath right) => left.CompareTo(right) <= 0;

    /// <inheritdoc/>
    public static bool operator >(RelationshipPath left, RelationshipPath right) => left.CompareTo(right) > 0;

    /// <inheritdoc/>
    public static bool operator >=(RelationshipPath left, RelationshipPath right) => left.CompareTo(right) >= 0;

}

/// <summary>Character rules for <see cref="RelationshipPath"/> — the terminal owns its grammar ([[domain-language]]).</summary>
internal static partial class RelationshipPathPatterns
{
    private const RegexOptions PatternOptions =
        RegexOptions.ExplicitCapture |
        RegexOptions.Singleline |
        RegexOptions.CultureInvariant;

    // name-only: '...' is not a relationship — it is the '#...' marker of the ResourceFact member production ([[domain-language]])
    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*$", PatternOptions)]
    public static partial Regex Validation();
}
