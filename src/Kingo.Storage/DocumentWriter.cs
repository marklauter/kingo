using Kingo.Storage.Clocks;
using Kingo.Storage.Indexing;
using LanguageExt;
using LanguageExt.Common;

namespace Kingo.Storage;

public sealed class DocumentWriter<HK, RK>(DocumentIndex<HK, RK> index)
    where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>
{
    private readonly DocumentReader<HK, RK> reader = new(index);

    public Either<Error, Unit> Insert(Document<HK, RK> document, CancellationToken cancellationToken)
    {
        Either<Error, Unit> Recur(Document<HK, RK> doc, CancellationToken ct) =>
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

    private bool Insert(Snapshot<HK, RK> snapshot, Document<HK, RK> document) =>
        index.Exchange(snapshot, Insert(snapshot.Map, document));

    private static Snapshot<HK, RK> Insert(
        Map<HK, Map<RK, Document<HK, RK>>> map,
        Document<HK, RK> document) =>
        Snapshot.From(
            map.AddOrUpdate(
                document.HashKey,
                map
                .Find(document.HashKey)
                .Match(
                    Some: innerMap => innerMap,
                    None: () => Prelude.Empty)
                .Add(document.RangeKey, document)));

    public Either<Error, Unit> InsertOrUpdate<R>(Document<HK, RK, R> document, CancellationToken cancellationToken) where R : notnull
    {
        Either<Error, Unit> Recur(Document<HK, RK> doc, CancellationToken ct) =>
            ct.IsCancellationRequested
                ? Error.New(ErrorCodes.TimeoutError, $"timeout while inserting/updating key {doc.HashKey}/{doc.RangeKey}")
                : reader
                    .Find<R>(doc.HashKey, doc.RangeKey)
                    .Match(
                        Some: original => CheckVersion(original, doc)
                            .Bind(_ => Update(index.Snapshot(), doc with { Version = doc.Version.Tick() })
                                ? Prelude.unit
                                : Recur(doc, ct)),
                        None: () => Try.lift(() => Insert(index.Snapshot(), doc with { Version = Revision.Zero }))
                            .Match(
                                Succ: success => success ? Prelude.unit : Recur(doc, ct),
                                Fail: _ => Recur(doc, ct)));

        return Recur(document, cancellationToken);
    }

    public Either<Error, Unit> Update<R>(Document<HK, RK, R> document, CancellationToken cancellationToken) where R : notnull
    {
        Either<Error, Unit> Recur(Document<HK, RK> doc, CancellationToken ct) =>
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

    private static Either<Error, Unit> CheckVersion(Document<HK, RK> original, Document<HK, RK> replacement) =>
        original.Version == replacement.Version
            ? Prelude.unit
            : Error.New(ErrorCodes.VersionConflictError, $"version concflict {replacement.HashKey}/{replacement.RangeKey}, expected: {replacement.Version}, actual: {original.Version}");

    private bool Update(Snapshot<HK, RK> snapshot, Document<HK, RK> document) =>
        index.Exchange(snapshot, Update(snapshot.Map, document));

    private static Snapshot<HK, RK> Update(
        Map<HK, Map<RK, Document<HK, RK>>> map,
        Document<HK, RK> document) =>
        Snapshot.From(
            map.AddOrUpdate(
                document.HashKey,
                map
                .Find(document.HashKey)
                .Match(
                    Some: innerMap => innerMap,
                    None: () => Prelude.Empty)
                .AddOrUpdate(document.RangeKey, document)));
}
