using Results;

namespace Values;

/// <summary>
/// The domain parse contract: a validating, <see cref="Result{TSelf}"/>-returning parse from the type's canonical text form. Implemented by
/// <see cref="IValue{TSelf, TValue}"/> wrappers (which inherit it) and by composite value records whose canonical form spans multiple parts (each part parsed
/// and the errors accumulated). <c>ToString()</c> on an implementor is the inverse: it emits the canonical text form that <see cref="Parse"/> accepts.
/// </summary>
/// <typeparam name="TSelf">The implementing type. Self-referential (CRTP) so the static abstract member resolves through the type parameter at every call site.
/// No struct constraint — sealed records implement this alongside value-type wrappers.</typeparam>
public interface IParse<TSelf>
    where TSelf : IParse<TSelf>
{
    /// <summary>
    /// Parses <paramref name="s"/> with full validation, returning a <see cref="Result{TSelf}"/> that carries either the parsed value or the structured
    /// <see cref="Error"/>s describing what failed. All parse and validation logic lives here. Whether <see langword="null"/> is valid input is a business rule
    /// of the implementing type: a type for which null is invalid checks and returns a validation failure; a type for which it is valid does not check.
    /// Reflection-based callers can deliver null at runtime regardless of the parameter's non-nullable annotation.
    /// </summary>
    /// <param name="s">The untrusted input string.</param>
    /// <returns>
    /// <see cref="Result{TSelf}.Success"/> wrapping the parsed value when <paramref name="s"/> is well-formed and satisfies every validation rule; otherwise
    /// <see cref="Result{TSelf}.Failure"/> carrying one or more validation <see cref="Error"/>s.
    /// </returns>
    static abstract Result<TSelf> Parse(string s);
}
