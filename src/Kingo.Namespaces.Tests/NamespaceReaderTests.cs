using Kingo.Namespaces.Serializable;
using Kingo.Storage;

namespace Kingo.Namespaces.Tests;

public sealed class NamespaceReaderTests
{
    [Fact]
    public async Task FindReturnsSomeWhenNamespaceExists()
    {
        var store = DocumentStore.Empty();
        var writer = new NamespaceWriter(store);
        var spec = await NamespaceSpec.FromFileAsync("Data/Namespace.Doc.json");
        _ = writer.Write(spec, CancellationToken.None);

        var reader = new NamespaceReader(store);
        var result = reader.Find("namespace/doc", "owner");

        _ = result.Match(
            Some: rewrite => Assert.IsType<This>(rewrite),
            None: () => Assert.Fail("Expected Some but got None"));
    }

    [Fact]
    public void FindReturnsNoneWhenNamespaceDoesNotExist()
    {
        var store = DocumentStore.Empty();
        var reader = new NamespaceReader(store);

        var result = reader.Find("namespace/doc", "owner");

        Assert.True(result.IsNone);
    }
}
