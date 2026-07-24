using Results;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Values;

namespace Kingo;

/// <summary>
/// The name of a <see cref="Domains.Domain"/>, the config-side aggregate root's domain key, one segment of the identifier grammar ([[identifiers]]): <c>io</c>.
/// Name-as-identity (settled 2026-07-15, provisionally: no rename, only a new domain. The surrogate-key alternative stays available if admin rename-freedom is
/// worth more than the identity being legible. See [[domain-language]]). The domain is the root of the config tree, so this name is never itself qualified. It
/// is instead what qualifies a <see cref="NamespacePath"/>. Case-insensitive: <see cref="Parse"/> normalizes to lowercase, the canonical form.
/// </summary>
public readonly record struct DomainName
    : IValue<DomainName, string>
{
    /// <inheritdoc/>
    public string Value { get; }

    /// <inheritdoc/>
    public static DomainName Unchecked(string value) => new(value);

    /// <inheritdoc/>
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "lowercase is the canonical form of the identifier; the value is compared and stored, never round-tripped through case conversion")]
    public static Result<DomainName> Parse(string s) =>
        string.IsNullOrWhiteSpace(s)
            ? Result.Failure<DomainName>(Error.Validation("domain_name.empty", "domain name cannot be empty or whitespace"))
            : !DomainNamePatterns.Validation().IsMatch(s)
                ? Result.Failure<DomainName>(Error.Validation("domain_name.invalid", $"domain name '{s}' is malformed; expected '{IdentifierGrammar.NamePattern}'"))
                : Result.Success(new DomainName(s.ToLowerInvariant()));

    private DomainName(string value) => Value = value;

    /// <summary>Returns the canonical text form of the value.</summary>
    /// <returns>The underlying string, unquoted and undecorated.</returns>
    public override string ToString() => Value;

    /// <inheritdoc/>
    public int CompareTo(DomainName other) => string.CompareOrdinal(Value, other.Value);

    /// <inheritdoc/>
    public static bool operator <(DomainName left, DomainName right) => left.CompareTo(right) < 0;

    /// <inheritdoc/>
    public static bool operator <=(DomainName left, DomainName right) => left.CompareTo(right) <= 0;

    /// <inheritdoc/>
    public static bool operator >(DomainName left, DomainName right) => left.CompareTo(right) > 0;

    /// <inheritdoc/>
    public static bool operator >=(DomainName left, DomainName right) => left.CompareTo(right) >= 0;

}

/// <summary>Character rules for <see cref="DomainName"/>: one name, composed from <see cref="IdentifierGrammar"/> ([[identifiers]]).</summary>
internal static partial class DomainNamePatterns
{
    private const RegexOptions PatternOptions =
        RegexOptions.ExplicitCapture |
        RegexOptions.Singleline |
        RegexOptions.CultureInvariant;

    [GeneratedRegex(IdentifierGrammar.NamePattern, PatternOptions)]
    public static partial Regex Validation();
}
