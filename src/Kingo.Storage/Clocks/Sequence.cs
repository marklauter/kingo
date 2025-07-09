using Kingo.Storage.Keys;
using LanguageExt;
using LanguageExt.Common;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Kingo.Storage.Clocks;

public sealed class Sequence<N>(
    DocumentReader<Key> reader,
    DocumentWriter<Key> writer)
    where N : INumber<N>
{
    private static readonly Key ValueKey = Key.From("v");

    public Either<Error, N> Next(Key seqName, CancellationToken cancellationToken)
    {
        Either<Error, N> Recur(CancellationToken ct) =>
            ct.IsCancellationRequested
            ? (Either<Error, N>)Error.New(ErrorCodes.TimeoutError, $"timeout updating sequence {seqName}")
            : Write(seqName, Read(seqName) + N.One, ct)
            .Match(
                Right: n => n,
                Left: error => error.Code == ErrorCodes.VersionConflictError
                    ? Recur(ct)
                    : error);

        return Recur(cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Key ToHashKey(Key seqName) => Key.From($"seq/{seqName}");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private N Read(Key seqName) =>
        reader.Find(ToHashKey(seqName))
        .Match(
            Some: d => (N)d.Data[ValueKey],
            None: () => N.Zero);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Either<StorageError, N> Write(Key seqName, N n, CancellationToken cancellationToken) =>
        writer.InsertOrUpdate(Document.Cons(ToHashKey(seqName), Document.ConsData(ValueKey, n)), cancellationToken)
        .Map(_ => n);
}
