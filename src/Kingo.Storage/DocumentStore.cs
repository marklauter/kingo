using Kingo.Primitives;
using Kingo.Storage.Ranges;
using LanguageExt;
using System.Diagnostics.CodeAnalysis;

namespace Kingo.Storage;

public sealed class DocumentStore
{
    public static DocumentStore Empty() => new();

    // outer = hashkey (partition key), inner = rangekey (sort key)
    private sealed record MapHolder(Map<Key, Map<Key, Document>> Map)
    {
        public static MapHolder Empty = new(Prelude.Empty);
        public static MapHolder From(Map<Key, Map<Key, Document>> map) => new(map);
    }

    private MapHolder mapHolder = MapHolder.Empty;

    public enum PutResponse
    {
        Success,
        TimeoutError,
        DuplicateKeyError,
    }

    public PutResponse TryPut<T>(Document<T> document, CancellationToken cancellationToken) where T : notnull
    {
        try
        {
            do
            {
                if (TryPut(mapHolder, document))
                    return PutResponse.Success;
            }
            while (!cancellationToken.IsCancellationRequested);
        }
        catch (ArgumentException) // document already exists
        {
            return PutResponse.DuplicateKeyError;
        }

        return PutResponse.TimeoutError;
    }

    private bool TryPut<T>(MapHolder snapshot, Document<T> document) where T : notnull =>
        ReferenceEquals(Interlocked.CompareExchange(ref mapHolder, Put(snapshot.Map, document), snapshot), snapshot);

    private static MapHolder Put<T>(
        Map<Key, Map<Key, Document>> map,
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

    public enum UpdateResponse
    {
        Success,
        TimeoutError,
        VersionCheckFailedError,
    }

    public UpdateResponse TryUpdate<T>(Document<T> document, CancellationToken cancellationToken) where T : notnull
    {
        do
        {
            if (HasVersionConflict(document))
                return UpdateResponse.VersionCheckFailedError;

            if (TryUpdate(mapHolder, document with { Version = document.Version.Tick() }))
                return UpdateResponse.Success;
        }
        while (!cancellationToken.IsCancellationRequested);

        return UpdateResponse.TimeoutError;
    }

    private bool HasVersionConflict<T>(Document<T> document) where T : notnull =>
        Find<T>(document.HashKey, document.RangeKey)
        .Exists(d => d.Version != document.Version);

    private bool TryUpdate<T>(MapHolder snapshot, Document<T> document) where T : notnull =>
        ReferenceEquals(Interlocked.CompareExchange(ref mapHolder, Update(snapshot.Map, document), snapshot), snapshot);

    private static MapHolder Update<T>(
        Map<Key, Map<Key, Document>> map,
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

    public Option<Document<T>> Find<T>(Key hashKey, Key rangeKey) where T : notnull =>
        mapHolder.Map.Find(hashKey)
        .Match(
            None: () => Prelude.None,
            Some: m =>
                m.Find(rangeKey)
                .Filter(document => document is Document<T>)
                .Map(document => (Document<T>)document));

    public Iterable<Document<T>> FindRange<T>(Key hashKey, KeyRange range) where T : notnull =>
        mapHolder.Map.Find(hashKey)
        .Match(
            None: () => Prelude.Empty,
            Some: m => range switch
            {
                Since since => FindRange<T>(m, since),
                Until until => FindRange<T>(m, until),
                Between span => FindRange<T>(m, span),
                Unbound u => FindRange<T>(m, u),
                _ => throw new NotSupportedException("unknown range type")
            });

    private static Iterable<Document<T>> FindRange<T>(Map<Key, Document> map, Since since) where T : notnull =>
        map.Filter(document =>
            document is Document<T> documentT
            && documentT.RangeKey >= since.RangeKey)
        .Values.Map(document => (Document<T>)document);

    private static Iterable<Document<T>> FindRange<T>(Map<Key, Document> map, Until until) where T : notnull =>
        map.Filter(document =>
            document is Document<T> documentT
            && documentT.RangeKey <= until.RangeKey)
        .Values.Map(document => (Document<T>)document);

    private static Iterable<Document<T>> FindRange<T>(Map<Key, Document> map, Between span) where T : notnull =>
        map.FindRange(span.FromKey, span.ToKey)
        .Filter(document => document is Document<T>)
        .Map(document => (Document<T>)document);

    private static Iterable<Document<T>> FindRange<T>(Map<Key, Document> map, Unbound _) where T : notnull =>
        map.Filter(document => document is Document<T>)
        .Values.Map(document => (Document<T>)document);

    [SuppressMessage("Style", "IDE0301:Simplify collection initialization", Justification = "I prefer explicit empty here")]
    public Iterable<Document<T>> Where<T>(Key hashKey, Func<Document<T>, bool> predicate) where T : notnull =>
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
