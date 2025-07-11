using Kingo.Facts;
using Kingo.Json;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Kingo;

[JsonConverter(typeof(StringConvertible<RelationName>))]
public readonly struct RelationName
    : IStringConvertible<RelationName>
    , IEquatable<RelationName>
    , IComparable<RelationName>
    , IEquatable<string>
    , IComparable<string>
{
    private readonly string value;
    private static readonly Regex Validation = RegExPatterns.Relationship();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RelationName Empty() => throw new ArgumentException($"empty {nameof(value)} not allowed");

    public static RelationName Nothing { get; } = From("...");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RelationName From(string s) => new(s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [JsonConstructor]
    private RelationName(string value) => this.value = ValidValue(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ValidValue(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return !Validation.IsMatch(value)
            ? throw new ArgumentException($"value contains invalid characters. expected: '^[A-Za-z0-9_.]+$', actual: {value}'", nameof(value))
            : value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => value.GetHashCode();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString() => value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(RelationName other) => string.Equals(value, other.value, StringComparison.Ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj) => obj is RelationName relationship && Equals(relationship);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(RelationName other) => string.CompareOrdinal(value, other.value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(string? other) => other is not null && string.Equals(value, other, StringComparison.Ordinal);

    [SuppressMessage("Globalization", "CA1310:Specify StringComparison for correctness", Justification = "it's fine")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(string? other) => value.CompareTo(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator string(RelationName relationship) => relationship.value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator RelationName(string value) => new(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(RelationName left, RelationName right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(RelationName left, RelationName right) => !(left == right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(RelationName left, RelationName right) => left.CompareTo(right) < 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(RelationName left, RelationName right) => left.CompareTo(right) <= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(RelationName left, RelationName right) => left.CompareTo(right) > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(RelationName left, RelationName right) => left.CompareTo(right) >= 0;
}
