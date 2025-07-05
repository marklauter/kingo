using Kingo.Namespaces.Serializable;
using Kingo.Storage;
using Kingo.Storage.Indexing;

namespace Kingo.Namespaces.Tests;

public sealed class RewriteReaderTests
{
    private readonly DocumentIndex index = DocumentIndex.Empty();

    private (DocumentReader reader, DocumentWriter writer) ReaderWriter() =>
        (new(index), new(index));

    [Fact]
    public async Task FindReturnsSomeWhenNamespaceExists()
    {
        var (reader, writer) = ReaderWriter();

        _ = new NamespaceWriter(writer)
            .Insert(await NamespaceSpec.FromFileAsync("Data/Namespace.Doc.json"), CancellationToken.None);

        var result = new RewriteReader(reader)
            .Read("doc", "owner");

        _ = result.Match(
            Some: rewrite => Assert.IsType<This>(rewrite),
            None: () => Assert.Fail("Expected Some but got None"));
    }

    [Fact]
    public void FindReturnsNoneWhenNamespaceDoesNotExist()
    {
        var (reader, _) = ReaderWriter();

        var result = new RewriteReader(reader).Read("doc", "owner");

        Assert.True(result.IsNone);
    }
}
