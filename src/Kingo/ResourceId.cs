using Results;
using System.Text.RegularExpressions;
using ValueTypes;

namespace Kingo;

public readonly record struct ResourceId
    : IValueType<ResourceId, string>
{
    public string Value { get; }

    public static ResourceId Unchecked(string value) => new(value);

    public static Result<ResourceId> Checked(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? Result.Failure<ResourceId>(Error.Validation(Diagnostics.ErrorCodes.ResourceId.Empty, ErrorMessage.Unchecked("resource identifier cannot be empty or whitespace")))
            : !ResourceIdPatterns.Validation().IsMatch(value)
                ? Result.Failure<ResourceId>(Error.Validation(Diagnostics.ErrorCodes.ResourceId.Invalid, ErrorMessage.Unchecked($"resource identifier '{value}' contains invalid characters; expected '{IdentifierGrammar.IdPattern}'")))
                : Result.Success(new ResourceId(value));

    public static Result<ResourceId> Parse(string s) => Checked(s);

    private ResourceId(string value) => Value = value;

    public override string ToString() => Value;

    public int CompareTo(ResourceId other) => string.CompareOrdinal(Value, other.Value);

    public static bool operator <(ResourceId left, ResourceId right) => left.CompareTo(right) < 0;

    public static bool operator <=(ResourceId left, ResourceId right) => left.CompareTo(right) <= 0;

    public static bool operator >(ResourceId left, ResourceId right) => left.CompareTo(right) > 0;

    public static bool operator >=(ResourceId left, ResourceId right) => left.CompareTo(right) >= 0;

}

internal static partial class ResourceIdPatterns
{
    private const RegexOptions PatternOptions =
        RegexOptions.ExplicitCapture |
        RegexOptions.Singleline |
        RegexOptions.CultureInvariant;

    [GeneratedRegex(IdentifierGrammar.IdPattern, PatternOptions)]
    public static partial Regex Validation();
}
