using Kingo.Facts;
using Kingo.Json;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Kingo;

[JsonConverter(typeof(StringConvertible<Identifier>))]
public readonly struct Identifier
    : IStringConvertible<Identifier>
    , IEquatable<Identifier>
    , IComparable<Identifier>
    , IEquatable<string>
    , IComparable<string>
{
    private readonly string value;
    private static readonly Regex Validation = RegExPatterns.Identifier();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Identifier Empty() => throw new ArgumentException($"empty {nameof(value)} not allowed");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Identifier From(string s) => new(s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [JsonConstructor]
    private Identifier(string value) => this.value = ValidValue(value);

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
    public bool Equals(Identifier other) => string.Equals(value, other.value, StringComparison.Ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj) => obj is Identifier identifier && Equals(identifier);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(Identifier other) => string.CompareOrdinal(value, other.value);

    public bool Equals(string? other) => other is not null && string.Equals(value, other, StringComparison.Ordinal);

    [SuppressMessage("Globalization", "CA1310:Specify StringComparison for correctness", Justification = "it's fine")]
    public int CompareTo(string? other) => value.CompareTo(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator string(Identifier identifier) => identifier.value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Identifier(string value) => new(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Identifier left, Identifier right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Identifier left, Identifier right) => !(left == right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(Identifier left, Identifier right) => left.CompareTo(right) < 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(Identifier left, Identifier right) => left.CompareTo(right) <= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(Identifier left, Identifier right) => left.CompareTo(right) > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(Identifier left, Identifier right) => left.CompareTo(right) >= 0;
}
