using Dapper;
using Kingo.Storage.Db;
using LanguageExt;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace Kingo.Storage.Sqlite;

internal sealed class SqliteDocumentWriter<D, HK>(
    IDbContext context)
    where D : Document<HK>
    where HK : IEquatable<HK>, IComparable<HK>
{
    private static class Journal
    {
        private static readonly string InsertStatement = $"insert into {DocumentTypeCache<D>.TypeName}_journal ({FieldNames<D>.Columns}) values ({FieldNames<D>.Values})";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<int> InsertAsync(D document, DbConnection db, DbTransaction tx) =>
            db.ExecuteAsync(InsertStatement, document, tx);
    }

    private static class Header
    {
        private static readonly string InsertStatement = $"insert into {DocumentTypeCache<D>.TypeName}_header (hashkey, version) values (@HashKey, @Version)";
        private static readonly string UpdateStatement = $"update {DocumentTypeCache<D>.TypeName}_header set version = @NewVersion where hashkey = @HashKey and version = @CurrentVersion";
        private static readonly string ExistsStatement = $"select exists(select 1 from {DocumentTypeCache<D>.TypeName}_header where hashkey = @HashKey)";
        private static readonly string RevisionStatement = $"select version from {DocumentTypeCache<D>.TypeName}_header where hashkey = @HashKey";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<int> InsertAsync(D document, DbConnection db, DbTransaction tx) =>
            db.ExecuteAsync(InsertStatement, document, tx);

        private readonly record struct UpdateParam(HK HashKey, Revision CurrentVersion, Revision NewVersion);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<int> UpdateAsync(HK hashKey, Revision currentVersion, Revision newVersion, DbConnection db, DbTransaction tx) =>
            db.ExecuteAsync(UpdateStatement, new UpdateParam(hashKey, currentVersion, newVersion), tx);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<bool> ExistsAsync(D document, DbConnection db, DbTransaction tx) =>
            db.QuerySingleAsync<bool>(ExistsStatement, document, tx);

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
                $"version conflict {document.HashKey}, expected version: {document.Version}, actual version: {originalVersion}",
                StorageErrorCodes.VersionConflictError);

        await UpdateAsync(document, db, tx);
    }

    private static async Task UpdateAsync(D document, DbConnection db, DbTransaction tx)
    {
        var currentVersion = document.Version;
        var newVersion = document.Version.Tick();
        if (await Journal.InsertAsync(document with { Version = newVersion }, db, tx) != 1)
            throw new DocumentWriterException(
                $"journal insert failed for with key {document.HashKey}, version {newVersion}",
                StorageErrorCodes.InsertCountMismatch);

        if (await Header.UpdateAsync(document.HashKey, currentVersion, newVersion, db, tx) != 1)
            throw new DocumentWriterException(
                $"version conflict {document.HashKey}, expected: {document.Version}, actual: unknown",
                StorageErrorCodes.VersionConflictError);
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
