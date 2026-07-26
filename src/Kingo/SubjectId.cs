using Results;
using System.Text.RegularExpressions;
using Values;

namespace Kingo;

public readonly record struct SubjectId
    : IValue<SubjectId, string>
{
    public string Value { get; }

    public static SubjectId Unchecked(string value) => new(value);

    public static Result<SubjectId> Parse(string s) =>
        string.IsNullOrWhiteSpace(s)
            ? Result.Failure<SubjectId>(Error.Validation(Diagnostics.ErrorCodes.SubjectId.Empty, "subject identifier cannot be empty or whitespace"))
            : !SubjectIdPatterns.Validation().IsMatch(s)
                ? Result.Failure<SubjectId>(Error.Validation(Diagnostics.ErrorCodes.SubjectId.Invalid, $"subject identifier '{s}' contains invalid characters; expected '{IdentifierGrammar.IdPattern}'"))
                : Result.Success(new SubjectId(s));

    private SubjectId(string value) => Value = value;

    public override string ToString() => Value;

    public int CompareTo(SubjectId other) => string.CompareOrdinal(Value, other.Value);

    public static bool operator <(SubjectId left, SubjectId right) => left.CompareTo(right) < 0;

    public static bool operator <=(SubjectId left, SubjectId right) => left.CompareTo(right) <= 0;

    public static bool operator >(SubjectId left, SubjectId right) => left.CompareTo(right) > 0;

    public static bool operator >=(SubjectId left, SubjectId right) => left.CompareTo(right) >= 0;

}

internal static partial class SubjectIdPatterns
{
    private const RegexOptions PatternOptions =
        RegexOptions.ExplicitCapture |
        RegexOptions.Singleline |
        RegexOptions.CultureInvariant;

    [GeneratedRegex(IdentifierGrammar.IdPattern, PatternOptions)]
    public static partial Regex Validation();
}
