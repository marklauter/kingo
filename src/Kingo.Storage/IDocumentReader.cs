using LanguageExt;

namespace Kingo.Storage;

public interface IDocumentReader<D>
{
    Eff<Option<D>> Find<HK>(HK hashKey)
        where HK : IEquatable<HK>, IComparable<HK>;

    Eff<Option<D>> Find<HK, RK>(HK hashKey, RK rangeKey)
        where HK : IEquatable<HK>, IComparable<HK>
        where RK : IEquatable<RK>, IComparable<RK>;

    Eff<Seq<D>> Query<HK>(Query<D, HK> query)
        where HK : IEquatable<HK>, IComparable<HK>;
}

