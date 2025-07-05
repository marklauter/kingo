using Kingo.Storage.Clocks;
using Kingo.Storage.Indexing;
using Kingo.Storage.Keys;
using LanguageExt;
using LanguageExt.Common;

namespace Kingo.Storage;

public sealed class DocumentWriter(DocumentIndex index)
{
    private readonly DocumentReader reader = new(index);

    public Either<Error, Unit> Insert<R>(Document<R> document, CancellationToken cancellationToken) where R : notnull
    {
        Either<Error, Unit> Recur(Document<R> doc, CancellationToken ct) =>
            ct.IsCancellationRequested
                ? Error.New(ErrorCodes.TimeoutError, $"timeout while inserting key {doc.HashKey}/{doc.RangeKey}")
                : Try.lift(() => Insert(index.Snapshot(), doc))
                .Match(
                    Succ: success => success
                        ? Prelude.unit
                        : Recur(doc, ct),
                    Fail: _ => Error.New(ErrorCodes.DuplicateKeyError, $"duplicate key {doc.HashKey}/{doc.RangeKey}"));

        return Recur(document, cancellationToken);
    }

    private bool Insert<T>(Snapshot snapshot, Document<T> document) where T : notnull =>
        index.Exchange(snapshot, Insert(snapshot.Map, document));

    private static Snapshot Insert<R>(
        Map<Key, Map<Key, Document>> map,
        Document<R> document) where R : notnull =>
        Snapshot.From(
            map.AddOrUpdate(
                document.FullHashKey,
                map
                .Find(document.FullHashKey)
                .Match(
                    Some: innerMap => innerMap,
                    None: () => Prelude.Empty)
                .Add(document.RangeKey, document)));

    public Either<Error, Unit> InsertOrUpdate<R>(Document<R> document, CancellationToken cancellationToken) where R : notnull
    {
        Either<Error, Unit> Recur(Document<R> doc, CancellationToken ct) =>
            ct.IsCancellationRequested
                ? Error.New(ErrorCodes.TimeoutError, $"timeout while inserting/updating key {doc.HashKey}/{doc.RangeKey}")
                : reader
                    .Find<R>(doc.HashKey, doc.RangeKey)
                    .Match(
                        Some: original => CheckVersion(original, doc)
                            .Bind(_ => Update(index.Snapshot(), doc with { Version = doc.Version.Tick() })
                                ? Prelude.unit
                                : Recur(doc, ct)),
                        None: () => Try.lift(() => Insert(index.Snapshot(), doc with { Version = VersionClock.Zero }))
                            .Match(
                                Succ: success => success ? Prelude.unit : Recur(doc, ct),
                                Fail: _ => Recur(doc, ct)));

        return Recur(document, cancellationToken);
    }

    public Either<Error, Unit> Update<R>(Document<R> document, CancellationToken cancellationToken) where R : notnull
    {
        Either<Error, Unit> Recur(Document<R> doc, CancellationToken ct) =>
            ct.IsCancellationRequested
                ? Error.New(ErrorCodes.TimeoutError, $"timeout while updating key {doc.HashKey}/{doc.RangeKey}")
                : reader
                    .Find<R>(doc.HashKey, doc.RangeKey)
                    .ToEither(Error.New(ErrorCodes.NotFoundError, $"key not found {doc.HashKey}/{doc.RangeKey}"))
                    .Bind(original => CheckVersion(original, doc))
                    .Bind(_ => Update(index.Snapshot(), doc with { Version = doc.Version.Tick() })
                        ? Prelude.unit
                        : Recur(doc, ct));

        return Recur(document, cancellationToken);
    }

    private static Either<Error, Unit> CheckVersion<R>(Document<R> original, Document<R> replacement) where R : notnull =>
        original.Version == replacement.Version
            ? Prelude.unit
            : Error.New(ErrorCodes.VersionConflictError, $"version concflict {replacement.HashKey}/{replacement.RangeKey}, expected: {replacement.Version}, actual: {original.Version}");

    private bool Update<R>(Snapshot snapshot, Document<R> document) where R : notnull =>
        index.Exchange(snapshot, Update(snapshot.Map, document));

    private static Snapshot Update<R>(
        Map<Key, Map<Key, Document>> map,
        Document<R> document) where R : notnull =>
        Snapshot.From(
            map.AddOrUpdate(
                document.FullHashKey,
                map
                .Find(document.FullHashKey)
                .Match(
                    Some: innerMap => innerMap,
                    None: () => Prelude.Empty)
                .AddOrUpdate(document.RangeKey, document)));
}
