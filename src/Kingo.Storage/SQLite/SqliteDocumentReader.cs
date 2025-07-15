using Dapper;
using Kingo.Storage.Keys;
using LanguageExt;
using Microsoft.Data.Sqlite;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Kingo.Storage.Sqlite;

public static class SQLiteDocumentReader
{
    public static Either<SqliteError, SqliteDocumentReader<HK>> Cons<HK>(
        string connectionString,
        Key table)
        where HK : IEquatable<HK>, IComparable<HK> =>
    // todo: really important - this will not work because it needs to dispose / close connections. the reader and writer are not disposable anymore
        Open(new SqliteConnection(connectionString))
        .Map(conn => new SqliteDocumentReader<HK>(conn, table));

    public static Either<SqliteError, SqliteDocumentReader<HK>> Cons<HK, RK>(
        string connectionString,
        Key table)
        where HK : IEquatable<HK>, IComparable<HK>
        where RK : IEquatable<RK>, IComparable<RK> =>
    // todo: really important - this will not work because it needs to dispose / close connections. the reader and writer are not disposable anymore
        Open(new SqliteConnection(connectionString))
        .Map(conn => new SqliteDocumentReader<HK>(conn, table));

    // todo: really important - this will not work because it needs to dispose / close connections. the reader and writer are not disposable anymore
    private static Either<SqliteError, SqliteConnection> Open(SqliteConnection connection) =>
        Try.lift(connection.Open)
        .Match<Either<SqliteError, SqliteConnection>>(
            Fail: e => SqliteError.New(StorageErrorCodes.SqliteError, "failed to connect", e),
            Succ: _ => connection);
}

public sealed class SqliteDocumentReader<HK>(
    SqliteConnection connection,
    Key table)
    : IDocumentReader<HK>
    where HK : IEquatable<HK>, IComparable<HK>
{
    private readonly record struct HkParam(HK HashKey);
    private readonly string hkQuery =
        $"select a.hashkey, a.version, b.data from {table}_header a join {table}_journal b on b.id = a.id and b.version = a.version where a.hashkey = @HashKey";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Option<Document<HK>> Find(HK hashKey) =>
        connection.QuerySingle<Document<HK>>(hkQuery, new HkParam(hashKey));

    [SuppressMessage("IDisposableAnalyzers.Correctness", "IDISP007:Don't dispose injected", Justification = "reader owns the connection")]
    public void Dispose() => connection.Dispose();
}

public sealed class SqliteDocumentReader<HK, RK>(
    SqliteConnection connection,
    Key table)
    : IDocumentReader<HK, RK>
    where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>
{
    private readonly record struct HkRkParam(HK HashKey, RK RangeKey);
    private readonly string hkrkQuery =
        $"select a.hashkey, a.rangekey, a.version, b.data from {table}_header a join {table}_journal b on b.id = a.id and b.version = a.version where a.hashkey = @HashKey and a.rangekey = @RangeKey";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Option<Document<HK, RK>> Find(HK hashKey, RK rangeKey) =>
        connection.QuerySingle<Document<HK, RK>>(hkrkQuery, new HkRkParam(hashKey, rangeKey));

    private readonly record struct HkParam(HK HashKey);
    private readonly string hkQuery =
        $"select a.hashkey, a.rangekey, a.version, b.data from {table}_header a join {table}_journal b on b.id = a.id and b.version = a.version where a.hashkey = @HashKey";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Iterable<Document<HK, RK>> Where(HK hashKey, Func<Document<HK, RK>, bool> predicate) =>
        Prelude.Iterable(Find(hashKey).Where(predicate));

    [SuppressMessage("Style", "IDE0301:Simplify collection initialization", Justification = "prefer Empty here")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Iterable<Document<HK, RK>> Find(HK hashKey, RangeKey range) =>
        Filter(Find(hashKey), range);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Iterable<Document<HK, RK>> Filter(Iterable<Document<HK, RK>> documents, RangeKey range) =>
        range switch
        {
            LowerBound<RK> lower => documents.Filter(d => LowerBound(d, lower.Key)),
            UpperBound<RK> upper => documents.Filter(d => UppperBound(d, upper.Key)),
            Between<RK> span => documents.Filter(d => Between(d, span)),
            Unbound u => documents,
            _ => throw new NotSupportedException("unknown range type")
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool LowerBound(Document<HK, RK> document, RK key) =>
        document.RangeKey.CompareTo(key) >= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool UppperBound(Document<HK, RK> document, RK key) =>
        document.RangeKey.CompareTo(key) <= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Between(Document<HK, RK> document, Between<RK> span) =>
        LowerBound(document, span.LowerBound) && UppperBound(document, span.UpperBound);

    private Iterable<Document<HK, RK>> Find(HK hashKey) =>
        Prelude.Iterable(connection.Query<Document<HK, RK>>(hkQuery, new HkParam(hashKey)));
}
