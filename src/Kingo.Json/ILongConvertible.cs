namespace Kingo.Json;

public interface ILongConvertible<T>
{
    static abstract T From(long l);
    static abstract T Empty();
}

