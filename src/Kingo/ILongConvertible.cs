namespace Kingo;

public interface ILongConvertible<T>
{
    static abstract T From(long l);
    static abstract T Empty();
}

