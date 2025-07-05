using Kingo.Storage.Keys;
using LanguageExt;
using System.Diagnostics.CodeAnalysis;

namespace Kingo.Storage;

public static class DocumentReader
{
    public static Option<Document<R>> Find<R>(Key hashKey, Key rangeKey) where R : notnull =>
        DocumentIndex
        .Snapshot()
        .Map
        .Find(Document.FullHashKey<R>(hashKey))
        .Match(
            None: () => Prelude.None,
            Some: m =>
                m.Find(rangeKey)
                .Filter(document => document is Document<R>)
                .Map(document => (Document<R>)document));

    public static Iterable<Document<R>> Find<R>(Key hashKey, KeyRange range) where R : notnull =>
        DocumentIndex
        .Snapshot()
        .Map.Find(Document.FullHashKey<R>(hashKey))
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

    [SuppressMessage("Style", "IDE0301:Simplify collection initialization", Justification = "I prefer explicit empty here")]
    public static Iterable<Document<R>> Where<R>(Key hashKey, Func<Document<R>, bool> predicate) where R : notnull =>
        DocumentIndex
        .Snapshot()
        .Map
        .Find(Document.FullHashKey<R>(hashKey))
        .Match(
            None: () => Iterable<Document<R>>.Empty,
            Some: m =>
                m.Filter(document =>
                    document is Document<R> documentT
                    && predicate(documentT))
                .Values
                .Map(document => (Document<R>)document));

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
}
