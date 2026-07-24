using Results;
using System.Text.RegularExpressions;
using Values;

namespace Kingo;

/// <summary>
/// A reference to a subject, the <c>&lt;subject-id&gt;</c> terminal of the fact grammar (see [[domain-language]]). A subject is the unified identity a
/// set of authn-side principals maps to. It need not be human and need not have authenticated. The caller owns this value: Kingo compares it and never
/// interprets it ([[split-identities-at-ownership-boundaries]]). The rule is shared with <see cref="ResourceId"/> as <see cref="IdentifierGrammar.IdPattern"/>
/// and admits the real shapes callers bring: GUIDs, integers, URNs, URIs, emails, and UPNs. It requires only a non-empty run of visible characters with no
/// whitespace and no control characters.
/// </summary>
public readonly record struct SubjectId
    : IValue<SubjectId, string>
{
    /// <inheritdoc/>
    public string Value { get; }

    /// <inheritdoc/>
    public static SubjectId Unchecked(string value) => new(value);

    /// <inheritdoc/>
    public static Result<SubjectId> Parse(string s) =>
        string.IsNullOrWhiteSpace(s)
            ? Result.Failure<SubjectId>(Error.Validation("subject_id.empty", "subject identifier cannot be empty or whitespace"))
            : !SubjectIdPatterns.Validation().IsMatch(s)
                ? Result.Failure<SubjectId>(Error.Validation("subject_id.invalid", $"subject identifier '{s}' contains invalid characters; expected '{IdentifierGrammar.IdPattern}'"))
                : Result.Success(new SubjectId(s));

    private SubjectId(string value) => Value = value;

    /// <summary>Returns the canonical text form of the value.</summary>
    /// <returns>The underlying string, unquoted and undecorated.</returns>
    public override string ToString() => Value;

    /// <inheritdoc/>
    public int CompareTo(SubjectId other) => string.CompareOrdinal(Value, other.Value);

    /// <inheritdoc/>
    public static bool operator <(SubjectId left, SubjectId right) => left.CompareTo(right) < 0;

    /// <inheritdoc/>
    public static bool operator <=(SubjectId left, SubjectId right) => left.CompareTo(right) <= 0;

    /// <inheritdoc/>
    public static bool operator >(SubjectId left, SubjectId right) => left.CompareTo(right) > 0;

    /// <inheritdoc/>
    public static bool operator >=(SubjectId left, SubjectId right) => left.CompareTo(right) >= 0;

}

/// <summary>Character rules for <see cref="SubjectId"/>: the caller's grammar, held in <see cref="IdentifierGrammar"/> ([[domain-language]]).</summary>
internal static partial class SubjectIdPatterns
{
    private const RegexOptions PatternOptions =
        RegexOptions.ExplicitCapture |
        RegexOptions.Singleline |
        RegexOptions.CultureInvariant;

    // provisional per [[domain-language]]: the same rule as a resource id;
    // must never contain the fact-grammar delimiters '/' ':' '#' '@'
    [GeneratedRegex(IdentifierGrammar.IdPattern, PatternOptions)]
    public static partial Regex Validation();
}
