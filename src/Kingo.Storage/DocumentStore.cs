using Kingo.Clock;
using LanguageExt;

namespace Kingo.Storage;

public record Range
{
    public static Range U() => new();
    public static RangeSince Since(string rangeKey) => new(rangeKey);
    public static RangeUntil RangeUntil(string rangeKey) => new(rangeKey);
    public static RangeSpan FromTo(string fromKey, string toKey) => new(fromKey, toKey);
}

public sealed record RangeSince(string RangeKey) : Range;
public sealed record RangeUntil(string RangeKey) : Range;
public sealed record RangeSpan(string FromKey, string ToKey) : Range;

public sealed class DocumentStore
{
    // outer = hashkey (partition key), inner = rangekey (sort key)
    private Map<string, Map<string, Document>> map = [];

    public void Put<T>(string hashKey, string rangeKey, T tuple) where T : notnull =>
        map = map.AddOrUpdate(
            hashKey,
            map.Find(hashKey)
                .Match(
                    Some: x => x,
                    None: () => [])
                .Add(rangeKey, Document.New(hashKey, rangeKey, LogicalClock.Zero, tuple))); // throws on exists

    public void Update<T>(Document<T> document) where T : notnull =>
        // todo: need to check exists, then check versions are equal for optimistic concurrency and the decide to write or fail
        throw new NotImplementedException();

    public Option<Document<T>> Read<T>(string hashKey, string rangeKey) where T : notnull =>
        map.Find(hashKey).Match(
            None: () => Prelude.None,
            Some: m =>
                m.Find(rangeKey)
                .Match(
                    None: () => Prelude.None,
                    Some: d => Prelude.Some((Document<T>)d)));

    public Option<Seq<Document<T>>> Read<T>(string hashKey, Range range) where T : notnull =>
        map.Find(hashKey).Match(
            None: () => Prelude.None,
            Some: m =>
                m.Find(rangeKey)
                .Match(
                    None: () => Prelude.None,
                    Some: d => Prelude.Some((Document<T>)d)));

    public Option<Seq<Document<T>>> Query<T>(string hashKey, Func<T, bool> predicate) where T : notnull =>
        map.Find(hashKey).Match(
            None: () => Prelude.None,
            Some: m =>
                m.Find(rangeKey)
                .Match(
                    None: () => Prelude.None,
                    Some: d => Prelude.Some((Document<T>)d)));
}
