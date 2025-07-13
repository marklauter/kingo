using LanguageExt;

namespace Kingo.Storage;

public interface IDocumentWriter<HK>
    where HK : IEquatable<HK>, IComparable<HK>
{
    Either<DocumentWriterError, Unit> Insert(Document<HK> document, CancellationToken cancellationToken);
    Either<DocumentWriterError, Unit> InsertOrUpdate(Document<HK> document, CancellationToken cancellationToken);
    Either<DocumentWriterError, Unit> Update(Document<HK> document, CancellationToken cancellationToken);
}

public interface IDocumentWriter<HK, RK>
    where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>
{
    Either<DocumentWriterError, Unit> Insert(Document<HK, RK> document, CancellationToken cancellationToken);
    Either<DocumentWriterError, Unit> InsertOrUpdate(Document<HK, RK> document, CancellationToken cancellationToken);
    Either<DocumentWriterError, Unit> Update(Document<HK, RK> document, CancellationToken cancellationToken);
}
