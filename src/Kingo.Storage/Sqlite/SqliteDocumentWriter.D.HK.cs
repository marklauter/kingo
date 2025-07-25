using Dapper;
using Kingo.Storage.Context;
using Kingo.Storage.Db;
using LanguageExt;
using System.Data.Common;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Kingo.Storage.Sqlite;

internal sealed class SqliteDocumentWriter<D>(IDbContext context)
{
    private static class Journal
    {
        private static readonly string InsertStatement;

        static Journal() => InsertStatement = new StringBuilder()
                .AppendLine(CultureInfo.InvariantCulture, $"insert into {DocumentTypeCache<D>.Name}_journal")
                .AppendLine(CultureInfo.InvariantCulture, $"({FieldNames<D>.Columns})")
                .AppendLine(CultureInfo.InvariantCulture, $"values ({FieldNames<D>.Values})")
                .ToString();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<int> InsertAsync(D document, DbConnection db, DbTransaction tx) =>
            db.ExecuteAsync(InsertStatement, document, tx);
    }

    private static class Header
    {
        private static readonly string ExistsStatement;
        private static readonly string InsertStatement;
        private static readonly Option<string> RevisionStatement;
        private static readonly Option<string> UpdateStatement;

        static Header()
        {
            var tablePrefix = DocumentTypeCache<D>.Name;
            var hashKey = DocumentTypeCache<D>.HashKeyProperty.Name;
            var rangeKey = DocumentTypeCache<D>.RangeKeyProperty;
            var version = DocumentTypeCache<D>.VersionProperty;

            InsertStatement = BuildInsertStatement(tablePrefix, hashKey, rangeKey, version);
            UpdateStatement = BuildUpdateStatement(tablePrefix, hashKey, rangeKey, version);
            ExistsStatement = BuildExistsStatement(tablePrefix, hashKey, rangeKey);
            RevisionStatement = BuildRevisionStatement(tablePrefix, hashKey, rangeKey, version);
        }

        private static Option<string> BuildRevisionStatement(
            string tablePrefix,
            string hashKey,
            Option<PropertyInfo> rangeKey,
            Option<PropertyInfo> version)
        {
            if (version.IsNone)
                return Prelude.None;

            var versionName = version
                .IfNone(() => throw new ArgumentException("unexpected none for version"))
                .Name;

            var builder = new StringBuilder("select")
                .AppendLine(versionName)
                .AppendLine(CultureInfo.InvariantCulture, $"from {tablePrefix}_header")
                .AppendLine(CultureInfo.InvariantCulture, $"where {hashKey} = @{hashKey}");

            return rangeKey
                .Match(
                    Some: pi =>
                        builder.AppendLine(CultureInfo.InvariantCulture, $"and {pi.Name} = @{pi.Name}"),
                    None: () => builder)
                .ToString();
        }

        private static string BuildExistsStatement(
            string tablePrefix,
            string hashKey,
            Option<PropertyInfo> rangeKey)
        {
            var builder = new StringBuilder("select exists(")
                .AppendLine(CultureInfo.InvariantCulture, $"select 1 from {tablePrefix}_header")
                .AppendLine(CultureInfo.InvariantCulture, $"where {hashKey} = @{hashKey}");
            return rangeKey
                .Match(
                    Some: pi =>
                        builder.AppendLine(CultureInfo.InvariantCulture, $"and {pi.Name} = @{pi.Name}"),
                    None: () => builder)
                .Append(')')
                .ToString();
        }

        private static Option<string> BuildUpdateStatement(
            string tablePrefix,
            string hashKey,
            Option<PropertyInfo> rangeKey,
            Option<PropertyInfo> version)
        {
            if (version.IsNone)
                return Prelude.None;

            var versionName = version
                .IfNone(() => throw new ArgumentException("unexpected none for version"))
                .Name;

            var builder = new StringBuilder()
                .AppendLine(CultureInfo.InvariantCulture, $"update {tablePrefix}_header")
                .AppendLine(CultureInfo.InvariantCulture, $"set {versionName}=@NewVersion")
                .AppendLine(CultureInfo.InvariantCulture, $"where {hashKey} = @HashKey");

            return rangeKey
                .Match(
                    Some: pi =>
                        builder.AppendLine(CultureInfo.InvariantCulture, $"and {pi.Name} = @RangeKey"),
                    None: () => builder)
                .AppendLine(CultureInfo.InvariantCulture, $"and {versionName} = @OldVersion")
                .ToString();
        }

