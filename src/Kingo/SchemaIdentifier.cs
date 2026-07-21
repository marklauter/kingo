using Results;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Values;

namespace Kingo;

/// <summary>
/// Identifies a <see cref="Schemas.Schema"/> — the config-side aggregate's domain key. Name-as-identity (settled 2026-07-15,
/// provisionally: no rename, only a new schema; the surrogate-key alternative stays available if admin rename-freedom is worth
/// more than the identity being legible — see [[domain-language]]). Shares <see cref="NamespaceIdentifier"/>'s grammar
/// and case-insensitivity because it is the same kind of thing: authored vocabulary, not a client-minted reference like
/// <see cref="ResourceIdentifier"/>. <see cref="Parse"/> normalizes to lowercase, the canonical form.
/// </summary>
public readonly record struct SchemaIdentifier
    : IValue<SchemaIdentifier, string>
{
    /// <inheritdoc/>
    public string Value { get; }

    /// <inheritdoc/>
    public static SchemaIdentifier Unchecked(string value) => new(value);

    /// <inheritdoc/>
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "lowercase is the canonical form of the identifier; the value is compared and stored, never round-tripped through case conversion")]
    public static Result<SchemaIdentifier> Parse(string s) =>
        string.IsNullOrWhiteSpace(s)
            ? Result.Failure<SchemaIdentifier>(Error.Validation("schema_id.empty", "schema identifier cannot be empty or whitespace"))
            : !SchemaIdentifierPatterns.Validation().IsMatch(s)
                ? Result.Failure<SchemaIdentifier>(Error.Validation("schema_id.invalid", $"schema identifier '{s}' contains invalid characters; expected '^[A-Za-z_][A-Za-z0-9_]*$'"))
                : Result.Success(new SchemaIdentifier(s.ToLowerInvariant()));

    private SchemaIdentifier(string value) => Value = value;

    /// <summary>Canonical text form: the underlying string value, unquoted and undecorated.</summary>
    public override string ToString() => Value;

    /// <inheritdoc/>
    public int CompareTo(SchemaIdentifier other) => string.CompareOrdinal(Value, other.Value);

    /// <inheritdoc/>
    public static bool operator <(SchemaIdentifier left, SchemaIdentifier right) => left.CompareTo(right) < 0;

    /// <inheritdoc/>
    public static bool operator <=(SchemaIdentifier left, SchemaIdentifier right) => left.CompareTo(right) <= 0;

    /// <inheritdoc/>
    public static bool operator >(SchemaIdentifier left, SchemaIdentifier right) => left.CompareTo(right) > 0;

    /// <inheritdoc/>
    public static bool operator >=(SchemaIdentifier left, SchemaIdentifier right) => left.CompareTo(right) >= 0;

}

/// <summary>Character rules for <see cref="SchemaIdentifier"/> — the terminal owns its grammar ([[domain-language]]).</summary>
internal static partial class SchemaIdentifierPatterns
{
    private const RegexOptions PatternOptions =
        RegexOptions.ExplicitCapture |
        RegexOptions.Singleline |
        RegexOptions.CultureInvariant;

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*$", PatternOptions)]
    public static partial Regex Validation();
}
