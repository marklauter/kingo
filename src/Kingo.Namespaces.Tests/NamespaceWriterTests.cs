using Kingo.Namespaces.Serializable;
using Kingo.Storage;

namespace Kingo.Namespaces.Tests;

public sealed class NamespaceWriterTests
{
    [Fact]
    public async Task WriteWritesAllRelationships()
    {
        var store = DocumentStore.Empty();
        var writer = new NamespaceWriter(store);
        var spec = await NamespaceSpec.FromFileAsync("Data/Namespace.Doc.json");

        var results = writer.Write(spec, CancellationToken.None);

        Assert.Equal(3, results.Length);
        Assert.All(results, result => Assert.Equal(NamespaceWriter.WriteStatus.Success, result.Status));
        Assert.Contains(results, result => result.DocumentId == "namespace/doc/owner");
        Assert.Contains(results, result => result.DocumentId == "namespace/doc/editor");
        Assert.Contains(results, result => result.DocumentId == "namespace/doc/viewer");
    }
}
