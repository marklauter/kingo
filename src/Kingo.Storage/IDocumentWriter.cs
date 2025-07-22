using LanguageExt;

namespace Kingo.Storage;

public interface IDocumentWriter<D, HK>
    where D : Document<HK>
    where HK : IEquatable<HK>, IComparable<HK>
{
    Eff<Unit> Insert(D document);
    Eff<Unit> InsertOrUpdate(D document);
    Eff<Unit> Update(D document);
}

public interface IDocumentWriter<D, HK, RK>
    where D : Document<HK, RK>
    where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>
{
    Eff<Unit> Insert(D document);
    Eff<Unit> InsertOrUpdate(D document);
    Eff<Unit> Update(D document);
}
