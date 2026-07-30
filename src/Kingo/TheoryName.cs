using Results;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using ValueTypes;

namespace Kingo;

public readonly record struct TheoryName
    : IValueType<TheoryName, string>
{
    public string Value { get; }

    public static TheoryName Unchecked(string value) => new(value);

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "lowercase is the canonical form of the identifier; the value is compared and stored, never round-tripped through case conversion")]
    public static Result<TheoryName> Checked(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? Result.Failure<TheoryName>(Error.Validation(Diagnostics.ErrorCodes.TheoryName.Empty, ErrorMessage.Unchecked("theory name cannot be empty or whitespace")))
            : !TheoryNamePatterns.Validation().IsMatch(value)
                ? Result.Failure<TheoryName>(Error.Validation(Diagnostics.ErrorCodes.TheoryName.Invalid, ErrorMessage.Unchecked($"theory name '{value}' is malformed; expected '{IdentifierGrammar.NamePattern}'")))
                : Result.Success(new TheoryName(value.ToLowerInvariant()));

    public static Result<TheoryName> Parse(string s) => Checked(s);

    private TheoryName(string value) => Value = value;

    public override string ToString() => Value;

    public int CompareTo(TheoryName other) => string.CompareOrdinal(Value, other.Value);

    public static bool operator <(TheoryName left, TheoryName right) => left.CompareTo(right) < 0;

    public static bool operator <=(TheoryName left, TheoryName right) => left.CompareTo(right) <= 0;

    public static bool operator >(TheoryName left, TheoryName right) => left.CompareTo(right) > 0;

    public static bool operator >=(TheoryName left, TheoryName right) => left.CompareTo(right) >= 0;

}

internal static partial class TheoryNamePatterns
{
    private const RegexOptions PatternOptions =
        RegexOptions.ExplicitCapture |
        RegexOptions.Singleline |
        RegexOptions.CultureInvariant;

    [GeneratedRegex(IdentifierGrammar.NamePattern, PatternOptions)]
    public static partial Regex Validation();
}
