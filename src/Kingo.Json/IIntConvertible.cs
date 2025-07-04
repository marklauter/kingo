namespace Kingo.Json;

public interface IIntConvertible<T>
{
    static abstract T From(int l);
    static abstract T Empty();
}
