using Dapper;
using Kingo.Storage.Db;
using LanguageExt;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace Kingo.Storage.Sqlite;

public static class SqliteDocumentWriter
{
    public static IDocumentWriter<D, HK> WithIO<D, HK>(
        IDbContext context)
        where D : Document<HK>
        where HK : IEquatable<HK>, IComparable<HK> =>
        new SqliteDocumentWriterWithIO<D, HK>(context);

    private sealed class SqliteDocumentWriterWithIO<D, HK>(
        IDbContext context)
        : IDocumentWriter<D, HK>
        where D : Document<HK>
        where HK : IEquatable<HK>, IComparable<HK>
    {
        private readonly SqliteDocumentWriter<D, HK> writer = new(context);

        public Eff<Unit> Insert(D document) =>
            Lift(token => writer.InsertAsync(document, token));

        public Eff<Unit> InsertOrUpdate(D document) =>
            Lift(token => writer.InsertOrUpdateAsync(document, token));

        public Eff<Unit> Update(D document) =>
            Lift(token => writer.UpdateAsync(document, token));
    }

    private static Eff<Unit> Lift(Func<CancellationToken, Task> asyncOperation) =>
        Prelude.liftIO(env => asyncOperation(env.Token));
}

static file class Names<D>
    where D : Document
{
    public static string Columns { get; } = string.Join(',', DocumentTypeCache<D>.PropertyNames.Select(n => n.ToLowerInvariant()));
    public static string Values { get; } = string.Join(',', DocumentTypeCache<D>.PropertyNames.Select(n => $"@{n}"));
}

internal sealed class SqliteDocumentWriter<D, HK>(
    IDbContext context)
    where D : Document<HK>
    where HK : IEquatable<HK>, IComparable<HK>
{
    private static class Journal
    {
        private static readonly string InsertStatement = $"insert into {DocumentTypeCache<D>.TypeName}_journal (hashkey, version, {Names<D>.Columns}) values (@HashKey, @Version, {Names<D>.Values});";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<int> InsertAsync(D document, DbConnection db, DbTransaction tx) =>
            db.ExecuteAsync(InsertStatement, document, tx);
    }

    private static class Header
    {
        private static readonly string InsertStatement = $"insert into {DocumentTypeCache<D>.TypeName}_header (hashkey, version) values (@HashKey, @Version);";
        private static readonly string UpdateStatement = $"update {DocumentTypeCache<D>.TypeName}_header set version = @Version where hashkey = @HashKey;";
        private static readonly string ExistsStatement = $"select exists(select 1 from {DocumentTypeCache<D>.TypeName}_header where hashkey = @HashKey);";
        private static readonly string RevisionStatement = $"select version from {DocumentTypeCache<D>.TypeName}_header where hashkey = @HashKey;";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<int> InsertAsync(D document, DbConnection db, DbTransaction tx) =>
            db.ExecuteAsync(InsertStatement, document, tx);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<int> UpdateAsync(D document, DbConnection db, DbTransaction tx) =>
            db.ExecuteAsync(UpdateStatement, document, tx);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<bool> ExistsAsync(D document, DbConnection db, DbTransaction tx) =>
            db.QuerySingleAsync<bool>(ExistsStatement, document.HashKey, tx);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<Revision?> ReadRevisionAsync(D document, DbConnection db, DbTransaction tx) =>
            db.QuerySingleOrDefaultAsync<Revision?>(RevisionStatement, document, tx);
    }

    public Task InsertAsync(D document, CancellationToken token) =>
        context.ExecuteAsync((db, tx) => InsertIfNotExistsAsync(document, db, tx), token);

    private static async Task InsertIfNotExistsAsync(D document, DbConnection db, DbTransaction tx)
    {
        if (await Header.ExistsAsync(document, db, tx))
        {
            throw new DocumentWriterException(
                $"duplicate key {document.HashKey}",
                StorageErrorCodes.DuplicateKeyError);
        }

        await InsertAsync(document, db, tx);
    }

    private static async Task InsertAsync(D document, DbConnection db, DbTransaction tx)
    {
        document = document with { Version = Revision.Zero };

        if ((await Task.WhenAll(
                Journal.InsertAsync(document, db, tx),
                Header.InsertAsync(document, db, tx))).Sum() != 2)
            throw new DocumentWriterException(
                $"expected two records modified for document insert with key {document.HashKey}",
                StorageErrorCodes.InsertCountMismatch);
    }

    public Task UpdateAsync(D document, CancellationToken token) =>
        context.ExecuteAsync((db, tx) => UpdateIfExistsAsync(document, db, tx), token);

    private static async Task UpdateIfExistsAsync(D document, DbConnection db, DbTransaction tx)
    {
        var originalVersion = await Header.ReadRevisionAsync(document, db, tx);
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

    private static async Task UpdateAsync(D document, DbConnection db, DbTransaction tx)
    {
        document = document with { Version = document.Version.Tick() };

        if ((await Task.WhenAll(
                Journal.InsertAsync(document, db, tx),
                Header.UpdateAsync(document, db, tx))).Sum() != 2)
            throw new DocumentWriterException(
                $"expected two records modified for document update with key {document.HashKey}",
                StorageErrorCodes.InsertCountMismatch);
    }

    public Task InsertOrUpdateAsync(D document, CancellationToken token) =>
        context.ExecuteAsync((db, tx) => InsertOrUpdateAsync(document, db, tx), token);

    private static async Task InsertOrUpdateAsync(D document, DbConnection db, DbTransaction tx)
    {
        var originalVersion = await Header.ReadRevisionAsync(document, db, tx);
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

//internal sealed class SqliteDocumentWriter<HK, RK>(
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
