using Kingo.Clock;
using LanguageExt;

namespace Kingo.Storage;

public sealed record Error(string Message);

public sealed class DocumentStore
{
    // outer = hashkey, inner = rangekey
    private readonly Map<string, Map<string, Document>> map = [];

    public DocumentStore() { }

    private DocumentStore(Map<string, Map<string, Document>> map) => this.map = map;

    public DocumentStore Write<T>(string hashKey, string rangeKey, T tuple) where T : notnull =>
        new(Add(hashKey, rangeKey, tuple));

    private Map<string, Map<string, Document>> Add<T>(string hashKey, string rangeKey, T tuple) where T : notnull =>
        map.AddOrUpdate(
            hashKey,
            map.Find(hashKey)
                .Match(
                    Some: x => x,
                    None: () => [])
                .Add(rangeKey, Document.New(hashKey, rangeKey, LogicalClock.Zero, tuple))); // throws on exists

    public DocumentStore Write<T>(Document<T> document) where T : notnull =>
        new(Update(document));

    private Map<string, Map<string, Document>> Update<T>(Document<T> document) where T : notnull =>
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
}
