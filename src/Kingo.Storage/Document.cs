namespace Kingo.Storage;

public interface IDocument
{
    Revision Version { get; }
}

public interface IDocument<HK>
    : IDocument
    where HK : IEquatable<HK>, IComparable<HK>
{
    HK? HashKey { get; }
}

public interface IDocument<HK, RK>
    : IDocument<HK>
    where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>
{
    RK RangeKey { get; }
}
