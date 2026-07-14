using Results;

namespace Values;

/// <summary>
/// Static helpers shared by <see cref="IValue{TSelf, TValue}"/> implementations. Houses the canonical <c>TryParse</c> body so each implementor's <c>TryParse</c> declaration can be a one-line delegation, keeping the parse-to-<c>bool</c>+<c>out</c> projection in one place across every wrapper.
/// </summary>
public static class ValueParser
{
    /// <summary>
    /// Canonical <c>TryParse</c> body for <see cref="IValue{TSelf, TValue}"/> implementors: invokes <typeparamref name="TSelf"/>'s <see cref="IValue{TSelf, TValue}.Parse"/> and projects the <see cref="Result{T}"/> into the BCL <c>bool</c>+<c>out</c> shape, discarding any accumulated <see cref="Error"/>s on failure.
    /// </summary>
    /// <typeparam name="TSelf">The wrapper type implementing <see cref="IValue{TSelf, TValue}"/>.</typeparam>
    /// <typeparam name="TValue">The underlying primitive type the wrapper carries.</typeparam>
    /// <param name="s">The untrusted input string.</param>
    /// <param name="parsed">When this method returns <see langword="true"/>, the parsed value; otherwise <see langword="default"/>.</param>
    /// <returns><see langword="true"/> when parsing succeeded; <see langword="false"/> otherwise.</returns>
    public static bool TryParse<TSelf, TValue>(
        string s,
        out TSelf parsed)
        where TSelf : struct, IValue<TSelf, TValue>
    {
        if (TSelf.Parse(s) is Result<TSelf>.Success success)
        {
            parsed = success.Value;
            return true;
        }

        parsed = default;
        return false;
    }
}
