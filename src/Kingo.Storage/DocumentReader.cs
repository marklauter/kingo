using Kingo.Storage.Indexing;
using Kingo.Storage.Keys;
using LanguageExt;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Kingo.Storage;

public interface IDocumentReader<HK>
    where HK : IEquatable<HK>, IComparable<HK>
{
    Option<Document<HK>> Find(HK hashKey);
}

public interface IDocumentReader1<HK, RK>
    where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>
{
    Iterable<Document<HK, RK>> Find(HK hashKey, RangeKey range);
    Option<Document<HK, RK>> Find(HK hashKey, RK rangeKey);
    Iterable<Document<HK, RK>> Where(HK hashKey, Func<Document<HK, RK>, bool> predicate);
}

public sealed class DocumentReader<HK>(Index<HK> index)
    : IDocumentReader<HK> where HK : IEquatable<HK>, IComparable<HK>
{
    public Option<Document<HK>> Find(HK hashKey) =>
        index.Snapshot().Map.Find(hashKey);
}

public sealed class DocumentReader<HK, RK>(Index<HK, RK> index)
    : IDocumentReader1<HK, RK> where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>
{
    public Option<Document<HK, RK>> Find(HK hashKey, RK rangeKey) =>
        Find(hashKey).Bind(m => m.Find(rangeKey));

    [SuppressMessage("Style", "IDE0301:Simplify collection initialization", Justification = "prefer Empty here")]
    public Iterable<Document<HK, RK>> Find(HK hashKey, RangeKey range) =>
        Find(hashKey)
        .Map(m => range switch
        {
            Since<RK> since => FindRange(m, since),
            Until<RK> until => FindRange(m, until),
            Between<RK> span => FindRange(m, span),
            Unbound u => m.Values,
            _ => throw new NotSupportedException("unknown range type")
        })
        .IfNone(Iterable<Document<HK, RK>>.Empty);

    [SuppressMessage("Style", "IDE0301:Simplify collection initialization", Justification = "prefer Empty here")]
    public Iterable<Document<HK, RK>> Where(HK hashKey, Func<Document<HK, RK>, bool> predicate) =>
        Find(hashKey)
        .Map(m => m.Filter(document => predicate(document)).Values)
        .IfNone(Iterable<Document<HK, RK>>.Empty);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Iterable<Document<HK, RK>> FindRange(Map<RK, Document<HK, RK>> map, Since<RK> since) =>
        map.Filter(document => document.RangeKey.CompareTo(since.FromKey) >= 0)
        .Values;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Iterable<Document<HK, RK>> FindRange(Map<RK, Document<HK, RK>> map, Until<RK> until) =>
        map.Filter(document => document.RangeKey.CompareTo(until.ToKey) <= 0)
        .Values;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Iterable<Document<HK, RK>> FindRange(Map<RK, Document<HK, RK>> map, Between<RK> span) =>
        map.FindRange(span.FromKey, span.ToKey);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Map<HK, Map<RK, Document<HK, RK>>> Map() => index.Snapshot().Map;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Option<Map<RK, Document<HK, RK>>> Find(HK hashKey) => Map().Find(hashKey);
}
