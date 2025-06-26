using Kingo.Storage.Ranges;
using LanguageExt;

namespace Kingo.Storage;

public sealed class DocumentStore
{
    // outer = hashkey (partition key), inner = rangekey (sort key)
    private sealed record MapHolder(Map<string, Map<string, Document>> Map)
    {
        public static MapHolder Empty = new(Prelude.Empty);
        public static MapHolder From(Map<string, Map<string, Document>> map) => new(map);
    }

    private MapHolder mapHolder = MapHolder.Empty;

    public bool TryPut<T>(Document<T> document, CancellationToken cancellationToken) where T : notnull
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (TryPut(mapHolder, document))
                    return true;
            }
        }
        catch (ArgumentException) // document already exists
        {
            return false;
        }

        return false;
    }

    private bool TryPut<T>(MapHolder snapshot, Document<T> document) where T : notnull =>
        ReferenceEquals(Interlocked.CompareExchange(ref mapHolder, Put(snapshot.Map, document), snapshot), snapshot);

    private static MapHolder Put<T>(
        Map<string, Map<string, Document>> map,
        Document<T> document) where T : notnull =>
        MapHolder.From(
            map.AddOrUpdate(
                document.HashKey,
                map
                .Find(document.HashKey)
                .Match(
                    Some: innerMap => innerMap,
                    None: () => Prelude.Empty)
                .Add(document.RangeKey, document)));

    // todo: need to check exists, then check versions are equal for optimistic concurrency and the decide to write or fail
    public bool TryUpdate<T>(Document<T> document, CancellationToken cancellationToken) where T : notnull
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!Read<T>(document.HashKey, document.RangeKey)
                .Exists(d => d.Version == document.Version))
                return false;

            if (TryUpdate(mapHolder, document with { Version = document.Version.Tick() }))
                return true;
        }

        return false;
    }

    private bool TryUpdate<T>(MapHolder snapshot, Document<T> document) where T : notnull =>
        ReferenceEquals(Interlocked.CompareExchange(ref mapHolder, Update(snapshot.Map, document), snapshot), snapshot);

    private static MapHolder Update<T>(
        Map<string, Map<string, Document>> map,
        Document<T> document) where T : notnull =>
        MapHolder.From(
            map.AddOrUpdate(
                document.HashKey,
                map
                .Find(document.HashKey)
                .Match(
                    Some: innerMap => innerMap,
                    None: () => Prelude.Empty)
                .AddOrUpdate(document.RangeKey, document)));

    public Option<Document<T>> Read<T>(string hashKey, string rangeKey) where T : notnull =>
        mapHolder.Map.Find(hashKey)
        .Match(
            None: () => Prelude.None,
            Some: m =>
                m.Find(rangeKey)
                .Filter(document => document is Document<T>)
                .Map(document => (Document<T>)document));

    public Iterable<Document<T>> Read<T>(string hashKey, UnboundRange range) where T : notnull =>
        mapHolder.Map.Find(hashKey)
        .Match(
            None: () => Prelude.Empty,
            Some: m => range switch
            {
                RangeSince since => Read<T>(m, since),
                RangeUntil until => Read<T>(m, until),
                RangeSpan span => Read<T>(m, span),
                UnboundRange u => Read<T>(m, u),
                _ => throw new NotSupportedException("unknown range type")
            });

    private static Iterable<Document<T>> Read<T>(Map<string, Document> map, RangeSince since) where T : notnull =>
        map.Filter(document =>
            document is Document<T> documentT
            && string.CompareOrdinal(documentT.RangeKey, since.RangeKey) >= 0)
        .Values.Map(document => (Document<T>)document);

    private static Iterable<Document<T>> Read<T>(Map<string, Document> map, RangeUntil until) where T : notnull =>
        map.Filter(document =>
            document is Document<T> documentT
            && string.CompareOrdinal(documentT.RangeKey, until.RangeKey) <= 0)
        .Values.Map(document => (Document<T>)document);

    private static Iterable<Document<T>> Read<T>(Map<string, Document> map, RangeSpan span) where T : notnull =>
        map.FindRange(span.FromKey, span.ToKey)
        .Filter(document => document is Document<T>)
        .Map(document => (Document<T>)document);

    private static Iterable<Document<T>> Read<T>(Map<string, Document> map, UnboundRange _) where T : notnull =>
        map.Filter(document => document is Document<T>)
        .Values.Map(document => (Document<T>)document);

    public Iterable<Document<T>> Where<T>(string hashKey, Func<Document<T>, bool> predicate) where T : notnull =>
        mapHolder.Map.Find(hashKey)
        .Match(
            None: () => Iterable<Document<T>>.Empty,
            Some: m =>
                m.Filter(document =>
                    document is Document<T> documentT
                    && predicate(documentT))
                .Values
                .Map(document => (Document<T>)document));
}
