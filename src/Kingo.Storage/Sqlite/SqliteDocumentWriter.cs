using Dapper;
using Kingo.Storage.Clocks;
using Kingo.Storage.Db;
using Kingo.Storage.Json;
using Kingo.Storage.Keys;
using LanguageExt;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace Kingo.Storage.Sqlite;

public static class SqliteDocumentWriter
{
    public static IDocumentWriter<HK> WithIO<HK>(
        IDbContext context,
        Key table)
        where HK : IEquatable<HK>, IComparable<HK> =>
        new SqliteDocumentWriterWithIO<HK>(context, table);

    private sealed class SqliteDocumentWriterWithIO<HK>(
        IDbContext context,
        Key table)
        : IDocumentWriter<HK>
        where HK : IEquatable<HK>, IComparable<HK>
    {
        private readonly SqliteDocumentWriter<HK> writer = new(context, table);

        public Eff<Unit> Insert(Document<HK> document) =>
            Lift(token => writer.InsertAsync(document, token));

        public Eff<Unit> InsertOrUpdate(Document<HK> document) =>
            Lift(token => writer.InsertOrUpdateAsync(document, token));

        public Eff<Unit> Update(Document<HK> document) =>
            Lift(token => writer.UpdateAsync(document, token));

        private static Eff<Unit> Lift(Func<CancellationToken, Task> asyncOperation) =>
            Prelude.liftIO(env => asyncOperation(env.Token));
    }
}

internal sealed class SqliteDocumentWriter<HK>(
    IDbContext context,
    Key table)
    where HK : IEquatable<HK>, IComparable<HK>
{
    private sealed class Journal(Key table)
    {
        private readonly record struct InsertParam(HK HashKey, Revision Version, string Data)
        {
            public InsertParam(Document<HK> document) : this(document.HashKey, document.Version, document.Data.Serialize()) { }
        }

        private readonly string insert = $"insert into {table}_journal (hashkey, version, data) values (@HashKey, @Version, @Data);";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<int> InsertAsync(Document<HK> document, DbConnection db, DbTransaction tx) =>
            db.ExecuteAsync(insert, new InsertParam(document), tx);
    }

    private sealed class Header(Key table)
    {
        private readonly record struct HkVersionParam(HK HashKey, Revision Version)
        {
            public HkVersionParam(Document<HK> document) : this(document.HashKey, document.Version) { }
        }

        private readonly record struct HashKeyParam(HK HashKey);

        private readonly string insert = $"insert into {table}_header (hashkey, version) values (@HashKey, @Version);";
        private readonly string update = $"update {table}_header set version = @Version where hashkey = @HashKey;";
        private readonly string exists = $"select exists(select 1 from {table}_header where hashkey = @HashKey);";
        private readonly string revision = $"select version from {table}_header where hashkey = @HashKey;";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<int> InsertAsync(Document<HK> document, DbConnection db, DbTransaction tx) =>
            db.ExecuteAsync(insert, new HkVersionParam(document), tx);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<int> UpdateAsync(Document<HK> document, DbConnection db, DbTransaction tx) =>
            db.ExecuteAsync(update, new HkVersionParam(document), tx);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<bool> ExistsAsync(Document<HK> document, DbConnection db, DbTransaction tx) =>
            await db.QuerySingleAsync<bool>(exists, new HashKeyParam(document.HashKey), tx);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<Revision?> ReadRevisionAsync(Document<HK> document, DbConnection db, DbTransaction tx) =>
            await db.QuerySingleOrDefaultAsync<Revision?>(revision, new HashKeyParam(document.HashKey), tx);
    }

    private readonly Journal journal = new(table);
    private readonly Header header = new(table);

    public Task InsertAsync(Document<HK> document, CancellationToken token) =>
        context.ExecuteAsync((db, tx) => InsertIfNotExistsAsync(document, db, tx), token);

    private async Task InsertIfNotExistsAsync(Document<HK> document, DbConnection db, DbTransaction tx)
    {
        if (await header.ExistsAsync(document, db, tx))
        {
            throw new DocumentWriterException(
                $"duplicate key {document.HashKey}",
                StorageErrorCodes.DuplicateKeyError);
        }

        await InsertAsync(document, db, tx);
    }

    private async Task InsertAsync(Document<HK> document, DbConnection db, DbTransaction tx)
    {
        document = document with { Version = Revision.Zero };

        if ((await Task.WhenAll(
                journal.InsertAsync(document, db, tx),
                header.InsertAsync(document, db, tx))).Sum() != 2)
            throw new DocumentWriterException(
                $"expected two records modified for document insert with key {document.HashKey}",
                StorageErrorCodes.InsertCountMismatch);
    }

    public Task UpdateAsync(Document<HK> document, CancellationToken token) =>
        context.ExecuteAsync((db, tx) => UpdateIfExistsAsync(document, db, tx), token);

    private async Task UpdateIfExistsAsync(Document<HK> document, DbConnection db, DbTransaction tx)
    {
        var originalVersion = await header.ReadRevisionAsync(document, db, tx);
        if (!originalVersion.HasValue)
            throw new DocumentWriterException(
                $"key not found {document.HashKey}",
                StorageErrorCodes.NotFoundError);

        if (IsVersionMismatch(originalVersion.Value, document.Version))
            throw new DocumentWriterException(
                $"version conflict {document.HashKey}, expected: {document.Version}, actual: {originalVersion}",
                StorageErrorCodes.VersionConflictError);

        await UpdateAsync(document, db, tx);
    }

    private async Task UpdateAsync(Document<HK> document, DbConnection db, DbTransaction tx)
    {
        document = document with { Version = document.Version.Tick() };

        if ((await Task.WhenAll(
                journal.InsertAsync(document, db, tx),
                header.UpdateAsync(document, db, tx))).Sum() != 2)
            throw new DocumentWriterException(
                $"expected two records modified for document update with key {document.HashKey}",
                StorageErrorCodes.InsertCountMismatch);
    }

    public Task InsertOrUpdateAsync(Document<HK> document, CancellationToken token) =>
        context.ExecuteAsync((db, tx) => InsertOrUpdateAsync(document, db, tx), token);

    private async Task InsertOrUpdateAsync(Document<HK> document, DbConnection db, DbTransaction tx)
    {
        var originalVersion = await header.ReadRevisionAsync(document, db, tx);
        if (!originalVersion.HasValue)
        {
            await InsertAsync(document, db, tx);
            return;
        }

        if (IsVersionMismatch(originalVersion.Value, document.Version))
        {
            throw new DocumentWriterException(
                $"version conflict {document.HashKey}, expected: {document.Version}, actual: {originalVersion}",
                StorageErrorCodes.VersionConflictError);
        }

        await UpdateAsync(document, db, tx);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsVersionMismatch(Revision original, Revision replacement) =>
        original != replacement;
}

//public sealed class SqliteDocumentWriter<HK, RK>(
//    SqliteConnection connection,
//    Key table)
//    : IDocumentWriter<HK, RK>
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
//}
