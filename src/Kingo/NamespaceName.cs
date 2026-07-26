using Results;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Values;

namespace Kingo;

public readonly record struct NamespaceName
    : IValue<NamespaceName, string>
{
    public string Value { get; }

    public static NamespaceName Unchecked(string value) => new(value);

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "lowercase is the canonical form of the identifier; the value is compared and stored, never round-tripped through case conversion")]
    public static Result<NamespaceName> Parse(string s) =>
        string.IsNullOrWhiteSpace(s)
            ? Result.Failure<NamespaceName>(Error.Validation(Diagnostics.ErrorCodes.NamespaceName.Empty, "namespace name cannot be empty or whitespace"))
            : !NamespaceNamePatterns.Validation().IsMatch(s)
                ? Result.Failure<NamespaceName>(Error.Validation(Diagnostics.ErrorCodes.NamespaceName.Invalid, $"namespace name '{s}' is malformed; expected '{IdentifierGrammar.NamePattern}'"))
                : Result.Success(new NamespaceName(s.ToLowerInvariant()));

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
