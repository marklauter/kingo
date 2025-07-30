using Dapper;
using Kingo.Storage.Context;
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
    private static readonly bool IsVersioned = DocumentTypeCache<D>.VersionProperty.IsSome;

    private static class Journal
    {
        private static readonly string InsertStatement = $"insert into {DocumentTypeCache<D>.Name}_journal ({FieldNames<D>.Columns}) values ({FieldNames<D>.Values})";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<int> InsertAsync(D document, DbConnection db, DbTransaction tx) =>
            db.ExecuteAsync(InsertStatement, document, tx);
    }

    private static class Table
    {
        private static readonly string InsertStatement = $"insert into {DocumentTypeCache<D>.Name} ({FieldNames<D>.Columns}) values ({FieldNames<D>.Values})";
        private static readonly string UpdateStatement;
        private static readonly string DeleteStatement;
        private static readonly string ExistsStatement;

        static Table()
        {
            var table = DocumentTypeCache<D>.Name;
            var hashKeyProperty = DocumentTypeCache<D>.HashKeyProperty;
            var rangeKeyProperty = DocumentTypeCache<D>.RangeKeyProperty;

            var hashKeyColumn = hashKeyProperty.Name;
            var whereClause = new StringBuilder($"where {hashKeyColumn} = @{hashKeyProperty.Name}");

            _ = rangeKeyProperty.IfSome(pi =>
            {
                var rangeKeyColumn = pi.Name;
                var s = $"and {rangeKeyColumn} = @{pi.Name}";
                _ = whereClause.AppendLine(s);
            });

            var keyProperties = new System.Collections.Generic.HashSet<string> { DocumentTypeCache<D>.HashKeyProperty.Name };
            _ = rangeKeyProperty.IfSome(pi => keyProperties.Add(pi.Name));

            var updateColumns = DocumentTypeCache<D>.MappedProperties
                .Where(pi => !keyProperties.Contains(pi.Name))
                .Select(pi => $"{pi.Name} = @{pi.Name}");

            UpdateStatement = $"update {table} set {string.Join(", ", updateColumns)} {whereClause}";
            DeleteStatement = $"delete from {table} {whereClause}";
            ExistsStatement = $"select exists(select 1 from {table} {whereClause})";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<int> InsertAsync(D document, DbConnection db, DbTransaction tx) =>
            db.ExecuteAsync(InsertStatement, document, tx);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<int> UpdateAsync(D document, DbConnection db, DbTransaction tx) =>
            db.ExecuteAsync(UpdateStatement, document, tx);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<int> DeleteAsync(D document, DbConnection db, DbTransaction tx) =>
            db.ExecuteAsync(DeleteStatement, document, tx);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<bool> ExistsAsync(D document, DbConnection db, DbTransaction tx) =>
            db.QuerySingleAsync<bool>(ExistsStatement, document, tx);
    }

    private static class Header
    {
        private static readonly string InsertStatement;
        private static readonly string UpdateStatement;
        private static readonly string DeleteStatement;
        private static readonly string ExistsStatement;
        private static readonly string RevisionStatement;

        static Header()
        {
            var table = $"{DocumentTypeCache<D>.Name}_header";
            var hashKeyProperty = DocumentTypeCache<D>.HashKeyProperty;
            var rangeKeyProperty = DocumentTypeCache<D>.RangeKeyProperty;
            var versionProperty = DocumentTypeCache<D>.VersionProperty.IfNone(() => throw new InvalidOperationException("Version property required for header operations"));

            var hashKeyColumn = hashKeyProperty.Name;
            var versionColumn = versionProperty.Name;

            var insertCols = new StringBuilder($"{hashKeyColumn}, {versionColumn}");
            var insertVals = new StringBuilder($"@{hashKeyProperty.Name}, @{versionProperty.Name}");
            var whereClause = new StringBuilder($"where {hashKeyColumn} = @{hashKeyProperty.Name}");

            _ = rangeKeyProperty.IfSome(pi =>
            {
                var rangeKeyColumn = pi.Name;
                _ = insertCols.Insert(hashKeyColumn.Length + 2, $"{rangeKeyColumn}, ");
                _ = insertVals.Insert($"@{hashKeyProperty.Name}".Length + 2, $"@{pi.Name}, ");
                var s = $" and {rangeKeyColumn} = @{pi.Name}";
                _ = whereClause.Append(s);
            });

            InsertStatement = $"insert into {table} ({insertCols}) values ({insertVals})";
            UpdateStatement = $"update {table} set {versionColumn} = @NewVersion {whereClause} and {versionColumn} = @OldVersion";
            DeleteStatement = $"delete from {table} {whereClause}";
            ExistsStatement = $"select exists(select 1 from {table} {whereClause})";
            RevisionStatement = $"select {versionColumn} from {table} {whereClause}";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<int> InsertAsync(D document, DbConnection db, DbTransaction tx) =>
            db.ExecuteAsync(InsertStatement, document, tx);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<int> UpdateAsync<V>(D document, V newVersion, DbConnection db, DbTransaction tx)
            where V : INumber<V>
        {
            var versionProperty = DocumentTypeCache<D>.VersionProperty.IfNone(() => throw new InvalidOperationException("Version property required"));
            var oldVersion = (V)versionProperty.GetValue(document)!;

            var parameters = new DynamicParameters(document);
            parameters.Add("NewVersion", newVersion);
            parameters.Add("OldVersion", oldVersion);
            return db.ExecuteAsync(UpdateStatement, parameters, tx);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<int> DeleteAsync(D document, DbConnection db, DbTransaction tx) =>
            db.ExecuteAsync(DeleteStatement, document, tx);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<bool> ExistsAsync(D document, DbConnection db, DbTransaction tx) =>
            db.QuerySingleAsync<bool>(ExistsStatement, document, tx);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<V?> ReadRevisionAsync<V>(D document, DbConnection db, DbTransaction tx)
            where V : struct, INumber<V> =>
            db.QuerySingleOrDefaultAsync<V?>(RevisionStatement, document, tx);
    }

    public Task InsertAsync(D document, CancellationToken token) =>
        context.ExecuteAsync((db, tx) => InsertIfNotExistsAsync(document, db, tx), token);

    private static async Task InsertIfNotExistsAsync(D document, DbConnection db, DbTransaction tx)
    {
        if (await ExistsAsync(document, db, tx))
            throw new DocumentWriterException(
                BuildDuplicateKeyError(document),
                StorageErrorCodes.DuplicateKeyError);

        await InsertInternalAsync(document, db, tx);
    }

    private static Task<bool> ExistsAsync(D document, DbConnection db, DbTransaction tx) =>
        IsVersioned
            ? Header.ExistsAsync(document, db, tx)
            : Table.ExistsAsync(document, db, tx);

    private static async Task InsertInternalAsync(D document, DbConnection db, DbTransaction tx)
    {
        if (IsVersioned)
        {
            document = ZeroVersion(document);

            if ((await Task.WhenAll(
                    Journal.InsertAsync(document, db, tx),
                    Header.InsertAsync(document, db, tx))).Sum() != 2)
                throw new DocumentWriterException(
                    $"expected two records modified for document insert with key {BuildKeyString(document)}",
                    StorageErrorCodes.InsertCountMismatch);
        }
        else
        {
            if (await Table.InsertAsync(document, db, tx) != 1)
                throw new DocumentWriterException(
                    $"expected one record modified for document insert with key {BuildKeyString(document)}",
                    StorageErrorCodes.InsertCountMismatch);
        }
    }

    public Task UpdateAsync(D document, CancellationToken token) =>
        context.ExecuteAsync((db, tx) => UpdateIfExistsAsync(document, db, tx), token);

    private static async Task UpdateIfExistsAsync(D document, DbConnection db, DbTransaction tx)
    {
        if (IsVersioned)
        {
            await DocumentTypeCache<D>.VersionProperty.Match(
                Some: async versionProperty =>
                {
                    var versionType = versionProperty.PropertyType;
                    var readRevisionMethod = typeof(Header)
                        .GetMethod(nameof(Header.ReadRevisionAsync))!
                        .MakeGenericMethod(versionType);

                    var originalVersionTask = (Task)readRevisionMethod.Invoke(null, [document, db, tx])!;
                    await originalVersionTask;

                    var originalVersion = originalVersionTask.GetType().GetProperty("Result")!.GetValue(originalVersionTask)
                        ?? throw new DocumentWriterException(
                            $"key not found {BuildKeyString(document)}",
                            StorageErrorCodes.NotFoundError);

                    var currentVersion = versionProperty.GetValue(document)!;

                    if (!currentVersion.Equals(originalVersion))
                        throw new DocumentWriterException(
                            $"version conflict {BuildKeyString(document)}, expected version: {currentVersion}, actual version: {originalVersion}",
                            StorageErrorCodes.VersionConflictError);

                    await UpdateVersionedAsync(document, db, tx);
                },
                None: () => throw new InvalidOperationException("Versioned document must have version property"));
        }
        else
        {
            if (!await Table.ExistsAsync(document, db, tx))
                throw new DocumentWriterException(
                    $"key not found {BuildKeyString(document)}",
                    StorageErrorCodes.NotFoundError);

            if (await Table.UpdateAsync(document, db, tx) != 1)
                throw new DocumentWriterException(
                    $"expected one record modified for document update with key {BuildKeyString(document)}",
                    StorageErrorCodes.InsertCountMismatch);
        }
    }

    private static async Task UpdateVersionedAsync(D document, DbConnection db, DbTransaction tx)
    {
        var versionProperty = DocumentTypeCache<D>.VersionProperty.IfNone(() => throw new InvalidOperationException("Version property is required"));
        var versionType = versionProperty.PropertyType;
        var currentVersion = versionProperty.GetValue(document)!;

        var newVersion = IncrementVersion(currentVersion, versionType);
        var newDocument = SetVersion(document, newVersion);

        if (await Journal.InsertAsync(newDocument, db, tx) != 1)
            throw new DocumentWriterException(
                $"journal insert failed for key {BuildKeyString(document)}, version {newVersion}",
                StorageErrorCodes.InsertCountMismatch);

        var updateMethod = typeof(Header)
            .GetMethod(nameof(Header.UpdateAsync))!
            .MakeGenericMethod(versionType);

        var updateResult = await (Task<int>)updateMethod.Invoke(null, [document, newVersion, db, tx])!;

        if (updateResult != 1)
            throw new DocumentWriterException(
                $"version conflict {BuildKeyString(document)}, expected: {currentVersion}, actual: unknown",
                StorageErrorCodes.VersionConflictError);
    }

    public Task InsertOrUpdateAsync(D document, CancellationToken token) =>
        context.ExecuteAsync((db, tx) => InsertOrUpdateInternalAsync(document, db, tx), token);

    private static async Task InsertOrUpdateInternalAsync(D document, DbConnection db, DbTransaction tx)
    {
        if (!await ExistsAsync(document, db, tx))
        {
            await InsertInternalAsync(document, db, tx);
            return;
        }

        if (IsVersioned)
        {
            await DocumentTypeCache<D>.VersionProperty.Match(
                Some: async versionProperty =>
                {
                    var versionType = versionProperty.PropertyType;
                    var readRevisionMethod = typeof(Header)
                        .GetMethod(nameof(Header.ReadRevisionAsync))!
                        .MakeGenericMethod(versionType);

                    var originalVersionTask = (Task)readRevisionMethod.Invoke(null, [document, db, tx])!;
                    await originalVersionTask;

                    var originalVersion = originalVersionTask.GetType().GetProperty("Result")!.GetValue(originalVersionTask);
                    var currentVersion = versionProperty.GetValue(document)!;

                    if (originalVersion is not null && !currentVersion.Equals(originalVersion))
                        throw new DocumentWriterException(
                            $"version conflict {BuildKeyString(document)}, expected: {currentVersion}, actual: {originalVersion}",
                            StorageErrorCodes.VersionConflictError);

                    await UpdateVersionedAsync(document, db, tx);
                },
                None: () => throw new InvalidOperationException("Versioned document must have version property"));
            return;
        }

        if (await Table.UpdateAsync(document, db, tx) != 1)
            throw new DocumentWriterException(
                $"expected one record modified for document update with key {BuildKeyString(document)}",
                StorageErrorCodes.InsertCountMismatch);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object IncrementVersion(object version, Type versionType)
    {
        var incrementMethod = versionType.GetMethod("op_Increment");
        if (incrementMethod is not null)
            return incrementMethod.Invoke(null, [version])!;

        var oneValue = Convert.ChangeType(1, versionType, CultureInfo.InvariantCulture);
        var addMethod = typeof(INumber<>).MakeGenericType(versionType)
            .GetMethod("op_Addition", [versionType, versionType]);
        return addMethod?.Invoke(null, [version, oneValue]) ??
               throw new InvalidOperationException($"Cannot increment version of type {versionType}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static D ZeroVersion(D document) =>
        DocumentTypeCache<D>.VersionProperty.Match(
            Some: versionProperty =>
            {
                var zeroValue = Convert.ChangeType(0, versionProperty.PropertyType, CultureInfo.InvariantCulture);
                return SetVersion(document, zeroValue);
            },
            None: () => document);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static D SetVersion(D document, object version)
    {
        var versionProperty = DocumentTypeCache<D>.VersionProperty.IfNone(() => throw new InvalidOperationException("Version property is required"));

        var constructor = typeof(D).GetConstructors()
            .FirstOrDefault(c => c.GetParameters().Length > 0);

        if (constructor is not null)
        {
            var paramValues = constructor.GetParameters()
                .Select(p => string.Equals(p.Name, versionProperty.Name, StringComparison.OrdinalIgnoreCase)
                    ? version
                    : typeof(D).GetProperty(p.Name!, BindingFlags.Public | BindingFlags.Instance)?.GetValue(document))
                .ToArray();

            return (D)Activator.CreateInstance(typeof(D), paramValues)!;
        }

        var newDocument = Activator.CreateInstance<D>();
        foreach (var prop in DocumentTypeCache<D>.MappedProperties)
        {
            if (prop.CanWrite)
            {
                var value = string.Equals(prop.Name, versionProperty.Name, StringComparison.OrdinalIgnoreCase)
                    ? version
                    : prop.GetValue(document);
                prop.SetValue(newDocument, value);
            }
        }

        return newDocument;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string BuildKeyString(D document)
    {
        var hashKeyValue = DocumentTypeCache<D>.HashKeyProperty.GetValue(document);
        var builder = new StringBuilder().Append(hashKeyValue);

        return DocumentTypeCache<D>.RangeKeyProperty.Match(
            Some: pi => builder.Append(CultureInfo.InvariantCulture, $":{pi.GetValue(document)}"),
            None: () => builder)
            .ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string BuildDuplicateKeyError(D document)
    {
        var hashKeyValue = DocumentTypeCache<D>.HashKeyProperty.GetValue(document);
        var builder = new StringBuilder("duplicate key (")
            .Append(CultureInfo.InvariantCulture, $"{hashKeyValue}");

        return DocumentTypeCache<D>.RangeKeyProperty.Match(
            Some: pi => builder.Append(CultureInfo.InvariantCulture, $", {pi.GetValue(document)}"),
            None: () => builder)
            .Append(')')
            .ToString();
    }
}
