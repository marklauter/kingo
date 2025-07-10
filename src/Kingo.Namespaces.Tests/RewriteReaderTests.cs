//using Kingo.Namespaces.Serializable;
//using Kingo.Storage;
//using Kingo.Storage.Indexing;
//using Kingo.Storage.Keys;

//namespace Kingo.Namespaces.Tests;

//public sealed class RewriteReaderTests
//{
//    private readonly DocumentIndex<Key, Key> index = Storage.Indexing.Index.Empty<Key, Key>();

//    private (DocumentReader<Key, Key> reader, DocumentWriter<Key, Key> writer) ReaderWriter() =>
//        (new(index), new(index));

//    [Fact]
//    public async Task FindReturnsSomeWhenNamespaceExists()
//    {
//        var (reader, writer) = ReaderWriter();

//        _ = new NamespaceWriter(writer)
//            .Insert(await NamespaceSpec.FromFileAsync("Data/Namespace.Doc.json"), CancellationToken.None);

//        var result = new RewriteReader(reader)
//            .Read("doc", "owner");

//        _ = result.Match(
//            Some: rewrite => Assert.IsType<This>(rewrite),
//            None: () => Assert.Fail("Expected Some but got None"));
//    }

//    [Fact]
//    public void FindReturnsNoneWhenNamespaceDoesNotExist()
//    {
//        var (reader, _) = ReaderWriter();

//        var result = new RewriteReader(reader).Read("doc", "owner");

//        Assert.True(result.IsNone);
//    }
//}
