using Results;
using System.Text.RegularExpressions;
using Values;

namespace Kingo;

/// <summary>
/// References a subject — the <c>&lt;subject-id&gt;</c> terminal of the fact grammar (see [[domain-language]]). A subject is the unified identity a
/// set of authn-side principals maps to; it need not be human and need not have authenticated. Character rules are provisional; <c>#</c> and <c>@</c> are
/// reserved by the fact grammar.
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
                ? Result.Failure<SubjectId>(Error.Validation("subject_id.invalid", $"subject identifier '{s}' contains invalid characters; expected '^[A-Za-z0-9_][A-Za-z0-9_.:-]*$'"))
                : Result.Success(new SubjectId(s));

    private SubjectId(string value) => Value = value;

    /// <summary>Canonical text form: the underlying string value, unquoted and undecorated.</summary>
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

/// <summary>Character rules for <see cref="SubjectId"/> — the terminal owns its grammar ([[domain-language]]).</summary>
internal static partial class SubjectIdPatterns
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
