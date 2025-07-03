using Kingo.Namespaces.Serializable;
using Kingo.Storage;

namespace Kingo.Namespaces.Tests;

public sealed class SubjectSetRewriteReaderTests
{
    [Fact]
    public async Task FindReturnsSomeWhenNamespaceExists()
    {
        var store = DocumentStore.Empty();
        _ = new NamespaceWriter(store)
            .Put(await NamespaceSpec.FromFileAsync("Data/Namespace.Doc.json"), CancellationToken.None);

        var result = new SubjectSetRewriteReader(store)
            .Read("doc", "owner");

        _ = result.Match(
            Some: rewrite => Assert.IsType<This>(rewrite),
            None: () => Assert.Fail("Expected Some but got None"));
    }

    [Fact]
    public void FindReturnsNoneWhenNamespaceDoesNotExist()
    {
        var store = DocumentStore.Empty();
        var reader = new SubjectSetRewriteReader(store);

        var result = reader.Read("doc", "owner");

        Assert.True(result.IsNone);
    }
}
