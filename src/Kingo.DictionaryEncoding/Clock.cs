using Kingo.Storage;
using Kingo.Storage.Keys;
using LanguageExt;
using LanguageExt.Common;

namespace Kingo.DictionaryEncoding;

internal class Clock(
    DocumentReader reader,
    DocumentWriter writer)
{
    private static readonly Key ClockRangeKey = Key.From("clock");

    public Either<Error, ulong> Tick(Key clockName, CancellationToken cancellationToken)
    {
        Either<Error, ulong> Recur(CancellationToken ct) =>
            ct.IsCancellationRequested
                ? (Either<Error, ulong>)Error.New(ErrorCodes.TimeoutError, $"Timeout updating clock {clockName}")
                : WriteClock(clockName, ReadClock(clockName) + 1, ct)
                .Match(
                    Right: clock => clock,
                    Left: error => error.Code == ErrorCodes.VersionConflictError
                        ? Recur(ct)
                        : error);

        return Recur(cancellationToken);
    }

    private static Key ToHashKey(Key clockName) => Key.From($"clock/{clockName}");

    private ulong ReadClock(Key clockName) =>
        reader.Find<ulong>(ToHashKey(clockName), ClockRangeKey)
            .Match(
                Some: d => d.Record,
                None: () => 0ul);

    private Either<Error, ulong> WriteClock(Key clockName, ulong clock, CancellationToken cancellationToken) =>
        writer.InsertOrUpdate(Document.Cons(ToHashKey(clockName), ClockRangeKey, clock), cancellationToken)
        .Map(_ => clock);
}
