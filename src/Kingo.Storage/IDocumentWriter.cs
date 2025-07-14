using LanguageExt;

namespace Kingo.Storage;

public interface IDocumentWriter<HK>
    where HK : IEquatable<HK>, IComparable<HK>
{
    Eff<Unit> Insert(Document<HK> document, CancellationToken cancellationToken);
    Eff<Unit> InsertOrUpdate(Document<HK> document, CancellationToken cancellationToken);
    Eff<Unit> Update(Document<HK> document, CancellationToken cancellationToken);
}

public interface IDocumentWriter<HK, RK>
    where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>
{
    Eff<Unit> Insert(Document<HK, RK> document, CancellationToken cancellationToken);
    Eff<Unit> InsertOrUpdate(Document<HK, RK> document, CancellationToken cancellationToken);
    Eff<Unit> Update(Document<HK, RK> document, CancellationToken cancellationToken);
}
