using Kingo.Storage.Keys;
using LanguageExt;

namespace Kingo.Storage;

public interface IDocumentReader<D, HK>
    where D : Document<HK>
    where HK : IEquatable<HK>, IComparable<HK>
{
    Eff<Option<D>> Find(HK hashKey);
}

public interface IDocumentReader<D, HK, RK>
    where D : Document<HK, RK>
    where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>
{
    Eff<Iterable<D>> Find(HK hashKey, RangeKeyCondition range);
    Eff<Option<D>> Find(HK hashKey, RK rangeKey);
    Eff<Iterable<D>> Where(HK hashKey, Func<D, bool> predicate);
}
