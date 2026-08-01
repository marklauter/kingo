using Results;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using ValueTypes;

namespace Kingo;

public readonly record struct NamespaceName
    : IValueType<NamespaceName, string>
{
    public string Value { get; }

    public static NamespaceName Unchecked(string value) => new(value);

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "lowercase is the canonical form of the identifier; the value is compared and stored, never round-tripped through case conversion")]
    public static Result<NamespaceName> Checked(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? Result.Failure<NamespaceName>(Error.Validation(Diagnostics.ErrorCodes.NamespaceName.Empty, ErrorMessage.Unchecked("namespace name cannot be empty or whitespace")))
            : !NamespaceNamePatterns.Validation().IsMatch(value)
                ? Result.Failure<NamespaceName>(Error.Validation(Diagnostics.ErrorCodes.NamespaceName.Invalid, ErrorMessage.Unchecked($"namespace name '{value}' is malformed; expected '{IdentifierGrammar.NamePattern}'")))
                : Result.Success(new NamespaceName(value.ToLowerInvariant()));

    public static Result<NamespaceName> Parse(string s) => Checked(s);

    private NamespaceName(string value) => Value = value;

    public override string ToString() => Value;

    public int CompareTo(NamespaceName other) => string.CompareOrdinal(Value, other.Value);

    public static bool operator <(NamespaceName left, NamespaceName right) => left.CompareTo(right) < 0;

    public static bool operator <=(NamespaceName left, NamespaceName right) => left.CompareTo(right) <= 0;

    public static bool operator >(NamespaceName left, NamespaceName right) => left.CompareTo(right) > 0;

    public static bool operator >=(NamespaceName left, NamespaceName right) => left.CompareTo(right) >= 0;

}

internal static partial class NamespaceNamePatterns
{
    private const RegexOptions PatternOptions =
        RegexOptions.ExplicitCapture |
        RegexOptions.Singleline |
        RegexOptions.CultureInvariant;

    [GeneratedRegex(IdentifierGrammar.NamePattern, PatternOptions)]
    public static partial Regex Validation();
}
