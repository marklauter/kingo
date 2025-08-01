namespace Kingo;

/// <summary>
/// Defines a contract for types that can be converted to and from a string representation.
/// </summary>
/// <remarks>
/// Implementing types must provide static methods to create an instance from a string and to represent
/// an empty instance. This is used by the StringConvertable : JsonConverter to instantiate any type that implements IStringConvertible.
/// </remarks>
/// <typeparam name="T">The type that implements the conversion operations.</typeparam>
public interface IStringConvertible<T>
{
    /// <summary>
    /// Instantiates T from the string s.
    /// </summary>
    /// <param name="s"></param>
    /// <returns>T</returns>
    static abstract T From(string s);

    /// <summary>
    /// Instantiates T with an empty string. 
    /// Implemnters may handle this gracefully, or throw ArgumentException at their discresion. 
    /// </summary>
    /// <returns>T</returns>
    static abstract T Empty();
}
