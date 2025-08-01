using Kingo.Json;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Kingo.Policies;

[JsonConverter(typeof(StringConvertible<RelationIdentifier>))]
public readonly struct RelationIdentifier
    : IStringConvertible<RelationIdentifier>
    , IEquatable<RelationIdentifier>
    , IComparable<RelationIdentifier>
    , IEquatable<string>
    , IComparable<string>
{
    private readonly string value;
    private static readonly Regex Validation = RegExPatterns.RelationIdentifier();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RelationIdentifier Empty() => throw new ArgumentException($"empty {nameof(value)} not allowed");

    public static RelationIdentifier Nothing { get; } = From("...");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RelationIdentifier From(string s) => new(s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [JsonConstructor]
    private RelationIdentifier(string value) => this.value = ValidValue(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ValidValue(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return !Validation.IsMatch(value)
            ? throw new ArgumentException($"value contains invalid characters. expected: '^[A-Za-z_][A-Za-z0-9_.]*$', actual: '{value}'", nameof(value))
            : value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => value.GetHashCode();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString() => value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(RelationIdentifier other) => string.Equals(value, other.value, StringComparison.Ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj) => obj is RelationIdentifier relationship && Equals(relationship);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(RelationIdentifier other) => string.CompareOrdinal(value, other.value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(string? other) => other is not null && string.Equals(value, other, StringComparison.Ordinal);

    [SuppressMessage("Globalization", "CA1310:Specify StringComparison for correctness", Justification = "it's fine")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(string? other) => value.CompareTo(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator string(RelationIdentifier relationship) => relationship.value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator RelationIdentifier(string value) => new(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(RelationIdentifier left, RelationIdentifier right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(RelationIdentifier left, RelationIdentifier right) => !(left == right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(RelationIdentifier left, RelationIdentifier right) => left.CompareTo(right) < 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(RelationIdentifier left, RelationIdentifier right) => left.CompareTo(right) <= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(RelationIdentifier left, RelationIdentifier right) => left.CompareTo(right) > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(RelationIdentifier left, RelationIdentifier right) => left.CompareTo(right) >= 0;
}
