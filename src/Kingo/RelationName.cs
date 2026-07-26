using Results;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Values;

namespace Kingo;

public readonly record struct RelationName
    : IValue<RelationName, string>
{
    public string Value { get; }

    public static RelationName Unchecked(string value) => new(value);

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "lowercase is the canonical form of the identifier; the value is compared and stored, never round-tripped through case conversion")]
    public static Result<RelationName> Parse(string s) =>
        string.IsNullOrWhiteSpace(s)
            ? Result.Failure<RelationName>(Error.Validation(Diagnostics.ErrorCodes.RelationName.Empty, "relation name cannot be empty or whitespace"))
            : !RelationNamePatterns.Validation().IsMatch(s)
                ? Result.Failure<RelationName>(Error.Validation(Diagnostics.ErrorCodes.RelationName.Invalid, $"relation name '{s}' is malformed; expected '{IdentifierGrammar.NamePattern}'"))
                : Result.Success(new RelationName(s.ToLowerInvariant()));

    private RelationName(string value) => Value = value;

    public override string ToString() => Value;

    public int CompareTo(RelationName other) => string.CompareOrdinal(Value, other.Value);

    public static bool operator <(RelationName left, RelationName right) => left.CompareTo(right) < 0;

    public static bool operator <=(RelationName left, RelationName right) => left.CompareTo(right) <= 0;

    public static bool operator >(RelationName left, RelationName right) => left.CompareTo(right) > 0;

    public static bool operator >=(RelationName left, RelationName right) => left.CompareTo(right) >= 0;

}

internal static partial class RelationNamePatterns
{
    private const RegexOptions PatternOptions =
        RegexOptions.ExplicitCapture |
        RegexOptions.Singleline |
        RegexOptions.CultureInvariant;

    [GeneratedRegex(IdentifierGrammar.NamePattern, PatternOptions)]
    public static partial Regex Validation();
}
