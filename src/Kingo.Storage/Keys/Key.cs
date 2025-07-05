using Kingo.Json;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Kingo.Storage.Keys;

[JsonConverter(typeof(StringConvertible<Key>))]
public readonly struct Key
    : IStringConvertible<Key>
    , IEquatable<Key>
    , IComparable<Key>
    , IEquatable<string>
    , IComparable<string>
{
    private readonly string value;
    private static readonly Regex Validation = RegExPatterns.Key();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Key Empty() => throw new ArgumentException($"empty {nameof(value)} not allowed");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Key From(string s) => new(s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [JsonConstructor]
    private Key(string value) => this.value = ValidValue(value).ToLowerInvariant();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ValidValue(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return !Validation.IsMatch(value)
            ? throw new ArgumentException($"value contains invalid characters. expected: '^[A-Za-z0-9_.:/@#!&*|-]+$', actual: '{value}'", nameof(value))
            : value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => value.GetHashCode();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString() => value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Key other) => string.Equals(value, other.value, StringComparison.Ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj) => obj is Key key && Equals(key);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(Key other) => string.CompareOrdinal(value, other.value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(string? other) => other is not null && string.Equals(value, other, StringComparison.Ordinal);

    [SuppressMessage("Globalization", "CA1310:Specify StringComparison for correctness", Justification = "it's fine")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(string? other) => value.CompareTo(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator string(Key key) => key.value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Key(string value) => new(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Key left, Key right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Key left, Key right) => !(left == right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(Key left, Key right) => left.CompareTo(right) < 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(Key left, Key right) => left.CompareTo(right) <= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(Key left, Key right) => left.CompareTo(right) > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(Key left, Key right) => left.CompareTo(right) >= 0;
}
