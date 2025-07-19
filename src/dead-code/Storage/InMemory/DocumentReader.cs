using dead_code.Storage.InMemory.Indexing;
using Kingo.Storage;
using Kingo.Storage.Keys;
using LanguageExt;
using System.Runtime.CompilerServices;

namespace dead_code.Storage.InMemory;

public sealed class DocumentReader<HK>(Index<HK> index)
    where HK : IEquatable<HK>, IComparable<HK>
{
    public Option<Document<HK>> Find(HK hashKey) =>
        index.Snapshot().Map.Find(hashKey);
}

public sealed class DocumentReader<HK, RK>(Index<HK, RK> index)
    where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>
{
    public Option<Document<HK, RK>> Find(HK hashKey, RK rangeKey) =>
        Find(hashKey).Bind(m => m.Find(rangeKey));

    public Iterable<Document<HK, RK>> Where(HK hashKey, Func<Document<HK, RK>, bool> predicate) =>
        Find(hashKey)
        .Map(m => m.Filter(document => predicate(document)).Values)
        .IfNone(Iterable<Document<HK, RK>>.Empty);

    public Iterable<Document<HK, RK>> Find(HK hashKey, RangeKey range) =>
        Find(hashKey)
        .Map(m => range switch
        {
            LowerBound<RK> lower => LowerBound(m, lower.Key),
            UpperBound<RK> upper => UpperBound(m, upper.Key),
            Between<RK> span => Between(m, span),
            Unbound u => m.Values,
            _ => throw new NotSupportedException("unknown range type")
        })
        .IfNone(Iterable<Document<HK, RK>>.Empty);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Iterable<Document<HK, RK>> LowerBound(Map<RK, Document<HK, RK>> map, RK key) =>
        map.Filter(document => document.RangeKey.CompareTo(key) >= 0)
        .Values;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Iterable<Document<HK, RK>> UpperBound(Map<RK, Document<HK, RK>> map, RK key) =>
        map.Filter(document => document.RangeKey.CompareTo(key) <= 0)
        .Values;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Iterable<Document<HK, RK>> Between(Map<RK, Document<HK, RK>> map, Between<RK> span) =>
        map.FindRange(span.LowerBound, span.UpperBound);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Map<HK, Map<RK, Document<HK, RK>>> Map() => index.Snapshot().Map;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Option<Map<RK, Document<HK, RK>>> Find(HK hashKey) => Map().Find(hashKey);
}
