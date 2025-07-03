using Kingo.Storage.Keys;
using LanguageExt;
using System.Diagnostics.CodeAnalysis;

namespace Kingo.Storage;

public sealed class DocumentStore
{
    public static DocumentStore Empty() => new();

    private sealed record MapHolder(Map<Key, Map<Key, Document>> Map)
    {
        // outer = hashkey (partition key), inner = rangekey (sort key)
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

    public PutResponse Put<R>(Document<R> document, CancellationToken cancellationToken) where R : notnull
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (Put(mapHolder, document))
                    return PutResponse.Success;
            }
        }
        catch (ArgumentException) // document already exists
        {
            return PutResponse.DuplicateKeyError;
        }

        return PutResponse.TimeoutError;
    }

    private bool Put<T>(MapHolder snapshot, Document<T> document) where T : notnull =>
        ReferenceEquals(Interlocked.CompareExchange(ref mapHolder, Put(snapshot.Map, document), snapshot), snapshot);

    private static MapHolder Put<R>(
        Map<Key, Map<Key, Document>> map,
        Document<R> document) where R : notnull =>
        MapHolder.From(
            map.AddOrUpdate(
                document.FullHashKey,
                map
                .Find(document.FullHashKey)
                .Match(
                    Some: innerMap => innerMap,
                    None: () => Prelude.Empty)
                .Add(document.RangeKey, document)));

    public enum UpdateResponse
    {
        Success,
        NotFoundError,
        TimeoutError,
        VersionConflictError,
        Retry,
    }

    public UpdateResponse Update<R>(Document<R> document, CancellationToken cancellationToken) where R : notnull
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var response = Find<R>(document.HashKey, document.RangeKey)
                .Match(
                    Some: original => CheckVersion(original, document),
                    None: () => UpdateResponse.NotFoundError)
                .Match(
                    Some: r => r,
                    None: () => Update(mapHolder, document with { Version = document.Version.Tick() })
                    ? UpdateResponse.Success
                    : UpdateResponse.Retry
                );

            if (response is not UpdateResponse.Retry)
                return response;
        }

        return UpdateResponse.TimeoutError;
    }

    private static Option<UpdateResponse> CheckVersion<R>(Document<R> original, Document<R> document) where R : notnull =>
        original.Version == document.Version
            ? Prelude.None
            : UpdateResponse.VersionConflictError;

    private bool Update<R>(MapHolder snapshot, Document<R> document) where R : notnull =>
        ReferenceEquals(
            Interlocked.CompareExchange(
                ref mapHolder,
                Update(snapshot.Map, document),
                snapshot),
            snapshot);

    private static MapHolder Update<R>(
        Map<Key, Map<Key, Document>> map,
        Document<R> document) where R : notnull =>
        MapHolder.From(
            map.AddOrUpdate(
                document.FullHashKey,
                map
                .Find(document.FullHashKey)
                .Match(
                    Some: innerMap => innerMap,
                    None: () => Prelude.Empty)
                .AddOrUpdate(document.RangeKey, document)));

    public Option<Document<R>> Find<R>(Key hashKey, Key rangeKey) where R : notnull =>
        mapHolder.Map.Find(Document.FullHashKey<R>(hashKey))
        .Match(
            None: () => Prelude.None,
            Some: m =>
                m.Find(rangeKey)
                .Filter(document => document is Document<R>)
                .Map(document => (Document<R>)document));

    public Iterable<Document<R>> Find<R>(Key hashKey, KeyRange range) where R : notnull =>
        mapHolder.Map.Find(Document.FullHashKey<R>(hashKey))
        .Match(
            None: () => Prelude.Empty,
            Some: m => range switch
            {
                Since since => FindRange<R>(m, since),
                Until until => FindRange<R>(m, until),
                Between span => FindRange<R>(m, span),
                Unbound u => FindRange<R>(m, u),
                _ => throw new NotSupportedException("unknown range type")
            });

    private static Iterable<Document<R>> FindRange<R>(Map<Key, Document> map, Since since) where R : notnull =>
        map.Filter(document =>
            document is Document<R> documentT
            && documentT.RangeKey >= since.FromKey)
        .Values.Map(document => (Document<R>)document);

    private static Iterable<Document<R>> FindRange<R>(Map<Key, Document> map, Until until) where R : notnull =>
        map.Filter(document =>
            document is Document<R> documentT
            && documentT.RangeKey <= until.ToKey)
        .Values.Map(document => (Document<R>)document);

    private static Iterable<Document<R>> FindRange<R>(Map<Key, Document> map, Between span) where R : notnull =>
        map.FindRange(span.FromKey, span.ToKey)
        .Filter(document => document is Document<R>)
        .Map(document => (Document<R>)document);

    private static Iterable<Document<R>> FindRange<R>(Map<Key, Document> map, Unbound _) where R : notnull =>
        map.Filter(document => document is Document<R>)
        .Values.Map(document => (Document<R>)document);

    [SuppressMessage("Style", "IDE0301:Simplify collection initialization", Justification = "I prefer explicit empty here")]
    public Iterable<Document<R>> Where<R>(Key hashKey, Func<Document<R>, bool> predicate) where R : notnull =>
        mapHolder.Map.Find(Document.FullHashKey<R>(hashKey))
        .Match(
            None: () => Iterable<Document<R>>.Empty,
            Some: m =>
                m.Filter(document =>
                    document is Document<R> documentT
                    && predicate(documentT))
                .Values
                .Map(document => (Document<R>)document));
}
