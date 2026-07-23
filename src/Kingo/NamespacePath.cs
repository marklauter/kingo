using Results;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Values;

namespace Kingo;

/// <summary>
/// Identifies a <see cref="Namespace"/> — the <c>&lt;namespace&gt;</c> terminal of the fact grammar (see [[domain-language]]). Case-insensitive:
/// <see cref="Parse"/> normalizes to lowercase, the canonical form.
/// </summary>
public readonly record struct NamespacePath
    : IValue<NamespacePath, string>
{
    /// <inheritdoc/>
    public string Value { get; }

    /// <inheritdoc/>
    public static NamespacePath Unchecked(string value) => new(value);

    /// <inheritdoc/>
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "lowercase is the canonical form of the identifier; the value is compared and stored, never round-tripped through case conversion")]
    public static Result<NamespacePath> Parse(string s) =>
        string.IsNullOrWhiteSpace(s)
            ? Result.Failure<NamespacePath>(Error.Validation("namespace_path.empty", "namespace path cannot be empty or whitespace"))
            : !NamespacePathPatterns.Validation().IsMatch(s)
                ? Result.Failure<NamespacePath>(Error.Validation("namespace_path.invalid", $"namespace path '{s}' contains invalid characters; expected '^[A-Za-z_][A-Za-z0-9_]*$'"))
                : Result.Success(new NamespacePath(s.ToLowerInvariant()));

    private NamespacePath(string value) => Value = value;

    /// <summary>Canonical text form: the underlying string value, unquoted and undecorated.</summary>
    public override string ToString() => Value;

    /// <inheritdoc/>
    public int CompareTo(NamespacePath other) => string.CompareOrdinal(Value, other.Value);

    /// <inheritdoc/>
    public static bool operator <(NamespacePath left, NamespacePath right) => left.CompareTo(right) < 0;

    /// <inheritdoc/>
    public static bool operator <=(NamespacePath left, NamespacePath right) => left.CompareTo(right) <= 0;

    /// <inheritdoc/>
    public static bool operator >(NamespacePath left, NamespacePath right) => left.CompareTo(right) > 0;

    /// <inheritdoc/>
    public static bool operator >=(NamespacePath left, NamespacePath right) => left.CompareTo(right) >= 0;

}

/// <summary>Character rules for <see cref="NamespacePath"/> — the terminal owns its grammar ([[domain-language]]).</summary>
internal static partial class NamespacePathPatterns
{
    private const RegexOptions PatternOptions =
        RegexOptions.ExplicitCapture |
        RegexOptions.Singleline |
        RegexOptions.CultureInvariant;

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*$", PatternOptions)]
    public static partial Regex Validation();
}
