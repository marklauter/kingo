using Kingo.Storage.Clocks;
using Kingo.Storage.Keys;
using LanguageExt;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Kingo.Storage.Sqlite;

public interface IDocumentWriter<HK>
    : IDisposable
    where HK : IEquatable<HK>, IComparable<HK>
{
    Eff<Unit> Insert(Document<HK> document);
    Eff<Unit> InsertOrUpdate(Document<HK> document);
    Eff<Unit> Update(Document<HK> document);
}

public interface IDocumentWriter<HK, RK>
    where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>
{
    Task Insert(Document<HK, RK> document, CancellationToken token);
    Task InsertOrUpdate(Document<HK, RK> document, CancellationToken token);
    Task Update(Document<HK, RK> document, CancellationToken token);
}

public static class SqliteDocumentWriter
{
    public static IDocumentWriter<HK> WithIO<HK>(
        SqliteConnection connection,
        Key table)
        where HK : IEquatable<HK>, IComparable<HK> =>
        new SqliteDocumentWriterWithIO<HK>(connection, table);

    private sealed class SqliteDocumentWriterWithIO<HK>(SqliteConnection connection, Key table) : IDocumentWriter<HK>, IDisposable
        where HK : IEquatable<HK>, IComparable<HK>
    {
        private readonly SqliteDocumentWriter<HK> writer = new(connection, table);

        public Eff<Unit> Insert(Document<HK> document) =>
            Lift(token => writer.InsertAsync(document, token));

        public Eff<Unit> InsertOrUpdate(Document<HK> document) =>
            Lift(token => writer.InsertOrUpdateAsync(document, token));

        public Eff<Unit> Update(Document<HK> document) =>
            Lift(token => writer.UpdateAsync(document, token));

        private static Eff<Unit> Lift(Func<CancellationToken, Task> asyncOperation) =>
            Prelude.liftIO(env => asyncOperation(env.Token));

        public void Dispose() =>
            writer.Dispose();
    }
}

