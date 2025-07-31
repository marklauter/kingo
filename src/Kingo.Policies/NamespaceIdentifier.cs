using Kingo.Json;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Kingo.Policies;

[JsonConverter(typeof(StringConvertible<NamespaceIdentifier>))]
[SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "this is for C# wizards only")]
public readonly struct NamespaceIdentifier
    : IStringConvertible<NamespaceIdentifier>
    , IEquatable<NamespaceIdentifier>
    , IComparable<NamespaceIdentifier>
    , IEquatable<string>
    , IComparable<string>
{
    private readonly string value;
    private static readonly Regex Validation = RegExPatterns.NamespaceIdentifier();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NamespaceIdentifier Empty() => throw new ArgumentException($"empty {nameof(value)} not allowed");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NamespaceIdentifier From(string s) => new(s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [JsonConstructor]
    private NamespaceIdentifier(string value) => this.value = ValidValue(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ValidValue(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return !Validation.IsMatch(value)
            ? throw new ArgumentException($"value contains invalid characters. expected: '^[A-Za-z0-9_]+$', actual: {value}'", nameof(value))
            : value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => value.GetHashCode();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString() => value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(NamespaceIdentifier other) => string.Equals(value, other.value, StringComparison.Ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj) => obj is NamespaceIdentifier @namespace && Equals(@namespace);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(NamespaceIdentifier other) => string.CompareOrdinal(value, other.value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(string? other) => other is not null && string.Equals(value, other, StringComparison.Ordinal);

    [SuppressMessage("Globalization", "CA1310:Specify StringComparison for correctness", Justification = "it's fine")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(string? other) => value.CompareTo(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator string(NamespaceIdentifier @namespace) => @namespace.value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator NamespaceIdentifier(string value) => new(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(NamespaceIdentifier left, NamespaceIdentifier right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(NamespaceIdentifier left, NamespaceIdentifier right) => !(left == right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(NamespaceIdentifier left, NamespaceIdentifier right) => left.CompareTo(right) < 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(NamespaceIdentifier left, NamespaceIdentifier right) => left.CompareTo(right) <= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(NamespaceIdentifier left, NamespaceIdentifier right) => left.CompareTo(right) > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(NamespaceIdentifier left, NamespaceIdentifier right) => left.CompareTo(right) >= 0;
}

