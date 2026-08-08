using Results;
using System.Text.RegularExpressions;
using ValueTypes;

namespace Kingo;

public readonly record struct Identity
    : IValueType<Identity, string>
{
    public string Value { get; }

    public static Identity Unchecked(string value) => new(value);

    public static Result<Identity> Checked(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? Result.Failure<Identity>(Error.Validation(Diagnostics.ErrorCodes.Identity.Empty, ErrorMessage.Unchecked("identity cannot be empty or whitespace")))
            : !IdentityPatterns.Validation().IsMatch(value)
                ? Result.Failure<Identity>(Error.Validation(Diagnostics.ErrorCodes.Identity.Invalid, ErrorMessage.Unchecked($"identity '{value}' contains invalid characters; expected '{IdentifierGrammar.IdPattern}'")))
                : Result.Success(new Identity(value));

    public static Result<Identity> Parse(string s) => Checked(s);

    private Identity(string value) => Value = value;

    public override string ToString() => Value;

    public int CompareTo(Identity other) => string.CompareOrdinal(Value, other.Value);

    public static bool operator <(Identity left, Identity right) => left.CompareTo(right) < 0;

    public static bool operator <=(Identity left, Identity right) => left.CompareTo(right) <= 0;

    public static bool operator >(Identity left, Identity right) => left.CompareTo(right) > 0;

    public static bool operator >=(Identity left, Identity right) => left.CompareTo(right) >= 0;

}

internal static partial class IdentityPatterns
{
    private const RegexOptions PatternOptions =
        RegexOptions.ExplicitCapture |
        RegexOptions.Singleline |
        RegexOptions.CultureInvariant;

    [GeneratedRegex(IdentifierGrammar.IdPattern, PatternOptions)]
    public static partial Regex Validation();
}
