using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Kingo.Pdl;

[SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "the domain word is 'namespace'")]
public readonly record struct NamespaceIdentifier
    : IValue<NamespaceIdentifier, string>
    , IEquatable<string>
    , IComparable<string>
{
    private readonly string value;
    private static readonly Regex Validation = RegExPatterns.NamespaceIdentifier();

    /// <summary>Gets the underlying string value.</summary>
    public string Value => value;

    /// <summary>
    /// Constructs a <see cref="NamespaceIdentifier"/> from a trusted source without validation. Use when the value has already been validated (for example, when reading from a trusted store).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NamespaceIdentifier Create(string value) => new(value);

    /// <summary>
    /// Parses <paramref name="s"/> with full validation, returning a <see cref="Result{T}"/>.
    /// </summary>
    public static Result<NamespaceIdentifier> Parse(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return Error.Validation("ns.empty", "namespace identifier cannot be empty or whitespace");
        if (!Validation.IsMatch(s))
            return Error.Validation("ns.invalid", $"namespace identifier '{s}' contains invalid characters; expected '^[A-Za-z_][A-Za-z0-9_]*$'");
        return new NamespaceIdentifier(s);
    }

    /// <summary>Attempts to parse <paramref name="s"/>; on success, <paramref name="parsed"/> receives the value.</summary>
    public static bool TryParse(string s, out NamespaceIdentifier parsed)
    {
        if (Parse(s) is Success<NamespaceIdentifier> success)
        {
            parsed = success.Value;
            return true;
        }
        parsed = default;
        return false;
    }

    /// <summary>Throws on invalid input; equivalent to <see cref="Parse"/> in success-or-throw shape. Retained for parser-internal use; new code should prefer <see cref="Parse"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NamespaceIdentifier From(string s) => new(ValidValue(s));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [JsonConstructor]
    private NamespaceIdentifier(string value) => this.value = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ValidValue(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return !Validation.IsMatch(value)
            ? throw new ArgumentException($"value contains invalid characters. expected: '^[A-Za-z_][A-Za-z0-9_]*$', actual: '{value}'", nameof(value))
            : value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString() => value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(NamespaceIdentifier other) => string.CompareOrdinal(value, other.value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(string? other) => other is not null && string.Equals(value, other, StringComparison.Ordinal);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(string? other) => string.CompareOrdinal(value, other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(NamespaceIdentifier left, NamespaceIdentifier right) => left.CompareTo(right) < 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(NamespaceIdentifier left, NamespaceIdentifier right) => left.CompareTo(right) <= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(NamespaceIdentifier left, NamespaceIdentifier right) => left.CompareTo(right) > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(NamespaceIdentifier left, NamespaceIdentifier right) => left.CompareTo(right) >= 0;
}
