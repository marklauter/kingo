using LanguageExt;

namespace Kingo.Storage;

public interface IDocumentWriter<HK>
    : IDisposable
    where HK : IEquatable<HK>, IComparable<HK>
{
    Eff<Unit> Insert(Document<HK> document);
    Eff<Unit> InsertOrUpdate(Document<HK> document);
    Eff<Unit> Update(Document<HK> document);
}

public interface IDocumentWriter<HK, RK>
    : IDisposable
    where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>
{
    Eff<Unit> Insert(Document<HK, RK> document);
    Eff<Unit> InsertOrUpdate(Document<HK, RK> document);
    Eff<Unit> Update(Document<HK, RK> document);
}
