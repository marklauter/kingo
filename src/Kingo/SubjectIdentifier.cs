using Results;
using System.Text.RegularExpressions;
using Values;

namespace Kingo;

/// <summary>
/// References a subject — the <c>&lt;subject-id&gt;</c> terminal of the fact grammar (see [[domain-language]]). A subject is the unified identity a
/// set of authn-side principals maps to; it need not be human and need not have authenticated. Character rules are provisional; <c>#</c> and <c>@</c> are
/// reserved by the fact grammar.
/// </summary>
public readonly record struct SubjectIdentifier
    : IValue<SubjectIdentifier, string>
{
    /// <inheritdoc/>
    public string Value { get; }

    /// <inheritdoc/>
    public static SubjectIdentifier Create(string value) => new(value);

    /// <inheritdoc/>
    public static Result<SubjectIdentifier> Parse(string s) =>
        string.IsNullOrWhiteSpace(s)
            ? Result.Failure<SubjectIdentifier>(Error.Validation("subject_id.empty", "subject identifier cannot be empty or whitespace"))
            : !SubjectIdentifierPatterns.Validation().IsMatch(s)
                ? Result.Failure<SubjectIdentifier>(Error.Validation("subject_id.invalid", $"subject identifier '{s}' contains invalid characters; expected '^[A-Za-z0-9_][A-Za-z0-9_.:-]*$'"))
                : Result.Success(new SubjectIdentifier(s));

    private SubjectIdentifier(string value) => Value = value;

    /// <summary>Canonical text form: the underlying string value, unquoted and undecorated.</summary>
    public override string ToString() => Value;

    /// <inheritdoc/>
    public int CompareTo(SubjectIdentifier other) => string.CompareOrdinal(Value, other.Value);

    /// <inheritdoc/>
    public static bool operator <(SubjectIdentifier left, SubjectIdentifier right) => left.CompareTo(right) < 0;

    /// <inheritdoc/>
    public static bool operator <=(SubjectIdentifier left, SubjectIdentifier right) => left.CompareTo(right) <= 0;

    /// <inheritdoc/>
    public static bool operator >(SubjectIdentifier left, SubjectIdentifier right) => left.CompareTo(right) > 0;

    /// <inheritdoc/>
    public static bool operator >=(SubjectIdentifier left, SubjectIdentifier right) => left.CompareTo(right) >= 0;

}

/// <summary>Character rules for <see cref="SubjectIdentifier"/> — the terminal owns its grammar ([[domain-language]]).</summary>
internal static partial class SubjectIdentifierPatterns
{
    private const RegexOptions PatternOptions =
        RegexOptions.ExplicitCapture |
        RegexOptions.Singleline |
        RegexOptions.CultureInvariant;

    // provisional per [[domain-language]]: an external identity reference (e.g. user:anne);
    // ':' is allowed, '#' and '@' are reserved by the fact grammar
    [GeneratedRegex(@"^[A-Za-z0-9_][A-Za-z0-9_.:-]*$", PatternOptions)]
    public static partial Regex Validation();
}
