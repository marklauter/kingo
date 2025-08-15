using Dapper;
using Kingo.Storage.Context;
using System.Data.Common;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Kingo.Storage.Sqlite;

internal sealed class SqliteDocumentWriter<D>(IDbContext context)
{
    private static readonly bool IsVersioned = DocumentTypeCache<D>.VersionProperty is not null;

    private static class Header
    {
        private static readonly string InsertStatement;
        private static readonly string UpdateStatement;
        private static readonly string DeleteStatement;
        private static readonly string ExistsStatement;
        private static readonly string RevisionStatement;

        static Header()
        {
            var hashKeyProperty = DocumentTypeCache<D>.HashKeyProperty;
            var versionProperty = DocumentTypeCache<D>.VersionProperty is not null
                ? DocumentTypeCache<D>.VersionProperty!
                : throw new SqlBuilderException($"version property is required for header operations. type: {DocumentTypeCache<D>.Name}");

            var versionColumn = versionProperty.Name;

            var insertCols = new StringBuilder($"{hashKeyProperty.Name}, {versionColumn}");
            var insertVals = new StringBuilder($"@{hashKeyProperty.Name}, @{versionProperty.Name}");
            var whereClause = new StringBuilder($"where {hashKeyProperty.Name} = @{hashKeyProperty.Name}");

            if (DocumentTypeCache<D>.RangeKeyProperty is PropertyInfo pi)
            {
                _ = insertCols.Insert(hashKeyProperty.Name.Length + 2, $"{pi.Name}, ");
                _ = insertVals.Insert($"@{hashKeyProperty.Name}".Length + 2, $"@{pi.Name}, ");
                var s = $" and {pi.Name} = @{pi.Name}";
                _ = whereClause.Append(s);
            }

            var table = $"{DocumentTypeCache<D>.Name}_header";
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
            var versionProperty = DocumentTypeCache<D>.VersionProperty is not null
                ? DocumentTypeCache<D>.VersionProperty!
                : throw new SqlBuilderException($"version property is required. type: {DocumentTypeCache<D>.Name}");
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
            var whereClause = new StringBuilder($"where {DocumentTypeCache<D>.HashKeyProperty.Name} = @{DocumentTypeCache<D>.HashKeyProperty.Name}");

            var keyProperties = new HashSet<string> { DocumentTypeCache<D>.HashKeyProperty.Name };
            if (DocumentTypeCache<D>.RangeKeyProperty is PropertyInfo rkpi)
            {
                var rangeKeyColumn = rkpi.Name;
                var s = $" and {rangeKeyColumn} = @{rkpi.Name}";
                _ = whereClause.Append(s);
                _ = keyProperties.Add(rkpi.Name);
            }

            var updateColumns = DocumentTypeCache<D>.MappedProperties
                .Where(pi => !keyProperties.Contains(pi.Name))
                .Select(pi => $"{pi.Name} = @{pi.Name}");

            var table = DocumentTypeCache<D>.Name;
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

    public Task InsertAsync(D document, CancellationToken token) =>
        context.ExecuteAsync((db, tx) => InsertIfNotExistsAsync(document, db, tx), token);

    private static async Task InsertIfNotExistsAsync(D document, DbConnection db, DbTransaction tx)
    {
        if (await ExistsAsync(document, db, tx))
            throw new DuplicateKeyException(BuildDuplicateKeyError(document));

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
                throw new WriterException(
                    $"expected two records modified for document insert with key {BuildKeyString(document)}");
        }
        else
        {
            if (await Table.InsertAsync(document, db, tx) != 1)
                throw new WriterException(
                    $"expected one record modified for document insert with key {BuildKeyString(document)}");
        }
    }

    public Task UpdateAsync(D document, CancellationToken token) =>
        context.ExecuteAsync((db, tx) => UpdateIfExistsAsync(document, db, tx), token);

    private static async Task UpdateIfExistsAsync(D document, DbConnection db, DbTransaction tx)
    {
        if (IsVersioned)
        {
            var versionProperty = DocumentTypeCache<D>.VersionProperty is not null
                ? DocumentTypeCache<D>.VersionProperty!
                : throw new SqlBuilderException($"versioned document must have version property, type: '{DocumentTypeCache<D>.Name}'");

            var versionType = versionProperty.PropertyType;
            var versionTask = (Task)typeof(Header)
                .GetMethod(nameof(Header.ReadRevisionAsync))!
                .MakeGenericMethod(versionType)
                .Invoke(null, [document, db, tx])!;

            await versionTask;

            var originalVersion = versionTask.GetType().GetProperty("Result")!.GetValue(versionTask)
                ?? throw new KeyNotFoundException(
                    $"key not found {BuildKeyString(document)}");

            var currentVersion = versionProperty.GetValue(document)!;

            if (!currentVersion.Equals(originalVersion))
                throw new VersionConflictException(
                    $"version conflict {BuildKeyString(document)}, expected version: {currentVersion}, actual version: {originalVersion}");

            await UpdateVersionedAsync(document, db, tx);

            return;
        }

        if (!await Table.ExistsAsync(document, db, tx))
            throw new NotFoundException(
                $"document not found {BuildKeyString(document)}");

        if (await Table.UpdateAsync(document, db, tx) != 1)
            throw new NotFoundException(
                $"expected one record modified for document update with key {BuildKeyString(document)}");
    }

