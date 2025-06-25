using LanguageExt;

namespace Kingo.Storage;

public sealed class DocumentStore
{
    // outer = hashkey, inner = rangekey
    private readonly Map<string, Map<string, Document>> map = [];

    public DocumentStore() { }

    private DocumentStore(Map<string, Map<string, Document>> map) => this.map = map;

    public DocumentStore Union(Document document) => new(AddOrUpdate(document));

    private Map<string, Map<string, Document>> AddOrUpdate(Document document) =>
        map.AddOrUpdate(
            document.HashKey,
            map.Find(document.HashKey)
                .Match(
                    Some: x => x,
                    None: () => [])
                .AddOrUpdate(document.RangeKey, document));
}
