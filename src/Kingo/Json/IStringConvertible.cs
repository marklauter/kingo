namespace Kingo.Json;

public interface IStringConvertible<T>
{
    static abstract T From(string s);
    static abstract T Empty();
}