        private static string BuildInsertStatement(
            string tablePrefix,
            string hashKey,
            Option<PropertyInfo> rangeKey,
            Option<PropertyInfo> version)
        {
            var builder = new StringBuilder()
                .AppendLine(CultureInfo.InvariantCulture, $"insert into {tablePrefix}_header")
                .AppendLine(CultureInfo.InvariantCulture, $"({hashKey}");
            _ = rangeKey
                .Match(
                    Some: pi =>
                        builder.AppendLine(CultureInfo.InvariantCulture, $", {pi.Name}"),
                    None: () => builder);
            _ = version
                .Match(
                    Some: pi =>
                        builder.AppendLine(CultureInfo.InvariantCulture, $", {pi.Name}"),
                    None: () => builder)
                .AppendLine(")")
                .AppendLine("values")
                .AppendLine(CultureInfo.InvariantCulture, $"(@{hashKey}");
            _ = rangeKey
                .Match(
                    Some: pi =>
                        builder.AppendLine(CultureInfo.InvariantCulture, $", @{pi.Name}"),
                    None: () => builder);
            return version
                .Match(
                    Some: pi =>
                        builder.AppendLine(CultureInfo.InvariantCulture, $", @{pi.Name}"),
                    None: () => builder)
                .Append(')')
                .ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<int> InsertAsync(D document, DbConnection db, DbTransaction tx) =>
            db.ExecuteAsync(InsertStatement, document, tx);

        private readonly record struct UpdateParam<HK, RK, V>(HK HashKey, RK? RangeKey, V OldVersion, V NewVersion);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<int> UpdateAsync<HK, RK, V>(HK hashKey, RK? rangeKey, V oldVersion, V newVersion, DbConnection db, DbTransaction tx)
            where V : INumber<V> =>
            UpdateStatement
            .Match(
                Some: statement => db.ExecuteAsync(statement, new UpdateParam<HK, RK, V>(hashKey, rangeKey, oldVersion, newVersion), tx),
                None: () => Task.FromResult(0));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<bool> ExistsAsync(D document, DbConnection db, DbTransaction tx) =>
            db.QuerySingleAsync<bool>(ExistsStatement, document, tx);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<V?> ReadRevisionAsync<V>(D document, DbConnection db, DbTransaction tx)
            where V : INumber<V> =>
            RevisionStatement
            .Match(
                Some: statement => db.QuerySingleOrDefaultAsync<V?>(statement, document, tx),
                None: () => Task.FromResult((V?)default));
    }

    public Task InsertAsync(D document, CancellationToken token) =>
        context.ExecuteAsync((db, tx) => InsertIfNotExistsAsync(document, db, tx), token);

    private static async Task InsertIfNotExistsAsync(D document, DbConnection db, DbTransaction tx)
    {
        if (await Header.ExistsAsync(document, db, tx))
        {
            throw new DocumentWriterException(
                DuplicateKeyError(document),
                StorageErrorCodes.DuplicateKeyError);
        }

        await InsertAsync(document, db, tx);
    }

    private static string DuplicateKeyError(D document)
    {
        var hashKeyValue = DocumentTypeCache<D>.HashKeyProperty.GetValue(document);
        var builder = new StringBuilder("duplicate key (")
            .Append(CultureInfo.InvariantCulture, $"{hashKeyValue}");
        return DocumentTypeCache<D>.RangeKeyProperty
            .Match(
                Some: pi => builder.Append(CultureInfo.InvariantCulture, $", {pi.GetValue(document)}"),
                None: () => builder)
            .Append(')')
            .ToString();
    }

    private static async Task InsertAsync(D document, DbConnection db, DbTransaction tx)
    {
        document = ZeroVersion(document);

        if ((await Task.WhenAll(
                Journal.InsertAsync(document, db, tx),
                Header.InsertAsync(document, db, tx))).Sum() != 2)
            throw new DocumentWriterException(
                $"expected two records modified for document insert with key {document.HashKey}",
                StorageErrorCodes.InsertCountMismatch);
    }

