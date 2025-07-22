using Dapper;
using Kingo.Storage.Db;
using Kingo.Storage.Keys;
using LanguageExt;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Kingo.Storage.Sqlite;

public static class SqliteDocumentReader
{
    public static IDocumentReader<D, HK> WithIO<D, HK>(
        IDbContext context)
        where D : Document<HK>
        where HK : IEquatable<HK>, IComparable<HK>
        => new SqliteDocumentReaderWithIO<D, HK>(context);

    public static IDocumentReader<D, HK, RK> WithIO<D, HK, RK>(
        IDbContext context)
        where D : Document<HK, RK>
        where HK : IEquatable<HK>, IComparable<HK>
        where RK : IEquatable<RK>, IComparable<RK>
        => new SqliteDocumentReaderWithIO<D, HK, RK>(context);

    private sealed class SqliteDocumentReaderWithIO<D, HK>(
        IDbContext context)
        : IDocumentReader<D, HK>
        where D : Document<HK>
        where HK : IEquatable<HK>, IComparable<HK>
    {
        private readonly SqliteDocumentReader<D, HK> reader = new(context);

        public Eff<Option<D>> Find(HK hashKey) =>
            Lift(token => reader.FindAsync(hashKey, token));
    }

    private sealed class SqliteDocumentReaderWithIO<D, HK, RK>(
        IDbContext context)
        : IDocumentReader<D, HK, RK>
        where D : Document<HK, RK>
        where HK : IEquatable<HK>, IComparable<HK>
        where RK : IEquatable<RK>, IComparable<RK>
    {
        private readonly SqliteDocumentReader<D, HK, RK> reader = new(context);

        public Eff<Iterable<D>> Find(HK hashKey, RangeKey range) =>
            Lift(token => reader.FindAsync(hashKey, range, token));

        public Eff<Option<D>> Find(HK hashKey, RK rangeKey) =>
            Lift(token => reader.FindAsync(hashKey, rangeKey, token));

        public Eff<Iterable<D>> Where(HK hashKey, Func<D, bool> predicate) =>
            Lift(token => reader.WhereAsync(hashKey, predicate, token));
    }

    private static Eff<T> Lift<T>(Func<CancellationToken, Task<T>> asyncOperation) =>
        Prelude.liftIO(env => asyncOperation(env.Token));
}

internal sealed class SqliteDocumentReader<D, HK>(
    IDbContext context)
    where D : Document<HK>
    where HK : IEquatable<HK>, IComparable<HK>
{
    private readonly record struct HkParam(HK HashKey);
    private static readonly string FindStatement =
        $"select b.* from {DocumentTypeCache<D>.TypeName}_header a join {DocumentTypeCache<D>.TypeName}_journal b on b.hashkey = a.hashkey and b.version = a.version where a.hashkey = @HashKey";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<Option<D>> FindAsync(HK hashKey, CancellationToken token) =>
        await context.ExecuteAsync((db, tx) =>
            db.QuerySingleOrDefaultAsync<D>(FindStatement, new HkParam(hashKey), tx),
            token);
}

internal sealed class SqliteDocumentReader<D, HK, RK>(
    IDbContext context)
    where D : Document<HK, RK>
    where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>
{
    private readonly record struct HkRkParam(HK HashKey, RK RangeKey);
    private static readonly string HkRkQuery =
        $"select b.* from {DocumentTypeCache<D>.TypeName}_header a join {DocumentTypeCache<D>.TypeName}_journal b on b.hashkey = a.hashkey and b.version = a.version where a.hashkey = @HashKey and a.rangekey = @RangeKey";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<Option<D>> FindAsync(HK hashKey, RK rangeKey, CancellationToken token) =>
        await context.ExecuteAsync((db, tx) =>
            db.QuerySingleOrDefaultAsync<D>(HkRkQuery, new HkRkParam(hashKey, rangeKey), tx),
            token);

    private readonly record struct HkParam(HK HashKey);
    private static readonly string HkQuery =
        $"select b.* from {DocumentTypeCache<D>.TypeName}_header a join {DocumentTypeCache<D>.TypeName}_journal b on b.hashkey = a.hashkey and b.version = a.version where a.hashkey = @HashKey";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<Iterable<D>> WhereAsync(
        HK hashKey,
        Func<D, bool> predicate,
        CancellationToken token) =>
        Prelude.Iterable((await FindAsync(hashKey, token)).Where(predicate));

    [SuppressMessage("Style", "IDE0301:Simplify collection initialization", Justification = "prefer Empty here")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<Iterable<D>> FindAsync(HK hashKey, RangeKey range, CancellationToken token) =>
        Prelude.Iterable(Filter(await FindAsync(hashKey, token), range));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IEnumerable<D> Filter(IEnumerable<D> documents, RangeKey range) =>
        range switch
        {
            LowerBound<RK> lower => documents.Where(d => LowerBound(d, lower.Key)),
            UpperBound<RK> upper => documents.Where(d => UpperBound(d, upper.Key)),
            Between<RK> span => documents.Where(d => Between(d, span)),
            Unbound u => documents,
            _ => throw new NotSupportedException("unknown range type")
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool LowerBound(D document, RK key) =>
        document.RangeKey.CompareTo(key) >= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool UpperBound(D document, RK key) =>
        document.RangeKey.CompareTo(key) <= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Between(D document, Between<RK> span) =>
        LowerBound(document, span.LowerBound)
        && UpperBound(document, span.UpperBound);

    private Task<IEnumerable<D>> FindAsync(HK hashKey, CancellationToken token) =>
        context.ExecuteAsync((db, tx) =>
            db.QueryAsync<D>(HkQuery, new HkParam(hashKey), tx),
            token);
}
