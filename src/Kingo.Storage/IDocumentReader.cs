using LanguageExt;

namespace Kingo.Storage;

public interface IDocumentReader<D>
{
    Eff<Option<D>> Find<HK>(HK hashKey, Option<object> rangeKey)
        where HK : IEquatable<HK>, IComparable<HK>;

    Eff<Seq<D>> Query<HK>(Query<D, HK> query)
        where HK : IEquatable<HK>, IComparable<HK>;
}

