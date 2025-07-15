using Kingo.Storage.Keys;
using LanguageExt;

namespace Kingo.Storage;

public interface IDocumentReader<HK>
    where HK : IEquatable<HK>, IComparable<HK>
{
    Option<Document<HK>> Find(HK hashKey);
}

public interface IDocumentReader<HK, RK>
    where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>
{
    Iterable<Document<HK, RK>> Find(HK hashKey, RangeKey range);
    Option<Document<HK, RK>> Find(HK hashKey, RK rangeKey);
    Iterable<Document<HK, RK>> Where(HK hashKey, Func<Document<HK, RK>, bool> predicate);
}
