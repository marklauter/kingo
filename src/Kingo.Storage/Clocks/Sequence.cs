using Kingo.Storage.InMemory;
using Kingo.Storage.Keys;
using LanguageExt;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Kingo.Storage.Clocks;

public sealed class Sequence<N>(
    DocumentReader<Key> reader,
    DocumentWriter<Key> writer)
    where N : INumber<N>
{
    private static readonly Key ValueKey = Key.From("v");

    public Either<DocumentWriterError, N> Next(Key name, CancellationToken cancellationToken)
    {
        Either<DocumentWriterError, N> RepeatUntil(CancellationToken ct) =>
            ct.IsCancellationRequested
            ? DocumentWriterError.New(ErrorCodes.TimeoutError, $"timeout updating sequence {name}")
            : Write(Read(name), ct)
            .Match(
                Right: n => n,
                Left: error => error.Code == ErrorCodes.VersionConflictError
                    ? RepeatUntil(ct)
                    : error);

        return RepeatUntil(cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Key ToHashKey(Key name) => Key.From($"seq/{name}");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (Document<Key> d, N n) Read(Key name) =>
        reader.Find(ToHashKey(name))
        .Match(
            Some: d => (d, (N)d.Data[ValueKey] + N.One),
            None: () => (Document.Cons(ToHashKey(name), Document.ConsData(ValueKey, N.One)), N.One));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Either<DocumentWriterError, N> Write((Document<Key> d, N n) dn, CancellationToken cancellationToken) =>
        writer.InsertOrUpdate(dn.d with { Data = Document.ConsData(ValueKey, dn.n) }, cancellationToken)
        .Map(_ => dn.n);
}
