using Results;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Values;

namespace Kingo.Pdl;

public readonly record struct RelationIdentifier
    : IValue<RelationIdentifier, string>
    , IEquatable<string>
    , IComparable<string>
{
    private readonly string value;
    private static readonly Regex Validation = RegExPatterns.RelationIdentifier();

    /// <summary>Gets the underlying string value.</summary>
    public string Value => value;

    /// <summary>
    /// Constructs a <see cref="RelationIdentifier"/> from a trusted source without validation. Use when the value has already been validated (for example, when reading from a trusted store).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RelationIdentifier Create(string value) => new(value);

    /// <summary>
    /// Parses <paramref name="s"/> with full validation, returning a <see cref="Result{T}"/>.
    /// </summary>
    public static Result<RelationIdentifier> Parse(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return Result.Failure<RelationIdentifier>(Error.Validation("rel.empty", "relation identifier cannot be empty or whitespace"));
        return !Validation.IsMatch(s)
            ? Result.Failure<RelationIdentifier>(Error.Validation("rel.invalid", $"relation identifier '{s}' contains invalid characters; expected '^\\.\\.\\.$|^[A-Za-z_][A-Za-z0-9_]*$'"))
            : Result.Success(new RelationIdentifier(s));
    }

    /// <summary>Attempts to parse <paramref name="s"/>; on success, <paramref name="parsed"/> receives the value.</summary>
    public static bool TryParse(string s, out RelationIdentifier parsed)
    {
        if (Parse(s) is Result<RelationIdentifier>.Success success)
        {
            parsed = success.Value;
            return true;
        }

        parsed = default;
        return false;
    }

    /// <summary>The "..." sentinel — used by the PDL grammar to represent an unspecified relation.</summary>
    public static RelationIdentifier Nothing { get; } = Create("...");

    /// <summary>Throws on invalid input; equivalent to <see cref="Parse"/> in success-or-throw shape. Retained for parser-internal use; new code should prefer <see cref="Parse"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RelationIdentifier From(string s) => new(ValidValue(s));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [JsonConstructor]
    private RelationIdentifier(string value) => this.value = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ValidValue(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return !Validation.IsMatch(value)
            ? throw new ArgumentException($"value contains invalid characters. expected: '^\\.\\.\\.$|^[A-Za-z_][A-Za-z0-9_]*$', actual: '{value}'", nameof(value))
            : value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString() => value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(RelationIdentifier other) => string.CompareOrdinal(value, other.value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(string? other) => other is not null && string.Equals(value, other, StringComparison.Ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(string? other) => string.CompareOrdinal(value, other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(RelationIdentifier left, RelationIdentifier right) => left.CompareTo(right) < 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(RelationIdentifier left, RelationIdentifier right) => left.CompareTo(right) <= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(RelationIdentifier left, RelationIdentifier right) => left.CompareTo(right) > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(RelationIdentifier left, RelationIdentifier right) => left.CompareTo(right) >= 0;
}
