using Dapper;
using Kingo.Storage.Clocks;
using Kingo.Storage.Keys;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Data.Sqlite;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Kingo.Storage.Sqlite;

public sealed class SqliteDocumentWriter<HK>(
    SqliteConnection connection,
    Key table)
    : IDisposable
    , IDocumentWriter<HK>
    where HK : IEquatable<HK>, IComparable<HK>
{
    private readonly string findQuery = $"select version from {table}_header where hashkey = @HashKey;";
    private readonly string journalInsert = $"insert into {table}_journal (hashkey, version, data) values (@HashKey, @Version, @Data);";
    private readonly string headerInsert = $"insert into {table}_header (hashkey, version) values (@HashKey, @Version);";
    private readonly string journalUpdate = $"insert into {table}_journal (hashkey, version, data) values (@HashKey, @Version, @Data);";
    private readonly string headerUpdate = $"update {table}_header set version = @Version where hashkey = @HashKey;";

    public Either<DocumentWriterError, Unit> Insert(Document<HK> document, CancellationToken cancellationToken) =>
        WithTransaction(transaction =>
            ReadRevision(document.HashKey, transaction).Match(
                Some: _ => DocumentWriterError.New(ErrorCodes.DuplicateKeyError, $"duplicate key {document.HashKey}"),
                None: () => InsertInternal(document, transaction)));

    public Either<DocumentWriterError, Unit> InsertOrUpdate(Document<HK> document, CancellationToken cancellationToken) =>
        WithTransaction(transaction =>
            ReadRevision(document.HashKey, transaction).Match(
                Some: rev =>
                {
                    var newDocument = document with { Version = rev.Tick() };
                    return CheckVersion(header.Version, document.Version, document.HashKey)
                        .Bind(_ => UpdateInternal(rev, newDocument, transaction));
                },
                None: () => InsertInternal(document, transaction)));

    public Either<DocumentWriterError, Unit> Update(Document<HK> document, CancellationToken cancellationToken) =>
        WithTransaction(transaction =>
            ReadRevision(document.HashKey, transaction).Match(
                Some: header =>
                {
                    var newDocument = document with { Version = header.Version.Tick() };
                    return CheckVersion(header.Version, document.Version, document.HashKey)
                        .Bind(_ => UpdateInternal(header.Id, newDocument, transaction));
                },
                None: () => DocumentWriterError.New(ErrorCodes.NotFoundError, $"key not found {document.HashKey}")));

    private Either<DocumentWriterError, Unit> WithTransaction(Func<SqliteTransaction, Either<DocumentWriterError, Unit>> operation)
    {
        using var transaction = connection.BeginTransaction();
        try
        {
            var result = operation(transaction);
            if (result.IsRight)
            {
                transaction.Commit();
            }
            else
            {
                transaction.Rollback();
            }

            return result;
        }
        catch (Exception e)
        {
            transaction.Rollback();
            return DocumentWriterError.New(ErrorCodes.UnknownError, "Transaction failed", Error.New(e));
        }
    }

    private Option<Revision> ReadRevision(HK hashKey, SqliteTransaction transaction) =>
        connection.QuerySingleOrDefault<Revision>(findQuery, new { HashKey = hashKey }, transaction);

    private Either<DocumentWriterError, Unit> InsertInternal(Document<HK> document, SqliteTransaction transaction)
    {
        var doc = document with { Version = Revision.Zero };
        var data = JsonSerializer.Serialize(doc.Data);

        var id = connection.ExecuteScalar<long>(journalInsert, new { doc.HashKey, doc.Version, Data = data }, transaction);
        _ = connection.Execute(headerInsert, new { Id = id, doc.HashKey, doc.Version }, transaction);

        return Prelude.unit;
    }

    private Either<DocumentWriterError, Unit> UpdateInternal(Document<HK> document, SqliteTransaction transaction)
    {
        var data = JsonSerializer.Serialize(document.Data);

        _ = connection.Execute(journalUpdate, new { Id = id, document.HashKey, document.Version, Data = data }, transaction);
        _ = connection.Execute(headerUpdate, new { document.Version, Id = id }, transaction);

        return Prelude.unit;
    }

    private static Either<DocumentWriterError, Unit> CheckVersion(Revision original, Revision replacement, HK hashKey) =>
        original == replacement
            ? Prelude.unit
            : DocumentWriterError.New(ErrorCodes.VersionConflictError, $"version conflict {hashKey}, expected: {replacement}, actual: {original}");

    [SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP007:Don't dispose injected", Justification = "reader owns the connection")]
    public void Dispose() => connection.Dispose();
}

public sealed class SqliteDocumentWriter<HK, RK>(
    SqliteConnection connection,
    Key table)
    : IDisposable
    , IDocumentWriter<HK, RK>
    where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>
{
    private readonly string findQuery = $"SELECT Id, HashKey, RangeKey, Version FROM {table}_header WHERE HashKey = @HashKey AND RangeKey = @RangeKey";
    private readonly string insertJournalQuery = $"INSERT INTO {table}_journal (HashKey, RangeKey, Version, Data) VALUES (@HashKey, @RangeKey, @Version, @Data); SELECT last_insert_rowid();";
    private readonly string insertHeaderQuery = $"INSERT INTO {table}_header (Id, HashKey, RangeKey, Version) VALUES (@Id, @HashKey, @RangeKey, @Version);";
    private readonly string updateJournalQuery = $"INSERT INTO {table}_journal (Id, HashKey, RangeKey, Version, Data) VALUES (@Id, @HashKey, @RangeKey, @Version, @Data);";
    private readonly string updateHeaderQuery = $"UPDATE {table}_header SET Version = @Version WHERE Id = @Id;";

    private record DocumentHeader(long Id, HK HashKey, RK RangeKey, Revision Version);

    public Either<DocumentWriterError, Unit> Insert(Document<HK, RK> document, CancellationToken cancellationToken) =>
        WithTransaction(transaction =>
            Find(document.HashKey, document.RangeKey, transaction).Match(
                Some: _ => DocumentWriterError.New(ErrorCodes.DuplicateKeyError, $"duplicate key {document.HashKey}/{document.RangeKey}"),
                None: () => InsertInternal(document, transaction)));

    public Either<DocumentWriterError, Unit> InsertOrUpdate(Document<HK, RK> document, CancellationToken cancellationToken) =>
        WithTransaction(transaction =>
            Find(document.HashKey, document.RangeKey, transaction).Match(
                Some: header =>
                {
                    var newDocument = document with { Version = header.Version.Tick() };
                    return CheckVersion(header.Version, document.Version, document.HashKey, document.RangeKey)
                        .Bind(_ => UpdateInternal(header.Id, newDocument, transaction));
                },
                None: () => InsertInternal(document, transaction)));

    public Either<DocumentWriterError, Unit> Update(Document<HK, RK> document, CancellationToken cancellationToken) =>
        WithTransaction(transaction =>
            Find(document.HashKey, document.RangeKey, transaction).Match(
                Some: header =>
                {
                    var newDocument = document with { Version = header.Version.Tick() };
                    return CheckVersion(header.Version, document.Version, document.HashKey, document.RangeKey)
                        .Bind(_ => UpdateInternal(header.Id, newDocument, transaction));
                },
                None: () => DocumentWriterError.New(ErrorCodes.NotFoundError, $"key not found {document.HashKey}/{document.RangeKey}")));

    private Either<DocumentWriterError, Unit> WithTransaction(Func<SqliteTransaction, Either<DocumentWriterError, Unit>> operation)
    {
        using var transaction = connection.BeginTransaction();
        try
        {
            var result = operation(transaction);
            if (result.IsRight)
            {
                transaction.Commit();
            }
            else
            {
                transaction.Rollback();
            }

            return result;
        }
        catch (Exception e)
        {
            transaction.Rollback();
            return DocumentWriterError.New(ErrorCodes.UnknownError, "Transaction failed", Error.New(e));
        }
    }

    private Option<DocumentHeader> Find(HK hashKey, RK rangeKey, SqliteTransaction transaction) =>
        connection.QuerySingleOrDefault<DocumentHeader>(findQuery, new { HashKey = hashKey, RangeKey = rangeKey }, transaction);

    private Either<DocumentWriterError, Unit> InsertInternal(Document<HK, RK> document, SqliteTransaction transaction)
    {
        var doc = document with { Version = Revision.Zero };
        var data = JsonSerializer.Serialize(doc.Data);

        var id = connection.ExecuteScalar<long>(insertJournalQuery, new { doc.HashKey, doc.RangeKey, doc.Version, Data = data }, transaction);
        _ = connection.Execute(insertHeaderQuery, new { Id = id, doc.HashKey, doc.RangeKey, doc.Version }, transaction);

        return Prelude.unit;
    }

    private Either<DocumentWriterError, Unit> UpdateInternal(long id, Document<HK, RK> document, SqliteTransaction transaction)
    {
        var data = JsonSerializer.Serialize(document.Data);

        _ = connection.Execute(updateJournalQuery, new { Id = id, document.HashKey, document.RangeKey, document.Version, Data = data }, transaction);
        _ = connection.Execute(updateHeaderQuery, new { document.Version, Id = id }, transaction);

        return Prelude.unit;
    }

    private static Either<DocumentWriterError, Unit> CheckVersion(Revision original, Revision replacement, HK hashKey, RK rangeKey) =>
        original == replacement
            ? Prelude.unit
            : DocumentWriterError.New(ErrorCodes.VersionConflictError, $"version conflict {hashKey}/{rangeKey}, expected: {replacement}, actual: {original}");

    [SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP007:Don't dispose injected", Justification = "reader owns the connection")]
    public void Dispose() => connection.Dispose();
}
