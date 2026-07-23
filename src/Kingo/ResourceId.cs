using Results;
using System.Text.RegularExpressions;
using Values;

namespace Kingo;

/// <summary>
/// Identifies a resource within a namespace — the <c>&lt;resource-id&gt;</c> terminal of the fact grammar (see [[domain-language]]). Character rules
/// are provisional; the terminal must never contain the fact-grammar delimiters <c>:</c>, <c>#</c>, or <c>@</c>.
/// </summary>
public readonly record struct ResourceId
    : IValue<ResourceId, string>
{
    /// <inheritdoc/>
    public string Value { get; }

    /// <inheritdoc/>
    public static ResourceId Unchecked(string value) => new(value);

    /// <inheritdoc/>
    public static Result<ResourceId> Parse(string s) =>
        string.IsNullOrWhiteSpace(s)
            ? Result.Failure<ResourceId>(Error.Validation("resource_id.empty", "resource identifier cannot be empty or whitespace"))
            : !ResourceIdPatterns.Validation().IsMatch(s)
                ? Result.Failure<ResourceId>(Error.Validation("resource_id.invalid", $"resource identifier '{s}' contains invalid characters; expected '^[A-Za-z0-9_][A-Za-z0-9_.-]*$'"))
                : Result.Success(new ResourceId(s));

    private ResourceId(string value) => Value = value;

    /// <summary>Canonical text form: the underlying string value, unquoted and undecorated.</summary>
    public override string ToString() => Value;

    /// <inheritdoc/>
    public int CompareTo(ResourceId other) => string.CompareOrdinal(Value, other.Value);

    /// <inheritdoc/>
    public static bool operator <(ResourceId left, ResourceId right) => left.CompareTo(right) < 0;

    /// <inheritdoc/>
    public static bool operator <=(ResourceId left, ResourceId right) => left.CompareTo(right) <= 0;

    /// <inheritdoc/>
    public static bool operator >(ResourceId left, ResourceId right) => left.CompareTo(right) > 0;

    /// <inheritdoc/>
    public static bool operator >=(ResourceId left, ResourceId right) => left.CompareTo(right) >= 0;

}

/// <summary>Character rules for <see cref="ResourceId"/> — the terminal owns its grammar ([[domain-language]]).</summary>
internal static partial class ResourceIdPatterns
{
    private const RegexOptions PatternOptions =
        RegexOptions.ExplicitCapture |
        RegexOptions.Singleline |
        RegexOptions.CultureInvariant;

    // provisional per [[domain-language]]: resource ids need dots (e.g. readme.md);
    // must never contain the fact-grammar delimiters ':' '#' '@'
    [GeneratedRegex(@"^[A-Za-z0-9_][A-Za-z0-9_.-]*$", PatternOptions)]
    public static partial Regex Validation();
}
