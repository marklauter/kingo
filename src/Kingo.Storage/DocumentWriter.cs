using Kingo.Storage.Indexing;
using LanguageExt;

namespace Kingo.Storage;

public interface IDocumentWriter<HK>
    where HK : IEquatable<HK>, IComparable<HK>
{
    Either<DocumentWriterError, Unit> Insert(Document<HK> document, CancellationToken cancellationToken);
    Either<DocumentWriterError, Unit> InsertOrUpdate(Document<HK> document, CancellationToken cancellationToken);
    Either<DocumentWriterError, Unit> Update(Document<HK> document, CancellationToken cancellationToken);
}

public sealed class DocumentWriter<HK>(Index<HK> index) : IDocumentWriter<HK> where HK : IEquatable<HK>, IComparable<HK>
{
    private readonly DocumentReader<HK> reader = new(index);

    public Either<DocumentWriterError, Unit> Insert(Document<HK> document, CancellationToken cancellationToken)
    {
        Either<DocumentWriterError, Unit> Recur(Document<HK> doc, CancellationToken ct) =>
            ct.IsCancellationRequested
                ? DocumentWriterError.New(ErrorCodes.TimeoutError, $"timeout while inserting key {doc.HashKey}")
                : Try
                .lift(() => TryExchange(index.Snapshot(), doc with { Version = Clocks.Revision.Zero }, InsertSnapshot))
                .Match(
                    Succ: success => success
                        ? Prelude.unit
                        : Recur(doc, ct),
                    Fail: e => DocumentWriterError.New(ErrorCodes.DuplicateKeyError, $"duplicate key {doc.HashKey}", e));

        return Recur(document, cancellationToken);
    }

    public Either<DocumentWriterError, Unit> InsertOrUpdate(Document<HK> document, CancellationToken cancellationToken)
    {
        Either<DocumentWriterError, Unit> Recur(Document<HK> doc, CancellationToken ct) =>
            ct.IsCancellationRequested
                ? DocumentWriterError.New(ErrorCodes.TimeoutError, $"timeout while inserting/updating key {doc.HashKey}")
                : reader
                    .Find(doc.HashKey)
                    .Match(
                        Some: original => DocumentWriter<HK>.CheckVersion(original, doc)
                            .Bind(_ => TryExchange(index.Snapshot(), doc with { Version = doc.Version.Tick() }, UpdateSnapshot)
                                ? Prelude.unit
                                : Recur(doc, ct)),
                        None: () =>
                            Try
                            .lift(() => TryExchange(index.Snapshot(), doc with { Version = Clocks.Revision.Zero }, InsertSnapshot))
                            .Match(
                                Succ: success => success ? Prelude.unit : Recur(doc, ct),
                                Fail: _ => Recur(doc, ct)));

        return Recur(document, cancellationToken);
    }

    public Either<DocumentWriterError, Unit> Update(Document<HK> document, CancellationToken cancellationToken)
    {
        Either<DocumentWriterError, Unit> Recur(Document<HK> doc, CancellationToken ct) =>
            ct.IsCancellationRequested
                ? DocumentWriterError.New(ErrorCodes.TimeoutError, $"timeout while updating key {doc.HashKey}")
                : reader
                    .Find(doc.HashKey)
                    .ToEither(DocumentWriterError.New(ErrorCodes.NotFoundError, $"key not found {doc.HashKey}"))
                    .Bind(original => CheckVersion(original, doc))
                    .Bind(_ => TryExchange(index.Snapshot(), doc with { Version = doc.Version.Tick() }, UpdateSnapshot)
                        ? Prelude.unit
                        : Recur(doc, ct));

        return Recur(document, cancellationToken);
    }

