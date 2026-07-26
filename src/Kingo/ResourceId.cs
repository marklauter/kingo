using Results;
using System.Text.RegularExpressions;
using Values;

namespace Kingo;

public readonly record struct ResourceId
    : IValue<ResourceId, string>
{
    public string Value { get; }

    public static ResourceId Unchecked(string value) => new(value);

    public static Result<ResourceId> Parse(string s) =>
        string.IsNullOrWhiteSpace(s)
            ? Result.Failure<ResourceId>(Error.Validation(Diagnostics.ErrorCodes.ResourceId.Empty, "resource identifier cannot be empty or whitespace"))
            : !ResourceIdPatterns.Validation().IsMatch(s)
                ? Result.Failure<ResourceId>(Error.Validation(Diagnostics.ErrorCodes.ResourceId.Invalid, $"resource identifier '{s}' contains invalid characters; expected '{IdentifierGrammar.IdPattern}'"))
                : Result.Success(new ResourceId(s));

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
