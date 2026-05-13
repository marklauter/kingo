namespace Kingo;

/// <summary>
/// Contract for types that can be converted to and from a string representation.
/// </summary>
/// <typeparam name="T">The type that implements the conversion operations.</typeparam>
public interface IStringConvertible<T>
{
    /// <summary>
    /// Instantiates <typeparamref name="T"/> from the string <paramref name="s"/>.
    /// </summary>
    static abstract T From(string s);

    /// <summary>
    /// Instantiates <typeparamref name="T"/> with an empty value, or throws if the type does not allow empty values.
    /// </summary>
    static abstract T Empty();
}
