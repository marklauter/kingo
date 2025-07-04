namespace Kingo.Json;

public interface IULongConvertible<T>
{
    static abstract T From(ulong l);
    static abstract T Empty();
}
