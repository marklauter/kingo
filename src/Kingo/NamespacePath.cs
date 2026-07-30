using Results;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using ValueTypes;

namespace Kingo;

public readonly record struct NamespacePath
    : IValueType<NamespacePath, string>
{
    public string Value { get; }

    public static NamespacePath Unchecked(string value) => new(value);

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "lowercase is the canonical form of the identifier; the value is compared and stored, never round-tripped through case conversion")]
    public static Result<NamespacePath> Checked(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? Result.Failure<NamespacePath>(Error.Validation(Diagnostics.ErrorCodes.NamespacePath.Empty, ErrorMessage.Unchecked("namespace path cannot be empty or whitespace")))
            : !NamespacePathPatterns.Validation().IsMatch(value)
                ? Result.Failure<NamespacePath>(Error.Validation(Diagnostics.ErrorCodes.NamespacePath.Invalid, ErrorMessage.Unchecked($"namespace path '{value}' is malformed; expected '{IdentifierGrammar.NamespacePathPattern}'")))
                : Result.Success(new NamespacePath(value.ToLowerInvariant()));

    public static Result<NamespacePath> Parse(string s) => Checked(s);

    private NamespacePath(string value) => Value = value;

    public override string ToString() => Value;

    public int CompareTo(NamespacePath other) => string.CompareOrdinal(Value, other.Value);

    public static bool operator <(NamespacePath left, NamespacePath right) => left.CompareTo(right) < 0;

    public static bool operator <=(NamespacePath left, NamespacePath right) => left.CompareTo(right) <= 0;

    public static bool operator >(NamespacePath left, NamespacePath right) => left.CompareTo(right) > 0;

    public static bool operator >=(NamespacePath left, NamespacePath right) => left.CompareTo(right) >= 0;

}

internal static partial class NamespacePathPatterns
{
    private const RegexOptions PatternOptions =
        RegexOptions.ExplicitCapture |
        RegexOptions.Singleline |
        RegexOptions.CultureInvariant;

    [GeneratedRegex(IdentifierGrammar.NamespacePathPattern, PatternOptions)]
    public static partial Regex Validation();
}
