using Results;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Values;

namespace Kingo;

/// <summary>
/// A bare relation name, the <c>⟨relation name⟩</c> production of the identifier grammar ([[identifiers]]), one segment: <c>viewer</c>. Not an
/// identity on its own: it names a relation only against a namespace supplied from elsewhere. Two places supply one: a
/// <see cref="Kingo.Facts.SubjectSet"/>, where the resource carries the namespace, and the rewrite algebra, where the resource under evaluation does. There is
/// no qualified relation type. Nothing holds one, so the qualified form is composed at the point of use if a use ever arises. Case-insensitive:
/// <see cref="Parse"/> normalizes to lowercase, the canonical form. The grammar is name-only, so <c>...</c> fails to parse here. That form is the
/// <c>#...</c> marker of the <c>Fact.ResourceFact</c> member production, fact-grammar punctuation rather than a relation concept.
/// </summary>
public readonly record struct RelationName
    : IValue<RelationName, string>
{
    /// <inheritdoc/>
    public string Value { get; }

    /// <inheritdoc/>
    public static RelationName Unchecked(string value) => new(value);

    /// <inheritdoc/>
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "lowercase is the canonical form of the identifier; the value is compared and stored, never round-tripped through case conversion")]
    public static Result<RelationName> Parse(string s) =>
        string.IsNullOrWhiteSpace(s)
            ? Result.Failure<RelationName>(Error.Validation(Diagnostics.ErrorCodes.RelationName.Empty, "relation name cannot be empty or whitespace"))
            : !RelationNamePatterns.Validation().IsMatch(s)
                ? Result.Failure<RelationName>(Error.Validation(Diagnostics.ErrorCodes.RelationName.Invalid, $"relation name '{s}' is malformed; expected '{IdentifierGrammar.NamePattern}'"))
                : Result.Success(new RelationName(s.ToLowerInvariant()));

    private RelationName(string value) => Value = value;

    /// <summary>Returns the canonical text form of the value.</summary>
    /// <returns>The underlying string, unquoted and undecorated.</returns>
    public override string ToString() => Value;

    /// <inheritdoc/>
    public int CompareTo(RelationName other) => string.CompareOrdinal(Value, other.Value);

    /// <inheritdoc/>
    public static bool operator <(RelationName left, RelationName right) => left.CompareTo(right) < 0;

    /// <inheritdoc/>
    public static bool operator <=(RelationName left, RelationName right) => left.CompareTo(right) <= 0;

    /// <inheritdoc/>
    public static bool operator >(RelationName left, RelationName right) => left.CompareTo(right) > 0;

    /// <inheritdoc/>
    public static bool operator >=(RelationName left, RelationName right) => left.CompareTo(right) >= 0;

}

/// <summary>Character rules for <see cref="RelationName"/>: one name, composed from <see cref="IdentifierGrammar"/> ([[identifiers]]).</summary>
internal static partial class RelationNamePatterns
{
    private const RegexOptions PatternOptions =
        RegexOptions.ExplicitCapture |
        RegexOptions.Singleline |
        RegexOptions.CultureInvariant;

    // name-only: '...' is not a relation — it is the '#...' marker of the ResourceFact member production ([[identifiers]])
    [GeneratedRegex(IdentifierGrammar.NamePattern, PatternOptions)]
    public static partial Regex Validation();
}