    private static Either<DocumentWriterError, Unit> CheckVersion(Document<HK> original, Document<HK> replacement) =>
        original.Version == replacement.Version
            ? Prelude.unit
            : DocumentWriterError.New(ErrorCodes.VersionConflictError, $"version conflict {replacement.HashKey}, expected: {replacement.Version}, actual: {original.Version}");

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

public interface IDocumentWriter1<HK, RK>
    where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>
{
    Either<DocumentWriterError, Unit> Insert(Document<HK, RK> document, CancellationToken cancellationToken);
    Either<DocumentWriterError, Unit> InsertOrUpdate(Document<HK, RK> document, CancellationToken cancellationToken);
    Either<DocumentWriterError, Unit> Update(Document<HK, RK> document, CancellationToken cancellationToken);
}

public sealed class DocumentWriter<HK, RK>(Index<HK, RK> index) : IDocumentWriter1<HK, RK> where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>
{
    private readonly DocumentReader<HK, RK> reader = new(index);

    public Either<DocumentWriterError, Unit> Insert(Document<HK, RK> document, CancellationToken cancellationToken)
    {
        Either<DocumentWriterError, Unit> Recur(Document<HK, RK> doc, CancellationToken ct) =>
            ct.IsCancellationRequested
                ? DocumentWriterError.New(ErrorCodes.TimeoutError, $"timeout while inserting key {doc.HashKey}/{doc.RangeKey}")
                : Try.lift(() => TryExchange(index.Snapshot(), doc with { Version = Clocks.Revision.Zero }, InsertSnapshot))
                .Match(
                    Succ: success => success
                        ? Prelude.unit
                        : Recur(doc, ct),
                    Fail: e => DocumentWriterError.New(ErrorCodes.DuplicateKeyError, $"duplicate key {doc.HashKey}/{doc.RangeKey}", e));

        return Recur(document, cancellationToken);
    }

    public Either<DocumentWriterError, Unit> InsertOrUpdate(Document<HK, RK> document, CancellationToken cancellationToken)
    {
        Either<DocumentWriterError, Unit> Recur(Document<HK, RK> doc, CancellationToken ct) =>
            ct.IsCancellationRequested
                ? DocumentWriterError.New(ErrorCodes.TimeoutError, $"timeout while inserting/updating key {doc.HashKey}/{doc.RangeKey}")
                : reader
                    .Find(doc.HashKey, doc.RangeKey)
                    .Match(
                        Some: original => DocumentWriter<HK, RK>.CheckVersion(original, doc)
                            .Bind(_ => TryExchange(index.Snapshot(), doc with { Version = doc.Version.Tick() }, UpdateSnapshot)
                                ? Prelude.unit
                                : Recur(doc, ct)),
                        None: () => Try.lift(() => TryExchange(index.Snapshot(), doc with { Version = Clocks.Revision.Zero }, InsertSnapshot))
                            .Match(
                                Succ: success => success ? Prelude.unit : Recur(doc, ct),
                                Fail: _ => Recur(doc, ct)));

        return Recur(document, cancellationToken);
    }

    public Either<DocumentWriterError, Unit> Update(Document<HK, RK> document, CancellationToken cancellationToken)
    {
        Either<DocumentWriterError, Unit> Recur(Document<HK, RK> doc, CancellationToken ct) =>
            ct.IsCancellationRequested
                ? DocumentWriterError.New(ErrorCodes.TimeoutError, $"timeout while updating key {doc.HashKey}/{doc.RangeKey}")
                : reader
                    .Find(doc.HashKey, doc.RangeKey)
                    .ToEither(DocumentWriterError.New(ErrorCodes.NotFoundError, $"key not found {doc.HashKey}/{doc.RangeKey}"))
                    .Bind(original => CheckVersion(original, doc))
                    .Bind(_ => TryExchange(index.Snapshot(), doc with { Version = doc.Version.Tick() }, UpdateSnapshot)
                        ? Prelude.unit
                        : Recur(doc, ct));

        return Recur(document, cancellationToken);
    }

    private static Either<DocumentWriterError, Unit> CheckVersion(Document<HK, RK> original, Document<HK, RK> replacement) =>
        original.Version == replacement.Version
            ? Prelude.unit
            : DocumentWriterError.New(ErrorCodes.VersionConflictError, $"version conflict {replacement.HashKey}/{replacement.RangeKey}, expected: {replacement.Version}, actual: {original.Version}");

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
