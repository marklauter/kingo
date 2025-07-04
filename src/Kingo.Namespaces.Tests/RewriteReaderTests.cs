using Kingo.Namespaces.Serializable;
using Kingo.Storage;

namespace Kingo.Namespaces.Tests;

public sealed class RewriteReaderTests
{
    [Fact]
    public async Task FindReturnsSomeWhenNamespaceExists()
    {
        var store = DocumentStore.Empty();
        _ = new NamespaceWriter(store)
            .Put(await NamespaceSpec.FromFileAsync("Data/Namespace.Doc.json"), CancellationToken.None);

        var result = new RewriteReader(store)
            .Read("doc", "owner");

        _ = result.Match(
            Some: rewrite => Assert.IsType<This>(rewrite),
            None: () => Assert.Fail("Expected Some but got None"));
    }

    [Fact]
    public void FindReturnsNoneWhenNamespaceDoesNotExist()
    {
        var store = DocumentStore.Empty();
        var reader = new RewriteReader(store);

        var result = reader.Read("doc", "owner");

        Assert.True(result.IsNone);
    }
}
