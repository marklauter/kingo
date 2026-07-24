using Results;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Values;

namespace Kingo;

/// <summary>
/// Names a <see cref="Schemas.Spec"/> — the config-side aggregate root's domain key, one segment of the identifier grammar ([[identifiers]]): <c>io</c>.
/// Name-as-identity (settled 2026-07-15, provisionally: no rename, only a new spec; the surrogate-key alternative stays available if admin rename-freedom is
/// worth more than the identity being legible — see [[domain-language]]). The spec is the root of the config tree, so this name is never itself qualified; it
/// is instead what qualifies a <see cref="NamespacePath"/>. Case-insensitive: <see cref="Parse"/> normalizes to lowercase, the canonical form.
/// </summary>
public readonly record struct SpecName
    : IValue<SpecName, string>
{
    /// <inheritdoc/>
    public string Value { get; }

    /// <inheritdoc/>
    public static SpecName Unchecked(string value) => new(value);

    /// <inheritdoc/>
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "lowercase is the canonical form of the identifier; the value is compared and stored, never round-tripped through case conversion")]
    public static Result<SpecName> Parse(string s) =>
        string.IsNullOrWhiteSpace(s)
            ? Result.Failure<SpecName>(Error.Validation("spec_name.empty", "spec name cannot be empty or whitespace"))
            : !SpecNamePatterns.Validation().IsMatch(s)
                ? Result.Failure<SpecName>(Error.Validation("spec_name.invalid", $"spec name '{s}' is malformed; expected '{IdentifierGrammar.NamePattern}'"))
                : Result.Success(new SpecName(s.ToLowerInvariant()));

    private SpecName(string value) => Value = value;

    /// <summary>Canonical text form: the underlying string value, unquoted and undecorated.</summary>
    public override string ToString() => Value;

    /// <inheritdoc/>
    public int CompareTo(SpecName other) => string.CompareOrdinal(Value, other.Value);

    /// <inheritdoc/>
    public static bool operator <(SpecName left, SpecName right) => left.CompareTo(right) < 0;

    /// <inheritdoc/>
    public static bool operator <=(SpecName left, SpecName right) => left.CompareTo(right) <= 0;

    /// <inheritdoc/>
    public static bool operator >(SpecName left, SpecName right) => left.CompareTo(right) > 0;

    /// <inheritdoc/>
    public static bool operator >=(SpecName left, SpecName right) => left.CompareTo(right) >= 0;

}

/// <summary>Character rules for <see cref="SpecName"/> — one name, composed from <see cref="IdentifierGrammar"/> ([[identifiers]]).</summary>
internal static partial class SpecNamePatterns
{
    private const RegexOptions PatternOptions =
        RegexOptions.ExplicitCapture |
        RegexOptions.Singleline |
        RegexOptions.CultureInvariant;

    [GeneratedRegex(IdentifierGrammar.NamePattern, PatternOptions)]
    public static partial Regex Validation();
}
