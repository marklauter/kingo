using Dapper;
using Kingo.Storage.Clocks;
using Kingo.Storage.Keys;
using LanguageExt;
using Microsoft.Data.Sqlite;
using System.Diagnostics.CodeAnalysis;

namespace Kingo.Storage.SQLite;

internal record DocumentHeader<HK>(long Id, HK HashKey, Revision Version)
    where HK : IEquatable<HK>, IComparable<HK>;

public static class SQLiteDocumentReader
{
    [SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP004:Don't ignore created IDisposable", Justification = "disposed by reader")]
    public static Either<SqliteError, SqliteDocumentReader<HK>> Cons<HK>(
        string connectionString,
        Key table)
        where HK : IEquatable<HK>, IComparable<HK> =>
        Open(new SqliteConnection(connectionString))
        .Map(conn => new SqliteDocumentReader<HK>(conn, table));

    private static Either<SqliteError, SqliteConnection> Open(SqliteConnection connection) =>
        Try.lift(connection.Open)
        .Match<Either<SqliteError, SqliteConnection>>(
            Fail: e => SqliteError.New(ErrorCodes.SqliteError, "failed to connect", e),
            Succ: _ => connection);
}

public sealed class SqliteDocumentReader<HK>(
    SqliteConnection connection,
    Key table)
    : IDisposable
    , IDocumentReader<HK>
    where HK : IEquatable<HK>, IComparable<HK>
{
    private readonly string query =
        $"select a.haskey, a.version, b.data from {table}_header a join {table}_journal b on b.id = a.id and b.version = a.version where a.haskey = @HashKey";

    public Option<Document<HK>> Find(HK hashKey) =>
        connection.QuerySingle<Document<HK>>(query, hashKey);

    [SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP007:Don't dispose injected", Justification = "reader owns the connection")]
    public void Dispose() => connection.Dispose();
}

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
