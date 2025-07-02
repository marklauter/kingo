using Kingo.Facts;
using Kingo.Json;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Kingo;

[JsonConverter(typeof(StringConvertible<Relationship>))]
public readonly struct Relationship
    : IStringConvertible<Relationship>
    , IEquatable<Relationship>
    , IComparable<Relationship>
    , IEquatable<string>
    , IComparable<string>
{
    private readonly string value;
    private static readonly Regex Validation = RegExPatterns.Relationship();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Relationship Empty() => throw new ArgumentException($"empty {nameof(value)} not allowed");

    public static Relationship Nothing { get; } = From("...");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Relationship From(string s) => new(s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [JsonConstructor]
    private Relationship(string value) => this.value = ValidValue(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ValidValue(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return !Validation.IsMatch(value)
            ? throw new ArgumentException($"value contains invalid characters: '{value}'", nameof(value))
            : value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => value.GetHashCode();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString() => value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Relationship other) => string.Equals(value, other.value, StringComparison.Ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj) => obj is Relationship relationship && Equals(relationship);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(Relationship other) => string.CompareOrdinal(value, other.value);

    public bool Equals(string? other) => other is not null && string.Equals(value, other, StringComparison.Ordinal);

    [SuppressMessage("Globalization", "CA1310:Specify StringComparison for correctness", Justification = "it's fine")]
    public int CompareTo(string? other) => value.CompareTo(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator string(Relationship relationship) => relationship.value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Relationship(string value) => new(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Relationship left, Relationship right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Relationship left, Relationship right) => !(left == right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(Relationship left, Relationship right) => left.CompareTo(right) < 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(Relationship left, Relationship right) => left.CompareTo(right) <= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(Relationship left, Relationship right) => left.CompareTo(right) > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(Relationship left, Relationship right) => left.CompareTo(right) >= 0;
}
