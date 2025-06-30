using Kingo.Json;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Kingo.Facts;

[JsonConverter(typeof(StringConvertible<Namespace>))]
[SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "this is for C# wizards only")]
public readonly struct Namespace
    : IStringConvertible<Namespace>
    , IEquatable<Namespace>
    , IComparable<Namespace>
    , IEquatable<string>
    , IComparable<string>
{
    private readonly string value;
    private static readonly Regex Validation = RegExPatterns.Identifier();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Namespace Empty() => throw new ArgumentException($"empty {nameof(value)} not allowed");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Namespace From(string s) => new(s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [JsonConstructor]
    private Namespace(string value) => this.value = ValidValue(value);

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
    public bool Equals(Namespace other) => string.Equals(value, other.value, StringComparison.Ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj) => obj is Namespace @namespace && Equals(@namespace);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(Namespace other) => string.CompareOrdinal(value, other.value);

    public bool Equals(string? other) => other is not null && string.Equals(value, other, StringComparison.Ordinal);

    [SuppressMessage("Globalization", "CA1310:Specify StringComparison for correctness", Justification = "it's fine")]
    public int CompareTo(string? other) => value.CompareTo(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator string(Namespace @namespace) => @namespace.value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Namespace(string value) => new(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Namespace left, Namespace right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Namespace left, Namespace right) => !(left == right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(Namespace left, Namespace right) => left.CompareTo(right) < 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(Namespace left, Namespace right) => left.CompareTo(right) <= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(Namespace left, Namespace right) => left.CompareTo(right) > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(Namespace left, Namespace right) => left.CompareTo(right) >= 0;
}

