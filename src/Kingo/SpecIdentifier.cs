using Results;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Values;

namespace Kingo;

/// <summary>
/// Identifies a <see cref="Schemas.Spec"/> — the config-side aggregate's domain key. Name-as-identity (settled 2026-07-15,
/// provisionally: no rename, only a new spec; the surrogate-key alternative stays available if admin rename-freedom is worth
/// more than the identity being legible — see [[domain-language]]). Shares <see cref="NamespaceIdentifier"/>'s grammar
/// and case-insensitivity because it is the same kind of thing: authored vocabulary, not a client-minted reference like
/// <see cref="ResourceIdentifier"/>. <see cref="Parse"/> normalizes to lowercase, the canonical form.
/// </summary>
public readonly record struct SpecIdentifier
    : IValue<SpecIdentifier, string>
{
    /// <inheritdoc/>
    public string Value { get; }

    /// <inheritdoc/>
    public static SpecIdentifier Unchecked(string value) => new(value);

    /// <inheritdoc/>
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "lowercase is the canonical form of the identifier; the value is compared and stored, never round-tripped through case conversion")]
    public static Result<SpecIdentifier> Parse(string s) =>
        string.IsNullOrWhiteSpace(s)
            ? Result.Failure<SpecIdentifier>(Error.Validation("spec_id.empty", "spec identifier cannot be empty or whitespace"))
            : !SpecIdentifierPatterns.Validation().IsMatch(s)
                ? Result.Failure<SpecIdentifier>(Error.Validation("spec_id.invalid", $"spec identifier '{s}' contains invalid characters; expected '^[A-Za-z_][A-Za-z0-9_]*$'"))
                : Result.Success(new SpecIdentifier(s.ToLowerInvariant()));

    private SpecIdentifier(string value) => Value = value;

    /// <summary>Canonical text form: the underlying string value, unquoted and undecorated.</summary>
    public override string ToString() => Value;

    /// <inheritdoc/>
    public int CompareTo(SpecIdentifier other) => string.CompareOrdinal(Value, other.Value);

    /// <inheritdoc/>
    public static bool operator <(SpecIdentifier left, SpecIdentifier right) => left.CompareTo(right) < 0;

    /// <inheritdoc/>
    public static bool operator <=(SpecIdentifier left, SpecIdentifier right) => left.CompareTo(right) <= 0;

    /// <inheritdoc/>
    public static bool operator >(SpecIdentifier left, SpecIdentifier right) => left.CompareTo(right) > 0;

    /// <inheritdoc/>
    public static bool operator >=(SpecIdentifier left, SpecIdentifier right) => left.CompareTo(right) >= 0;

}

/// <summary>Character rules for <see cref="SpecIdentifier"/> — the terminal owns its grammar ([[domain-language]]).</summary>
internal static partial class SpecIdentifierPatterns
{
    private const RegexOptions PatternOptions =
        RegexOptions.ExplicitCapture |
        RegexOptions.Singleline |
        RegexOptions.CultureInvariant;

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*$", PatternOptions)]
    public static partial Regex Validation();
}