    private static D ZeroVersion(D document) =>
        DocumentTypeCache<D>.VersionProperty.Match(
            Some: versionInfo =>
            {
                var constructor = typeof(D).GetConstructors()
                    .First(c => c.GetParameters().Length > 0);

                var paramValues = constructor.GetParameters()
                    .Select(p => p.Name?.Equals(versionInfo.Name, StringComparison.OrdinalIgnoreCase) == true
                        ? 0
                        : typeof(D).GetProperty(p.Name!)?.GetValue(document))
                    .ToArray();

                return (D)Activator.CreateInstance(typeof(D), paramValues)!;
            },
            None: () => document
        );

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

//internal sealed class SqliteDocumentWriter<D, HK>(
//    IDbContext context)
//    where D : Document<HK>
//    where HK : IEquatable<HK>, IComparable<HK>
//{
//    private static class Journal
//    {
//        private static readonly string InsertStatement = $"insert into {DocumentTypeCache<D>.Name}_journal ({FieldNames<D>.Columns}) values ({FieldNames<D>.Values})";

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static Task<int> InsertAsync(D document, DbConnection db, DbTransaction tx) =>
//            db.ExecuteAsync(InsertStatement, document, tx);
//    }

//    private static class Header
//    {
//        private static readonly string InsertStatement = $"insert into {DocumentTypeCache<D>.Name}_header (hashkey, version) values (@HashKey, @Version)";
//        private static readonly string UpdateStatement = $"update {DocumentTypeCache<D>.Name}_header set version = @NewVersion where hashkey = @HashKey and version = @CurrentVersion";
//        private static readonly string ExistsStatement = $"select exists(select 1 from {DocumentTypeCache<D>.Name}_header where hashkey = @HashKey)";
//        private static readonly string RevisionStatement = $"select version from {DocumentTypeCache<D>.Name}_header where hashkey = @HashKey";

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static Task<int> InsertAsync(D document, DbConnection db, DbTransaction tx) =>
//            db.ExecuteAsync(InsertStatement, document, tx);

//        private readonly record struct UpdateParam(HK HashKey, Revision CurrentVersion, Revision NewVersion);
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static Task<int> UpdateAsync(HK hashKey, Revision currentVersion, Revision newVersion, DbConnection db, DbTransaction tx) =>
//            db.ExecuteAsync(UpdateStatement, new UpdateParam(hashKey, currentVersion, newVersion), tx);

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static Task<bool> ExistsAsync(D document, DbConnection db, DbTransaction tx) =>
//            db.QuerySingleAsync<bool>(ExistsStatement, document, tx);

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public static Task<Revision?> ReadRevisionAsync(D document, DbConnection db, DbTransaction tx) =>
//            db.QuerySingleOrDefaultAsync<Revision?>(RevisionStatement, document, tx);
//    }

//    public Task InsertAsync(D document, CancellationToken token) =>
//        context.ExecuteAsync((db, tx) => InsertIfNotExistsAsync(document, db, tx), token);

//    private static async Task InsertIfNotExistsAsync(D document, DbConnection db, DbTransaction tx)
//    {
//        if (await Header.ExistsAsync(document, db, tx))
//        {
//            throw new DocumentWriterException(
//                $"duplicate key {document.HashKey}",
//                StorageErrorCodes.DuplicateKeyError);
//        }

//        await InsertAsync(document, db, tx);
//    }

//    private static async Task InsertAsync(D document, DbConnection db, DbTransaction tx)
//    {
//        document = document with { Version = Revision.Zero };

//        if ((await Task.WhenAll(
//                Journal.InsertAsync(document, db, tx),
//                Header.InsertAsync(document, db, tx))).Sum() != 2)
//            throw new DocumentWriterException(
//                $"expected two records modified for document insert with key {document.HashKey}",
//                StorageErrorCodes.InsertCountMismatch);
//    }

//    public Task UpdateAsync(D document, CancellationToken token) =>
//        context.ExecuteAsync((db, tx) => UpdateIfExistsAsync(document, db, tx), token);

//    private static async Task UpdateIfExistsAsync(D document, DbConnection db, DbTransaction tx)
//    {
//        var originalVersion = await Header.ReadRevisionAsync(document, db, tx);
//        if (!originalVersion.HasValue)
//            throw new DocumentWriterException(
//                $"key not found {document.HashKey}",
//                StorageErrorCodes.NotFoundError);

//        if (IsVersionMismatch(originalVersion.Value, document.Version))
//            throw new DocumentWriterException(
//                $"version conflict {document.HashKey}, expected version: {document.Version}, actual version: {originalVersion}",
//                StorageErrorCodes.VersionConflictError);

//        await UpdateAsync(document, db, tx);
//    }

//    private static async Task UpdateAsync(D document, DbConnection db, DbTransaction tx)
//    {
//        var currentVersion = document.Version;
//        var newVersion = document.Version.Tick();
//        if (await Journal.InsertAsync(document with { Version = newVersion }, db, tx) != 1)
//            throw new DocumentWriterException(
//                $"journal insert failed for with key {document.HashKey}, version {newVersion}",
//                StorageErrorCodes.InsertCountMismatch);

//        if (await Header.UpdateAsync(document.HashKey, currentVersion, newVersion, db, tx) != 1)
//            throw new DocumentWriterException(
//                $"version conflict {document.HashKey}, expected: {document.Version}, actual: unknown",
//                StorageErrorCodes.VersionConflictError);
//    }

//    public Task InsertOrUpdateAsync(D document, CancellationToken token) =>
//        context.ExecuteAsync((db, tx) => InsertOrUpdateAsync(document, db, tx), token);

//    private static async Task InsertOrUpdateAsync(D document, DbConnection db, DbTransaction tx)
//    {
//        var originalVersion = await Header.ReadRevisionAsync(document, db, tx);
//        if (!originalVersion.HasValue)
//        {
//            await InsertAsync(document, db, tx);
//            return;
//        }

//        if (IsVersionMismatch(originalVersion.Value, document.Version))
//        {
//            throw new DocumentWriterException(
//                $"version conflict {document.HashKey}, expected: {document.Version}, actual: {originalVersion}",
//                StorageErrorCodes.VersionConflictError);
//        }

//        await UpdateAsync(document, db, tx);
//    }

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    private static bool IsVersionMismatch(Revision original, Revision replacement) =>
//        original != replacement;
//}
