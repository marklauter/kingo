using Results;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Values;

namespace Kingo;

/// <summary>
/// The name of a <c>Kingo.Theories.Theory</c>, the theory-side aggregate root's key, one segment of the identifier grammar: <c>io</c>.
/// Name-as-identity (settled 2026-07-15, provisionally: no rename, only a new theory. The surrogate-key alternative stays available if admin rename-freedom is
/// worth more than the identity being legible). The theory is the root of the theory tree, so this name is never itself qualified. It
/// is instead what qualifies a <see cref="NamespacePath"/>. Case-insensitive: <see cref="Parse"/> normalizes to lowercase, the canonical form.
/// </summary>
public readonly record struct TheoryName
    : IValue<TheoryName, string>
{
    /// <inheritdoc/>
    public string Value { get; }

    /// <inheritdoc/>
    public static TheoryName Unchecked(string value) => new(value);

    /// <inheritdoc/>
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "lowercase is the canonical form of the identifier; the value is compared and stored, never round-tripped through case conversion")]
    public static Result<TheoryName> Parse(string s) =>
        string.IsNullOrWhiteSpace(s)
            ? Result.Failure<TheoryName>(Error.Validation(Diagnostics.ErrorCodes.TheoryName.Empty, "theory name cannot be empty or whitespace"))
            : !TheoryNamePatterns.Validation().IsMatch(s)
                ? Result.Failure<TheoryName>(Error.Validation(Diagnostics.ErrorCodes.TheoryName.Invalid, $"theory name '{s}' is malformed; expected '{IdentifierGrammar.NamePattern}'"))
                : Result.Success(new TheoryName(s.ToLowerInvariant()));

    private TheoryName(string value) => Value = value;

    /// <summary>Returns the canonical text form of the value.</summary>
    /// <returns>The underlying string, unquoted and undecorated.</returns>
    public override string ToString() => Value;

    /// <inheritdoc/>
    public int CompareTo(TheoryName other) => string.CompareOrdinal(Value, other.Value);

    /// <inheritdoc/>
    public static bool operator <(TheoryName left, TheoryName right) => left.CompareTo(right) < 0;

    /// <inheritdoc/>
    public static bool operator <=(TheoryName left, TheoryName right) => left.CompareTo(right) <= 0;

    /// <inheritdoc/>
    public static bool operator >(TheoryName left, TheoryName right) => left.CompareTo(right) > 0;

    /// <inheritdoc/>
    public static bool operator >=(TheoryName left, TheoryName right) => left.CompareTo(right) >= 0;

}

/// <summary>Character rules for <see cref="TheoryName"/>: one name, composed from <see cref="IdentifierGrammar"/>.</summary>
internal static partial class TheoryNamePatterns
{
    private const RegexOptions PatternOptions =
        RegexOptions.ExplicitCapture |
        RegexOptions.Singleline |
        RegexOptions.CultureInvariant;

    [GeneratedRegex(IdentifierGrammar.NamePattern, PatternOptions)]
    public static partial Regex Validation();
}