    private static async Task UpdateVersionedAsync(D document, DbConnection db, DbTransaction tx)
    {
        var versionProperty = DocumentTypeCache<D>.VersionProperty is not null
            ? DocumentTypeCache<D>.VersionProperty!
            : throw new WriterException($"version property is required. type: '{DocumentTypeCache<D>.Name}'");

        var versionType = versionProperty.PropertyType;
        var currentVersion = versionProperty.GetValue(document)!;

        var newVersion = IncrementVersion(currentVersion, versionType);
        var newDocument = SetVersion(document, newVersion);

        if (await Journal.InsertAsync(newDocument, db, tx) != 1)
            throw new WriterException(
                $"journal insert failed for key {BuildKeyString(document)}, version {newVersion}");

        var updateMethod = typeof(Header)
            .GetMethod(nameof(Header.UpdateAsync))!
            .MakeGenericMethod(versionType);

        var updateResult = await (Task<int>)updateMethod.Invoke(null, [document, newVersion, db, tx])!;

        if (updateResult != 1)
            throw new VersionConflictException(
                $"version conflict {BuildKeyString(document)}, expected: {currentVersion}, actual: unknown");
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
            var versionProperty = DocumentTypeCache<D>.VersionProperty is not null
                ? DocumentTypeCache<D>.VersionProperty!
                : throw new WriterException("Versioned document must have version property");

            var versionType = versionProperty.PropertyType;
            var readRevisionMethod = typeof(Header)
                .GetMethod(nameof(Header.ReadRevisionAsync))!
                .MakeGenericMethod(versionType);

            var originalVersionTask = (Task)readRevisionMethod.Invoke(null, [document, db, tx])!;
            await originalVersionTask;

            var originalVersion = originalVersionTask.GetType().GetProperty("Result")!.GetValue(originalVersionTask);
            var currentVersion = versionProperty.GetValue(document)!;

            if (originalVersion is not null && !currentVersion.Equals(originalVersion))
                throw new VersionConflictException(
                    $"version conflict {BuildKeyString(document)}, expected: {currentVersion}, actual: {originalVersion}");

            await UpdateVersionedAsync(document, db, tx);

            return;
        }

        if (await Table.UpdateAsync(document, db, tx) != 1)
            throw new WriterException(
                $"expected one record modified for document update with key {BuildKeyString(document)}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object IncrementVersion(object version, Type versionType)
    {
        // Handle common numeric types directly
        if (versionType == typeof(int))
            return (int)version + 1;
        if (versionType == typeof(long))
            return (long)version + 1L;
        if (versionType == typeof(uint))
            return (uint)version + 1U;
        if (versionType == typeof(ulong))
            return (ulong)version + 1UL;

        // Try reflection approach for other INumber<T> types
        var incrementMethod = versionType.GetMethod("op_Increment");
        if (incrementMethod is not null)
            return incrementMethod.Invoke(null, [version])!;

        // Fallback to addition using dynamic
        var oneValue = Convert.ChangeType(1, versionType, CultureInfo.InvariantCulture);
        try
        {
            dynamic dynamicVersion = version;
            dynamic dynamicOne = oneValue;
            return dynamicVersion + dynamicOne;
        }
        catch
        {
            throw new InvalidOperationException($"Cannot increment version of type {versionType}");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static D ZeroVersion(D document) =>
        DocumentTypeCache<D>.VersionProperty is PropertyInfo versionProperty
            ? SetVersion(document, Convert.ChangeType(0, versionProperty.PropertyType, CultureInfo.InvariantCulture))
            : document;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static D SetVersion(D document, object version)
    {
        var versionProperty = DocumentTypeCache<D>.VersionProperty is not null
            ? DocumentTypeCache<D>.VersionProperty!
            : throw new WriterException($"version property is required. type: '{DocumentTypeCache<D>.Name}'");

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

        return (
            DocumentTypeCache<D>.RangeKeyProperty is PropertyInfo pi
                ? builder.Append(CultureInfo.InvariantCulture, $":{pi.GetValue(document)}")
                : builder
            ).ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string BuildDuplicateKeyError(D document)
    {
        var hashKeyValue = DocumentTypeCache<D>.HashKeyProperty.GetValue(document);
        var builder = new StringBuilder("duplicate key (")
            .Append(CultureInfo.InvariantCulture, $"{hashKeyValue}");

        return (
            DocumentTypeCache<D>.RangeKeyProperty is not null
                ? builder.Append(CultureInfo.InvariantCulture, $", {DocumentTypeCache<D>.RangeKeyProperty.GetValue(document)}")
                : builder.Append(')')
            ).ToString();
    }
}
