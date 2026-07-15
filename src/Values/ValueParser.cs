using Results;
using System.Diagnostics.CodeAnalysis;

namespace Values;

/// <summary>
/// Static helpers shared by <see cref="IParse{TSelf}"/> implementations. Houses the canonical <c>TryParse</c> body so each <see cref="ITryParse{TSelf}"/>
/// implementor's declaration can be a one-line delegation, keeping the parse-to-<c>bool</c>+<c>out</c> projection in one place across every type that opts in.
/// </summary>
public static class ValueParser
{
    /// <summary>
    /// Canonical <c>TryParse</c> body for <see cref="ITryParse{TSelf}"/> implementors: invokes <typeparamref name="TSelf"/>'s <see cref="IParse{TSelf}.Parse"/>
    /// and projects the <see cref="Result{T}"/> into the BCL <c>bool</c>+<c>out</c> shape, discarding any accumulated <see cref="Error"/>s on failure.
    /// </summary>
    /// <typeparam name="TSelf">The type implementing <see cref="IParse{TSelf}"/>.</typeparam>
    /// <param name="s">The untrusted input string.</param>
    /// <param name="parsed">When this method returns <see langword="true"/>, the parsed value; otherwise <see langword="default"/>.</param>
    /// <returns><see langword="true"/> when parsing succeeded; <see langword="false"/> otherwise.</returns>
    public static bool TryParse<TSelf>(
        string s,
        [MaybeNullWhen(false)] out TSelf parsed)
        where TSelf : IParse<TSelf>
    {
        (var ok, parsed) = TSelf.Parse(s).Match(
            value => (true, value),
            _ => (false, default(TSelf)!));
        return ok;
    }
}
