using Kingo.Storage.Keys;
using LanguageExt;
using Microsoft.Data.Sqlite;
using System.Diagnostics.CodeAnalysis;

namespace Kingo.Storage.Sqlite;

public sealed class SqliteDocumentWriter<HK>(
    SqliteConnection connection,
    Key table)
    : IDisposable
    , IDocumentWriter<HK>
    where HK : IEquatable<HK>, IComparable<HK>
{
    public Either<DocumentWriterError, Unit> Insert(Document<HK> document, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Either<DocumentWriterError, Unit> InsertOrUpdate(Document<HK> document, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Either<DocumentWriterError, Unit> Update(Document<HK> document, CancellationToken cancellationToken) => throw new NotImplementedException();

    [SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP007:Don't dispose injected", Justification = "reader owns the connection")]
    public void Dispose() => connection.Dispose();
}
