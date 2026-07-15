using Results;
using System.Text.RegularExpressions;
using Values;

namespace Kingo;

/// <summary>
/// Identifies a resource within a namespace — the <c>&lt;resource-id&gt;</c> terminal of the tuple grammar (see docs/notes/domain-language.md). Character rules
/// are provisional; the terminal must never contain the tuple delimiters <c>:</c>, <c>#</c>, or <c>@</c>.
/// </summary>
public readonly record struct ResourceIdentifier
    : IValue<ResourceIdentifier, string>
{
    /// <inheritdoc/>
    public string Value { get; }

    /// <inheritdoc/>
    public static ResourceIdentifier Create(string value) => new(value);

    /// <inheritdoc/>
    public static Result<ResourceIdentifier> Parse(string s) =>
        string.IsNullOrWhiteSpace(s)
            ? Result.Failure<ResourceIdentifier>(Error.Validation("resource_id.empty", "resource identifier cannot be empty or whitespace"))
            : !ResourceIdentifierPatterns.Validation().IsMatch(s)
                ? Result.Failure<ResourceIdentifier>(Error.Validation("resource_id.invalid", $"resource identifier '{s}' contains invalid characters; expected '^[A-Za-z0-9_][A-Za-z0-9_.-]*$'"))
                : Result.Success(new ResourceIdentifier(s));

    private ResourceIdentifier(string value) => Value = value;

    /// <summary>Canonical text form: the underlying string value, unquoted and undecorated.</summary>
    public override string ToString() => Value;

    /// <inheritdoc/>
    public int CompareTo(ResourceIdentifier other) => string.CompareOrdinal(Value, other.Value);

    /// <inheritdoc/>
    public static bool operator <(ResourceIdentifier left, ResourceIdentifier right) => left.CompareTo(right) < 0;

    /// <inheritdoc/>
    public static bool operator <=(ResourceIdentifier left, ResourceIdentifier right) => left.CompareTo(right) <= 0;

    /// <inheritdoc/>
    public static bool operator >(ResourceIdentifier left, ResourceIdentifier right) => left.CompareTo(right) > 0;

    /// <inheritdoc/>
    public static bool operator >=(ResourceIdentifier left, ResourceIdentifier right) => left.CompareTo(right) >= 0;

}

/// <summary>Character rules for <see cref="ResourceIdentifier"/> — the terminal owns its grammar (docs/notes/domain-language.md).</summary>
internal static partial class ResourceIdentifierPatterns
{
    private const RegexOptions PatternOptions =
        RegexOptions.ExplicitCapture |
        RegexOptions.Singleline |
        RegexOptions.CultureInvariant;

    // provisional per docs/notes/domain-language.md: resource ids need dots (e.g. readme.md);
    // must never contain the tuple delimiters ':' '#' '@'
    [GeneratedRegex(@"^[A-Za-z0-9_][A-Za-z0-9_.-]*$", PatternOptions)]
    public static partial Regex Validation();
}
