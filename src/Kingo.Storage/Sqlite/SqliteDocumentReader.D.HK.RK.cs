using Dapper;
using Kingo.Storage.Db;
using Kingo.Storage.Keys;
using LanguageExt;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Kingo.Storage.Sqlite;

internal sealed class SqliteDocumentReader<D, HK, RK>(
    IDbContext context)
    where D : Document<HK, RK>
    where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>
{
    private readonly record struct HkRkParam(HK HashKey, RK RangeKey);
    private static readonly string HkRkQuery =
        $"select b.* from {DocumentTypeCache<D>.Name}_header a join {DocumentTypeCache<D>.Name}_journal b on b.hashkey = a.hashkey and a.rangekey = b.rangekey and b.version = a.version where a.hashkey = @HashKey and a.rangekey = @RangeKey";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<Option<D>> FindAsync(HK hashKey, RK rangeKey, CancellationToken token) =>
        await context.ExecuteAsync((db, tx) =>
            db.QuerySingleOrDefaultAsync<D>(HkRkQuery, new HkRkParam(hashKey, rangeKey), tx),
            token);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<Iterable<D>> WhereAsync(
        HK hashKey,
        Func<D, bool> predicate,
        CancellationToken token) =>
        Prelude.Iterable((await FindAsync(hashKey, token)).Where(predicate));

    [SuppressMessage("Style", "IDE0301:Simplify collection initialization", Justification = "prefer Empty here")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<Iterable<D>> FindAsync(HK hashKey, RangeKeyCondition range, CancellationToken token) =>
        Prelude.Iterable(Filter(await FindAsync(hashKey, token), range));

    private readonly record struct HkParam(HK HashKey);
    private static readonly string HkQuery =
        $"select b.* from {DocumentTypeCache<D>.Name}_header a join {DocumentTypeCache<D>.Name}_journal b on b.hashkey = a.hashkey and b.rangekey = a.rangekey and b.version = a.version where a.hashkey = @HashKey";

    private Task<IEnumerable<D>> FindAsync(HK hashKey, CancellationToken token) =>
        context.ExecuteAsync((db, tx) =>
            db.QueryAsync<D>(HkQuery, new HkParam(hashKey), tx),
            token);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IEnumerable<D> Filter(IEnumerable<D> documents, RangeKeyCondition range) =>
        range switch
        {
            LowerBound<RK> lower => documents.Where(d => LowerBound(d, lower.Key)),
            Conditions<RK> upper => documents.Where(d => UpperBound(d, upper.Key)),
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
}
