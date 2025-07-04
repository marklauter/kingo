using Kingo.Storage;
using Kingo.Storage.Keys;
using LanguageExt;
using LanguageExt.Common;

namespace Kingo.DictionaryEncoding;

internal class Clock(DocumentStore store)
{
    private static readonly Key ClockRangeKey = Key.From("clock");

    public Either<Error, ulong> Tick(Key clockName, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var clock = ReadClock(clockName) + 1;
            var updateStatus = WriteClock(clockName, clock, cancellationToken);

            if (updateStatus == DocumentStore.UpdateStatus.Success)
                return clock;

            if (updateStatus == DocumentStore.UpdateStatus.TimeoutError)
                return Error.New($"store timed out while writing clock {clockName}");
        }

        return Error.New($"clock timed out while writing clock {clockName}");
    }

    private static Key ToHashKey(Key clockName) => Key.From($"clock/{clockName}");

    private ulong ReadClock(Key clockName) =>
        store.Find<ulong>(ToHashKey(clockName), ClockRangeKey)
            .Match(
                Some: d => d.Record,
                None: () => 0ul);

    private DocumentStore.UpdateStatus WriteClock(Key clockName, ulong clock, CancellationToken cancellationToken) =>
        store.PutOrUpdate(Document
            .Cons(ToHashKey(clockName), ClockRangeKey, clock), cancellationToken);
}
