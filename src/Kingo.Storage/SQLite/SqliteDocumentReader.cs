using Dapper;
using Kingo.Storage.Db;
using Kingo.Storage.Keys;
using LanguageExt;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Kingo.Storage.Sqlite;

public static class SqliteDocumentReader
{
    public static IDocumentReader<HK> WithIO<HK>(
        IDbContext context,
        Key table)
        where HK : IEquatable<HK>, IComparable<HK>
        => new SqliteDocumentReaderWithIO<HK>(context, table);

    public static IDocumentReader<HK, RK> WithIO<HK, RK>(
        IDbContext context,
        Key table)
        where HK : IEquatable<HK>, IComparable<HK>
        where RK : IEquatable<RK>, IComparable<RK>
        => new SqliteDocumentReaderWithIO<HK, RK>(context, table);

    private sealed class SqliteDocumentReaderWithIO<HK>(
        IDbContext context,
        Key table)
        : IDocumentReader<HK>
        where HK : IEquatable<HK>, IComparable<HK>
    {
        private readonly SqliteDocumentReader<HK> reader = new(context, table);

        public Eff<Option<Document<HK>>> Find(HK hashKey) =>
            Lift(token => reader.FindAsync(hashKey, token));
    }

    private sealed class SqliteDocumentReaderWithIO<HK, RK>(
        IDbContext context,
        Key table)
        : IDocumentReader<HK, RK>
        where HK : IEquatable<HK>, IComparable<HK>
        where RK : IEquatable<RK>, IComparable<RK>
    {
        private readonly SqliteDocumentReader<HK, RK> reader = new(context, table);

        public Eff<Iterable<Document<HK, RK>>> Find(HK hashKey, RangeKey range) =>
            Lift(token => reader.FindAsync(hashKey, range, token));

        public Eff<Option<Document<HK, RK>>> Find(HK hashKey, RK rangeKey) =>
            Lift(token => reader.FindAsync(hashKey, rangeKey, token));

        public Eff<Iterable<Document<HK, RK>>> Where(HK hashKey, Func<Document<HK, RK>, bool> predicate) =>
            Lift(token => reader.WhereAsync(hashKey, predicate, token));
    }

    private static Eff<T> Lift<T>(Func<CancellationToken, Task<T>> asyncOperation) =>
        Prelude.liftIO(env => asyncOperation(env.Token));
}

internal sealed class SqliteDocumentReader<HK>(
    IDbContext context,
    Key table)
    where HK : IEquatable<HK>, IComparable<HK>
{
    private readonly record struct HkParam(HK HashKey);
    private readonly string hkQuery =
        $"select a.hashkey, a.version, b.data from {table}_header a join {table}_journal b on b.id = a.id and b.version = a.version where a.hashkey = @HashKey";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<Option<Document<HK>>> FindAsync(HK hashKey, CancellationToken token) =>
        await context.ExecuteAsync((db, tx) =>
            db.QuerySingleOrDefaultAsync<Document<HK>>(hkQuery, new HkParam(hashKey), tx),
            token);
}

internal sealed class SqliteDocumentReader<HK, RK>(
    IDbContext context,
    Key table)
    where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>
{
    private readonly record struct HkRkParam(HK HashKey, RK RangeKey);
    private readonly string hkrkQuery =
        $"select a.hashkey, a.rangekey, a.version, b.data from {table}_header a join {table}_journal b on b.id = a.id and b.version = a.version where a.hashkey = @HashKey and a.rangekey = @RangeKey";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<Option<Document<HK, RK>>> FindAsync(HK hashKey, RK rangeKey, CancellationToken token) =>
        await context.ExecuteAsync((db, tx) =>
            db.QuerySingleOrDefaultAsync<Document<HK, RK>>(hkrkQuery, new HkRkParam(hashKey, rangeKey), tx),
            token);

    private readonly record struct HkParam(HK HashKey);
    private readonly string hkQuery =
        $"select a.hashkey, a.rangekey, a.version, b.data from {table}_header a join {table}_journal b on b.id = a.id and b.version = a.version where a.hashkey = @HashKey";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<Iterable<Document<HK, RK>>> WhereAsync(
        HK hashKey,
        Func<Document<HK, RK>, bool> predicate,
        CancellationToken token) =>
        Prelude.Iterable((await FindAsync(hashKey, token)).Where(predicate));

    [SuppressMessage("Style", "IDE0301:Simplify collection initialization", Justification = "prefer Empty here")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<Iterable<Document<HK, RK>>> FindAsync(HK hashKey, RangeKey range, CancellationToken token) =>
        Prelude.Iterable(Filter(await FindAsync(hashKey, token), range));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IEnumerable<Document<HK, RK>> Filter(IEnumerable<Document<HK, RK>> documents, RangeKey range) =>
        range switch
        {
            LowerBound<RK> lower => documents.Where(d => LowerBound(d, lower.Key)),
            UpperBound<RK> upper => documents.Where(d => UpperBound(d, upper.Key)),
            Between<RK> span => documents.Where(d => Between(d, span)),
            Unbound u => documents,
            _ => throw new NotSupportedException("unknown range type")
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool LowerBound(Document<HK, RK> document, RK key) =>
        document.RangeKey.CompareTo(key) >= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool UpperBound(Document<HK, RK> document, RK key) =>
        document.RangeKey.CompareTo(key) <= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Between(Document<HK, RK> document, Between<RK> span) =>
        LowerBound(document, span.LowerBound)
        && UpperBound(document, span.UpperBound);

    private Task<IEnumerable<Document<HK, RK>>> FindAsync(HK hashKey, CancellationToken token) =>
        context.ExecuteAsync((db, tx) =>
            db.QueryAsync<Document<HK, RK>>(hkQuery, new HkParam(hashKey), tx),
            token);
}
