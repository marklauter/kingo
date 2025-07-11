//using Kingo.Namespaces.Serializable;
//using Kingo.Storage;
//using Kingo.Storage.Clocks;
//using Kingo.Storage.Indexing;
//using Kingo.Storage.Keys;

//namespace Kingo.Namespaces.Tests;

//public sealed class NamespaceWriterTests
//{
//    private readonly DocumentIndex<Key, Key> index = Storage.Indexing.Index.Empty<Key, Key>();

//    private (DocumentReader<Key, Key> reader, DocumentWriter<Key, Key> writer) ReaderWriter() =>
//        (new(index), new(index));

//    [Fact]
//    public async Task Insert_WritesAllRelationships_And_CanBeReadBack()
//    {
//        var (reader, writer) = ReaderWriter();

//        var nsWriter = new NamespaceWriter(writer);
//        var spec = await NamespaceSpec.FromFileAsync("Data/Namespace.Doc.json");

//        var results = nsWriter.Insert(spec, CancellationToken.None);

//        Assert.Equal(4, results.Length);
//        Assert.All(results, result => Assert.True(result.IsRight));

//        var rewriteReader = new RewriteReader(reader);

//        _ = rewriteReader.Read("doc", "owner")
//            .Match(
//                Some: owner => Assert.IsType<This>(owner),
//                None: () => Assert.Fail("Expected to find owner rewrite rule."));

//        _ = rewriteReader.Read("doc", "editor")
//            .Match(
//                Some: editor => Assert.IsType<UnionRewrite>(editor),
//                None: () => Assert.Fail("Expected to find editor rewrite rule."));

//        _ = rewriteReader.Read("doc", "viewer")
//            .Match(
//                Some: viewer => Assert.IsType<UnionRewrite>(viewer),
//                None: () => Assert.Fail("Expected to find viewer rewrite rule."));

//        _ = rewriteReader.Read("doc", "auditor")
//            .Match(
//                Some: viewer => Assert.IsType<IntersectionRewrite>(viewer),
//                None: () => Assert.Fail("Expected to find viewer rewrite rule."));
//    }

//    [Fact]
//    public async Task Insert_WhenNamespaceExists_Fails()
//    {
//        var (reader, writer) = ReaderWriter();
//        var nsWriter = new NamespaceWriter(writer);
//        var spec = await NamespaceSpec.FromFileAsync("Data/Namespace.Doc.json");

//        Assert.All(nsWriter.Insert(spec, CancellationToken.None), result => Assert.True(result.IsRight));

//        var results = nsWriter.Insert(spec, CancellationToken.None);

//        Assert.Equal(4, results.Length);
//        Assert.All(results, result =>
//        {
//            Assert.True(result.IsLeft);
//            _ = result.IfLeft(error => Assert.Equal(ErrorCodes.DuplicateKeyError, error.Code));
//        });
//    }

//    [Fact]
//    public async Task Update_WhenNamespaceExists_Succeeds()
//    {
//        var (reader, writer) = ReaderWriter();
//        var nsWriter = new NamespaceWriter(writer);
//        var spec = await NamespaceSpec.FromFileAsync("Data/Namespace.Doc.json");

//        Assert.All(nsWriter.Insert(spec, CancellationToken.None), result => Assert.True(result.IsRight));

//        var results = nsWriter.Update(spec, CancellationToken.None);

//        Assert.Equal(4, results.Length);
//        Assert.All(results, result => Assert.True(result.IsRight));

//        var ownerDoc = reader.Find("Namespace/doc", "owner");

//        Assert.True(ownerDoc.IsSome);
//        _ = ownerDoc.IfSome(doc => Assert.True(doc.Version > Revision.Zero));
//    }

//    [Fact]
//    public async Task Update_WhenNamespaceDoesNotExist_Fails()
//    {
//        var (reader, writer) = ReaderWriter();
//        var nsWriter = new NamespaceWriter(writer);
//        var spec = await NamespaceSpec.FromFileAsync("Data/Namespace.Doc.json");

//        var results = nsWriter.Update(spec, CancellationToken.None);

//        Assert.Equal(4, results.Length);
//        Assert.All(results, result =>
//        {
//            Assert.True(result.IsLeft);
//            _ = result.IfLeft(error => Assert.Equal(ErrorCodes.NotFoundError, error.Code));
//        });
//    }
//}
