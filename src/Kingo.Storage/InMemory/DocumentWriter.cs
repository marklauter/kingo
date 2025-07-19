using Kingo.Storage.InMemory.Indexing;
using LanguageExt;
using LanguageExt.Common;

namespace Kingo.Storage.InMemory;

public sealed class DocumentWriter<HK>(Index<HK> index)
    where HK : IEquatable<HK>, IComparable<HK>
{
    private readonly DocumentReader<HK> reader = new(index);

    public Eff<Unit> Insert(Document<HK> document, CancellationToken cancellationToken)
    {
        Eff<Unit> RepeatUntil(Document<HK> doc, CancellationToken ct) =>
            ct.IsCancellationRequested
                ? Errors.Cancelled
                : Try
                .lift(() => TryExchange(index.Snapshot(), doc with { Version = Revision.Zero }, InsertSnapshot))
                .Match(
                    Succ: success => success
                        ? Prelude.Pure(Prelude.unit)
                        : RepeatUntil(doc, ct),
                    Fail: e => DocumentWriterError.New(StorageErrorCodes.DuplicateKeyError, $"duplicate key {doc.HashKey}", e));

        return RepeatUntil(document, cancellationToken);
    }

    public Eff<Unit> InsertOrUpdate(Document<HK> document) => throw new NotImplementedException();
    public Eff<Unit> InsertOrUpdate(Document<HK> document, CancellationToken cancellationToken)
    {
        Eff<Unit> RepeatUntil(Document<HK> doc, CancellationToken ct) =>
            ct.IsCancellationRequested
                ? Errors.Cancelled
                : reader
                    .Find(doc.HashKey)
                    .Match(
                        Some: original => DocumentWriter<HK>.CheckVersion(original, doc)
                            .Bind(_ => TryExchange(index.Snapshot(), doc with { Version = doc.Version.Tick() }, UpdateSnapshot)
                                ? Prelude.Pure(Prelude.unit)
                                : RepeatUntil(doc, ct)),
                        None: () =>
                            Try
                            .lift(() => TryExchange(index.Snapshot(), doc with { Version = Revision.Zero }, InsertSnapshot))
                            .Match(
                                Succ: success => success
                                    ? Prelude.Pure(Prelude.unit)
                                    : RepeatUntil(doc, ct),
                                Fail: _ => RepeatUntil(doc, ct)));

        return RepeatUntil(document, cancellationToken);
    }

    public Eff<Unit> Update(Document<HK> document) => throw new NotImplementedException();
    public Eff<Unit> Update(Document<HK> document, CancellationToken cancellationToken)
    {
        Eff<Unit> RepeatUntil(Document<HK> doc, CancellationToken ct) =>
            ct.IsCancellationRequested
                ? Errors.Cancelled
                : reader
                    .Find(doc.HashKey)
                    .ToEff(DocumentWriterError.New(StorageErrorCodes.NotFoundError, $"key not found {doc.HashKey}"))
                    .Bind(original => CheckVersion(original, doc))
                    .Bind(_ => TryExchange(index.Snapshot(), doc with { Version = doc.Version.Tick() }, UpdateSnapshot)
                        ? Prelude.Pure(Prelude.unit)
                        : RepeatUntil(doc, ct));

        return RepeatUntil(document, cancellationToken);
    }

    private static Eff<Unit> CheckVersion(Document<HK> original, Document<HK> replacement) =>
        original.Version == replacement.Version
            ? Prelude.Pure(Prelude.unit)
            : DocumentWriterError.New(StorageErrorCodes.VersionConflictError, $"version conflict {replacement.HashKey}, expected: {replacement.Version}, actual: {original.Version}");

    private static Snapshot<HK> InsertSnapshot(
        Map<HK, Document<HK>> map,
        Document<HK> document) =>
        Snapshot.Cons(map.Add(document.HashKey, document));

    private static Snapshot<HK> UpdateSnapshot(
        Map<HK, Document<HK>> map,
        Document<HK> document) =>
        Snapshot.Cons(map.AddOrUpdate(document.HashKey, document));

    private bool TryExchange(
        Snapshot<HK> snapshot,
        Document<HK> document,
        Func<Map<HK, Document<HK>>, Document<HK>, Snapshot<HK>> operation) =>
        index.Exchange(snapshot, operation(snapshot.Map, document));
}

