using Results;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Values;

namespace Kingo;

/// <summary>
/// A bare relationship name, the <c>&lt;relationship name&gt;</c> production of the identifier grammar ([[identifiers]]), one segment: <c>viewer</c>. Not an
/// identity on its own: it names a relationship only against a namespace supplied from elsewhere. Two places supply one: a
/// <see cref="Kingo.Graphs.SubjectSet"/>, where the resource carries the namespace, and the rewrite algebra, where the resource under evaluation does. There is
/// no qualified relationship type. Nothing holds one, so the qualified form is composed at the point of use if a use ever arises. Case-insensitive:
/// <see cref="Parse"/> normalizes to lowercase, the canonical form. The grammar is name-only, so <c>...</c> fails to parse here. That form is the
/// <c>#...</c> marker of the <c>Fact.ResourceFact</c> member production, fact-grammar punctuation rather than a relationship concept.
/// </summary>
public readonly record struct RelationshipName
    : IValue<RelationshipName, string>
{
    /// <inheritdoc/>
    public string Value { get; }

    /// <inheritdoc/>
    public static RelationshipName Unchecked(string value) => new(value);

    /// <inheritdoc/>
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "lowercase is the canonical form of the identifier; the value is compared and stored, never round-tripped through case conversion")]
    public static Result<RelationshipName> Parse(string s) =>
        string.IsNullOrWhiteSpace(s)
            ? Result.Failure<RelationshipName>(Error.Validation("relationship_name.empty", "relationship name cannot be empty or whitespace"))
            : !RelationshipNamePatterns.Validation().IsMatch(s)
                ? Result.Failure<RelationshipName>(Error.Validation("relationship_name.invalid", $"relationship name '{s}' is malformed; expected '{IdentifierGrammar.NamePattern}'"))
                : Result.Success(new RelationshipName(s.ToLowerInvariant()));

    private RelationshipName(string value) => Value = value;

    /// <summary>Returns the canonical text form of the value.</summary>
    /// <returns>The underlying string, unquoted and undecorated.</returns>
    public override string ToString() => Value;

    /// <inheritdoc/>
    public int CompareTo(RelationshipName other) => string.CompareOrdinal(Value, other.Value);

    /// <inheritdoc/>
    public static bool operator <(RelationshipName left, RelationshipName right) => left.CompareTo(right) < 0;

    /// <inheritdoc/>
    public static bool operator <=(RelationshipName left, RelationshipName right) => left.CompareTo(right) <= 0;

    /// <inheritdoc/>
    public static bool operator >(RelationshipName left, RelationshipName right) => left.CompareTo(right) > 0;

    /// <inheritdoc/>
    public static bool operator >=(RelationshipName left, RelationshipName right) => left.CompareTo(right) >= 0;

}

/// <summary>Character rules for <see cref="RelationshipName"/>: one name, composed from <see cref="IdentifierGrammar"/> ([[identifiers]]).</summary>
internal static partial class RelationshipNamePatterns
{
    private const RegexOptions PatternOptions =
        RegexOptions.ExplicitCapture |
        RegexOptions.Singleline |
        RegexOptions.CultureInvariant;

    // name-only: '...' is not a relationship — it is the '#...' marker of the ResourceFact member production ([[identifiers]])
    [GeneratedRegex(IdentifierGrammar.NamePattern, PatternOptions)]
    public static partial Regex Validation();
}
