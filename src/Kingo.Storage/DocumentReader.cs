using Kingo.Storage.Indexing;
using Kingo.Storage.Keys;
using LanguageExt;
using System.Diagnostics.CodeAnalysis;

namespace Kingo.Storage;

public class DocumentReader<HK, RK>(DocumentIndex<HK, RK> index)
    where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>
{
    public Option<Document<HK, RK, R>> Find<R>(HK hashKey, RK rangeKey) where R : notnull =>
        index.Snapshot().Map.Find(hashKey)
        .Match(
            None: () => Prelude.None,
            Some: m =>
                m.Find(rangeKey)
                .Filter(document => document is Document<HK, RK, R>)
                .Map(document => (Document<HK, RK, R>)document));

    public Iterable<Document<HK, RK, R>> Find<R>(HK hashKey, Keys.RangeKey range) where R : notnull =>
        index.Snapshot().Map.Find(hashKey)
        .Match(
            None: () => Prelude.Empty,
            Some: m => range switch
            {
                Since<RK> since => FindRange<R>(m, since),
                Until<RK> until => FindRange<R>(m, until),
                Between<RK> span => FindRange<R>(m, span),
                Unbound u => FindRange<R>(m, u),
                _ => throw new NotSupportedException("unknown range type")
            });

    [SuppressMessage("Style", "IDE0301:Simplify collection initialization", Justification = "I prefer explicit empty here")]
    public Iterable<Document<HK, RK, R>> Where<R>(HK hashKey, Func<Document<HK, RK, R>, bool> predicate) where R : notnull =>
        index.Snapshot().Map.Find(hashKey)
        .Match(
            None: () => Iterable<Document<HK, RK, R>>.Empty,
            Some: m =>
                m.Filter(document =>
                    document is Document<HK, RK, R> documentT
                    && predicate(documentT))
                .Values
                .Map(document => (Document<HK, RK, R>)document));

    private static Iterable<Document<HK, RK, R>> FindRange<R>(Map<RK, Document<HK, RK>> map, Since<RK> since) where R : notnull =>
        map.Filter(document =>
            document is Document<HK, RK, R> documentT
            && documentT.RangeKey.CompareTo(since.FromKey) >= 0)
        .Values.Map(document => (Document<HK, RK, R>)document);

    private static Iterable<Document<HK, RK, R>> FindRange<R>(Map<RK, Document<HK, RK>> map, Until<RK> until) where R : notnull =>
        map.Filter(document =>
            document is Document<HK, RK, R> documentT
            && documentT.RangeKey.CompareTo(until.ToKey) <= 0)
        .Values.Map(document => (Document<HK, RK, R>)document);

    private static Iterable<Document<HK, RK, R>> FindRange<R>(Map<RK, Document<HK, RK>> map, Between<RK> span) where R : notnull =>
        map.FindRange(span.FromKey, span.ToKey)
        .Filter(document => document is Document<HK, RK, R>)
        .Map(document => (Document<HK, RK, R>)document);

    private static Iterable<Document<HK, RK, R>> FindRange<R>(Map<RK, Document<HK, RK>> map, Unbound _) where R : notnull =>
        map.Filter(document => document is Document<HK, RK, R>)
        .Values.Map(document => (Document<HK, RK, R>)document);
}