public sealed class DocumentWriter<HK, RK>(Index<HK, RK> index)
    where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>
{
    private readonly DocumentReader<HK, RK> reader = new(index);

    public Eff<Unit> Insert(Document<HK, RK> document, CancellationToken cancellationToken)
    {
        Eff<Unit> RepeatUntil(Document<HK, RK> doc, CancellationToken ct) =>
            ct.IsCancellationRequested
                ? Errors.Cancelled
                : Try.lift(() => TryExchange(index.Snapshot(), doc with { Version = Revision.Zero }, InsertSnapshot))
                .Match(
                    Succ: success => success
                        ? Prelude.Pure(Prelude.unit)
                        : RepeatUntil(doc, ct),
                    Fail: e => DocumentWriterError.New(StorageErrorCodes.DuplicateKeyError, $"duplicate key {doc.HashKey}/{doc.RangeKey}", e));

        return RepeatUntil(document, cancellationToken);
    }

    public Eff<Unit> InsertOrUpdate(Document<HK, RK> document, CancellationToken cancellationToken)
    {
        Eff<Unit> RepeatUntil(Document<HK, RK> doc, CancellationToken ct) =>
            ct.IsCancellationRequested
                ? Errors.Cancelled
                : reader
                    .Find(doc.HashKey, doc.RangeKey)
                    .Match(
                        Some: original => DocumentWriter<HK, RK>.CheckVersion(original, doc)
                            .Bind(_ => TryExchange(index.Snapshot(), doc with { Version = doc.Version.Tick() }, UpdateSnapshot)
                                ? Prelude.Pure(Prelude.unit)
                                : RepeatUntil(doc, ct)),
                        None: () => Try.lift(() => TryExchange(index.Snapshot(), doc with { Version = Revision.Zero }, InsertSnapshot))
                            .Match(
                                Succ: success => success
                                    ? Prelude.Pure(Prelude.unit)
                                    : RepeatUntil(doc, ct),
                                Fail: _ => RepeatUntil(doc, ct)));

        return RepeatUntil(document, cancellationToken);
    }

    public Eff<Unit> Update(Document<HK, RK> document, CancellationToken cancellationToken)
    {
        Eff<Unit> RepeatUntil(Document<HK, RK> doc, CancellationToken ct) =>
            ct.IsCancellationRequested
                ? Errors.Cancelled
                : reader
                    .Find(doc.HashKey, doc.RangeKey)
                    .ToEff(DocumentWriterError.New(StorageErrorCodes.NotFoundError, $"key not found {doc.HashKey}/{doc.RangeKey}"))
                    .Bind(original => CheckVersion(original, doc))
                    .Bind(_ => TryExchange(index.Snapshot(), doc with { Version = doc.Version.Tick() }, UpdateSnapshot)
                        ? Prelude.Pure(Prelude.unit)
                        : RepeatUntil(doc, ct));

        return RepeatUntil(document, cancellationToken);
    }

    private static Eff<Unit> CheckVersion(Document<HK, RK> original, Document<HK, RK> replacement) =>
        original.Version == replacement.Version
            ? Prelude.Pure(Prelude.unit)
            : DocumentWriterError.New(StorageErrorCodes.VersionConflictError, $"version conflict {replacement.HashKey}/{replacement.RangeKey}, expected: {replacement.Version}, actual: {original.Version}");

    private static Snapshot<HK, RK> InsertSnapshot(
        Map<HK, Map<RK, Document<HK, RK>>> map,
        Document<HK, RK> document) =>
        Snapshot.Cons(
            map.AddOrUpdate(
                document.HashKey,
                map
                .Find(document.HashKey)
                .Match(
                    Some: innerMap => innerMap,
                    None: () => Prelude.Empty)
                .Add(document.RangeKey, document)));

    private static Snapshot<HK, RK> UpdateSnapshot(
        Map<HK, Map<RK, Document<HK, RK>>> map,
        Document<HK, RK> document) =>
        Snapshot.Cons(
            map.AddOrUpdate(
                document.HashKey,
                map
                .Find(document.HashKey)
                .Match(
                    Some: innerMap => innerMap,
                    None: () => Prelude.Empty)
                .AddOrUpdate(document.RangeKey, document)));

    private bool TryExchange(
        Snapshot<HK, RK> snapshot,
        Document<HK, RK> document,
        Func<Map<HK, Map<RK, Document<HK, RK>>>, Document<HK, RK>, Snapshot<HK, RK>> operation) =>
        index.Exchange(snapshot, operation(snapshot.Map, document));
}
