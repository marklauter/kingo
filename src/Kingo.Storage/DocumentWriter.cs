using Kingo.Clocks;
using Kingo.Storage.Keys;
using LanguageExt;
using LanguageExt.Common;

namespace Kingo.Storage;

public static class DocumentWriter
{
    public static Either<Error, Unit> Insert<R>(Document<R> document, CancellationToken cancellationToken) where R : notnull
    {
        static Either<Error, Unit> Recur(Document<R> doc, CancellationToken ct) =>
            ct.IsCancellationRequested
                ? Error.New(ErrorCodes.TimeoutError, $"timeout while inserting key {doc.HashKey}/{doc.RangeKey}")
                : Try.lift(() => Insert(DocumentIndex.Snapshot(), doc))
                .Match(
                    Succ: success => success
                        ? Prelude.unit
                        : Recur(doc, ct),
                    Fail: _ => Error.New(ErrorCodes.DuplicateKeyError, $"duplicate key {doc.HashKey}/{doc.RangeKey}"));

        return Recur(document, cancellationToken);
    }

    private static bool Insert<T>(DocumentIndex.Index snapshot, Document<T> document) where T : notnull =>
        DocumentIndex.Exchange(snapshot, Insert(snapshot.Map, document));

    private static DocumentIndex.Index Insert<R>(
        Map<Key, Map<Key, Document>> map,
        Document<R> document) where R : notnull =>
        DocumentIndex.Index
        .From(
            map.AddOrUpdate(
                document.FullHashKey,
                map
                .Find(document.FullHashKey)
                .Match(
                    Some: innerMap => innerMap,
                    None: () => Prelude.Empty)
                .Add(document.RangeKey, document)));

    public static Either<Error, Unit> InsertOrUpdate<R>(Document<R> document, CancellationToken cancellationToken) where R : notnull
    {
        static Either<Error, Unit> Recur(Document<R> doc, CancellationToken ct) =>
            ct.IsCancellationRequested
                ? Error.New(ErrorCodes.TimeoutError, $"timeout while inserting/updating key {doc.HashKey}/{doc.RangeKey}")
                : DocumentReader
                    .Find<R>(doc.HashKey, doc.RangeKey)
                    .Match(
                        Some: original => CheckVersion(original, doc)
                            .Bind(_ => Update(DocumentIndex.Snapshot(), doc with { Version = doc.Version.Tick() })
                                ? Prelude.unit
                                : Recur(doc, ct)),
                        None: () => Try.lift(() => Insert(DocumentIndex.Snapshot(), doc with { Version = LogicalClock.Zero }))
                            .Match(
                                Succ: success => success ? Prelude.unit : Recur(doc, ct),
                                Fail: _ => Recur(doc, ct)));

        return Recur(document, cancellationToken);
    }

    public static Either<Error, Unit> Update<R>(Document<R> document, CancellationToken cancellationToken) where R : notnull
    {
        static Either<Error, Unit> Recur(Document<R> replacement, CancellationToken ct) =>
            ct.IsCancellationRequested
                ? Error.New(ErrorCodes.TimeoutError, $"timeout while updating key {replacement.HashKey}/{replacement.RangeKey}")
                : DocumentReader
                    .Find<R>(replacement.HashKey, replacement.RangeKey)
                    .ToEither(Error.New(ErrorCodes.NotFoundError, $"key not found {replacement.HashKey}/{replacement.RangeKey}"))
                    .Bind(original => CheckVersion(original, replacement))
                    .Bind(_ => Update(DocumentIndex.Snapshot(), replacement with { Version = replacement.Version.Tick() })
                        ? Prelude.unit
                        : Recur(replacement, ct));

        return Recur(document, cancellationToken);
    }

    private static Either<Error, Unit> CheckVersion<R>(Document<R> original, Document<R> replacement) where R : notnull =>
        original.Version == replacement.Version
            ? Prelude.unit
            : Error.New(ErrorCodes.VersionConflictError, $"version concflict {replacement.HashKey}/{replacement.RangeKey}, expected: {replacement.Version}, actual: {original.Version}");

    private static bool Update<R>(DocumentIndex.Index snapshot, Document<R> document) where R : notnull =>
        DocumentIndex.Exchange(snapshot, Update(snapshot.Map, document));

    private static DocumentIndex.Index Update<R>(
        Map<Key, Map<Key, Document>> map,
        Document<R> document) where R : notnull =>
        DocumentIndex.Index
        .From(
            map.AddOrUpdate(
                document.FullHashKey,
                map
                .Find(document.FullHashKey)
                .Match(
                    Some: innerMap => innerMap,
                    None: () => Prelude.Empty)
                .AddOrUpdate(document.RangeKey, document)));
}