public sealed class SqliteDocumentWriter<HK>(
    SqliteConnection connection,
    Key table)
    : IDisposable
    where HK : IEquatable<HK>, IComparable<HK>
{
    private readonly TransactionManager txman = new(connection);
    private readonly Journal<HK> journal = new(connection, table);
    private readonly Header<HK> header = new(connection, table);

    public Task InsertAsync(Document<HK> document, CancellationToken token) =>
        txman.ExecuteAsync(transaction => InsertIfNotExistsAsync(document, transaction), token);

    private async Task InsertIfNotExistsAsync(Document<HK> document, IDbTransaction transaction)
    {
        if (await header.ExistsAsync(document, transaction))
        {
            throw new DocumentWriterException(
                $"duplicate key {document.HashKey}",
                StorageErrorCodes.DuplicateKeyError);
        }

        await InsertAsync(document, transaction);
    }

    private async Task InsertAsync(Document<HK> document, IDbTransaction transaction)
    {
        document = document with { Version = Revision.Zero };

        if ((await Task.WhenAll(
                journal.InsertAsync(document, transaction),
                header.InsertAsync(document, transaction))).Sum() != 2)
            throw new DocumentWriterException(
                $"expected two records modified for document insert with key {document.HashKey}",
                StorageErrorCodes.InsertCountMismatch);
    }

    public Task UpdateAsync(Document<HK> document, CancellationToken token) =>
        txman.ExecuteAsync(transaction => UpdateIfExistsAsync(document, transaction), token);

    private async Task UpdateIfExistsAsync(Document<HK> document, IDbTransaction transaction)
    {
        var originalVersion = await header.ReadRevisionAsync(document, transaction);

        if (!originalVersion.HasValue)
            throw new DocumentWriterException(
                $"key not found {document.HashKey}",
                StorageErrorCodes.NotFoundError);

        if (IsVersionMismatch(originalVersion.Value, document.Version))
            throw new DocumentWriterException(
                $"version conflict {document.HashKey}, expected: {document.Version}, actual: {originalVersion}",
                StorageErrorCodes.VersionConflictError);

        await UpdateAsync(document, transaction);
    }

    private async Task UpdateAsync(Document<HK> document, IDbTransaction transaction)
    {
        document = document with { Version = document.Version.Tick() };

        if ((await Task.WhenAll(
                journal.InsertAsync(document, transaction),
                header.UpdateAsync(document, transaction))).Sum() != 2)
            throw new DocumentWriterException(
                $"expected two records modified for document update with key {document.HashKey}",
                StorageErrorCodes.InsertCountMismatch);
    }

    public Task InsertOrUpdateAsync(Document<HK> document, CancellationToken token) =>
        txman.ExecuteAsync(transaction => InsertOrUpdateAsync(document, transaction), token);

    private async Task InsertOrUpdateAsync(Document<HK> document, IDbTransaction transaction)
    {
        var originalVersion = await header.ReadRevisionAsync(document, transaction);
        if (!originalVersion.HasValue)
        {
            await InsertAsync(document, transaction);
            return;
        }

        if (IsVersionMismatch(originalVersion.Value, document.Version))
        {
            throw new DocumentWriterException(
                $"version conflict {document.HashKey}, expected: {document.Version}, actual: {originalVersion}",
                StorageErrorCodes.VersionConflictError);
        }

        await UpdateAsync(document, transaction);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsVersionMismatch(Revision original, Revision replacement) =>
        original != replacement;

    [SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP007:Don't dispose injected", Justification = "reader owns the connection")]
    public void Dispose() => connection.Dispose();
}

//public sealed class SqliteDocumentWriter<HK, RK>(
//    SqliteConnection connection,
//    Key table)
//    : IDisposable
//    , IDocumentWriter<HK, RK>
//    where HK : IEquatable<HK>, IComparable<HK>
//    where RK : IEquatable<RK>, IComparable<RK>
//{
//    private readonly string findQuery = $"SELECT Id, HashKey, RangeKey, Version FROM {table}_header WHERE HashKey = @HashKey AND RangeKey = @RangeKey";
//    private readonly string insertJournalQuery = $"INSERT INTO {table}_journal (HashKey, RangeKey, Version, Data) VALUES (@HashKey, @RangeKey, @Version, @Data); SELECT last_insert_rowid();";
//    private readonly string insertHeaderQuery = $"INSERT INTO {table}_header (Id, HashKey, RangeKey, Version) VALUES (@Id, @HashKey, @RangeKey, @Version);";
//    private readonly string updateJournalQuery = $"INSERT INTO {table}_journal (Id, HashKey, RangeKey, Version, Data) VALUES (@Id, @HashKey, @RangeKey, @Version, @Data);";
//    private readonly string updateHeaderQuery = $"UPDATE {table}_header SET Version = @Version WHERE Id = @Id;";

//    private record DocumentHeader(long Id, HK HashKey, RK RangeKey, Revision Version);

//    public Either<DocumentWriterError, Unit> Insert(Document<HK, RK> document, CancellationToken cancellationToken) =>
//        WithTransaction(transaction =>
//            Find(document.HashKey, document.RangeKey, transaction).Match(
//                Some: _ => DocumentWriterError.New(StorageErrorCodes.DuplicateKeyError, $"duplicate key {document.HashKey}/{document.RangeKey}"),
//                None: () => InsertInternal(document, transaction)));

//    public Either<DocumentWriterError, Unit> InsertOrUpdate(Document<HK, RK> document, CancellationToken cancellationToken) =>
//        WithTransaction(transaction =>
//            Find(document.HashKey, document.RangeKey, transaction).Match(
//                Some: header =>
//                {
//                    var newDocument = document with { Version = header.Version.Tick() };
//                    return CheckVersion(header.Version, document.Version, document.HashKey, document.RangeKey)
//                        .Bind(_ => UpdateInternal(header.Id, newDocument, transaction));
//                },
//                None: () => InsertInternal(document, transaction)));

//    public Either<DocumentWriterError, Unit> Update(Document<HK, RK> document, CancellationToken cancellationToken) =>
//        WithTransaction(transaction =>
//            Find(document.HashKey, document.RangeKey, transaction).Match(
//                Some: header =>
//                {
//                    var newDocument = document with { Version = header.Version.Tick() };
//                    return CheckVersion(header.Version, document.Version, document.HashKey, document.RangeKey)
//                        .Bind(_ => UpdateInternal(header.Id, newDocument, transaction));
//                },
//                None: () => DocumentWriterError.New(StorageErrorCodes.NotFoundError, $"key not found {document.HashKey}/{document.RangeKey}")));

//    private Either<DocumentWriterError, Unit> WithTransaction(Func<SqliteTransaction, Either<DocumentWriterError, Unit>> operation)
//    {
//        using var transaction = connection.BeginTransaction();
//        try
//        {
//            var result = operation(transaction);
//            if (result.IsRight)
//            {
//                transaction.Commit();
//            }
//            else
//            {
//                transaction.Rollback();
//            }

//            return result;
//        }
//        catch (Exception e)
//        {
//            transaction.Rollback();
//            return DocumentWriterError.New(StorageErrorCodes.UnknownError, "Transaction failed", Error.New(e));
//        }
//    }

//    private Option<DocumentHeader> Find(HK hashKey, RK rangeKey, SqliteTransaction transaction) =>
//        connection.QuerySingleOrDefault<DocumentHeader>(findQuery, new { HashKey = hashKey, RangeKey = rangeKey }, transaction);

//    private Either<DocumentWriterError, Unit> InsertInternal(Document<HK, RK> document, SqliteTransaction transaction)
//    {
//        var doc = document with { Version = Revision.Zero };
//        var data = JsonSerializer.Serialize(doc.Data);

//        var id = connection.ExecuteScalar<long>(insertJournalQuery, new { doc.HashKey, doc.RangeKey, doc.Version, Data = data }, transaction);
//        _ = connection.Execute(insertHeaderQuery, new { Id = id, doc.HashKey, doc.RangeKey, doc.Version }, transaction);

//        return Prelude.unit;
//    }

//    private Either<DocumentWriterError, Unit> UpdateInternal(long id, Document<HK, RK> document, SqliteTransaction transaction)
//    {
//        var data = JsonSerializer.Serialize(document.Data);

//        _ = connection.Execute(updateJournalQuery, new { Id = id, document.HashKey, document.RangeKey, document.Version, Data = data }, transaction);
//        _ = connection.Execute(updateHeaderQuery, new { document.Version, Id = id }, transaction);

//        return Prelude.unit;
//    }

//    private static Either<DocumentWriterError, Unit> CheckVersion(Revision original, Revision replacement, HK hashKey, RK rangeKey) =>
//        original == replacement
//            ? Prelude.unit
//            : DocumentWriterError.New(StorageErrorCodes.VersionConflictError, $"version conflict {hashKey}/{rangeKey}, expected: {replacement}, actual: {original}");

//    [SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP007:Don't dispose injected", Justification = "reader owns the connection")]
//    public void Dispose() => connection.Dispose();
//}
