using Results;
using System.Text.RegularExpressions;
using Values;

namespace Kingo;

/// <summary>
/// An identifier for a resource within a namespace, the <c>&lt;resource-id&gt;</c> terminal of the fact grammar (see [[domain-language]]). The caller owns this
/// value: Kingo compares it and never interprets it ([[split-identities-at-ownership-boundaries]]). The rule is shared with <see cref="SubjectId"/> as
/// <see cref="IdentifierGrammar.IdPattern"/> and admits the real shapes callers bring: GUIDs, integers, URNs, and URIs. It requires only a non-empty run of
/// visible characters with no whitespace and no control characters.
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
                ? Result.Failure<ResourceId>(Error.Validation("resource_id.invalid", $"resource identifier '{s}' contains invalid characters; expected '{IdentifierGrammar.IdPattern}'"))
                : Result.Success(new ResourceId(s));

    private ResourceId(string value) => Value = value;

    /// <summary>Returns the canonical text form of the value.</summary>
    /// <returns>The underlying string, unquoted and undecorated.</returns>
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

/// <summary>Character rules for <see cref="ResourceId"/>: the caller's grammar, held in <see cref="IdentifierGrammar"/> ([[domain-language]]).</summary>
internal static partial class ResourceIdPatterns
{
    private const RegexOptions PatternOptions =
        RegexOptions.ExplicitCapture |
        RegexOptions.Singleline |
        RegexOptions.CultureInvariant;

    // provisional per [[domain-language]]: resource ids need dots (e.g. readme.md);
    // must never contain the fact-grammar delimiters '/' ':' '#' '@'
    [GeneratedRegex(IdentifierGrammar.IdPattern, PatternOptions)]
    public static partial Regex Validation();
}
