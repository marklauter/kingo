using Kingo.Storage.Indexing;
using Kingo.Storage.Keys;
using LanguageExt;

namespace Kingo.Storage.Tests;

public sealed class DocumentReaderHKTests
{
    private static readonly Key SomeKey = Key.From("SomeKey");
    private static readonly string SomeValue = "SomeValue";
    private static readonly Map<Key, string> Data = Map.create((SomeKey, SomeValue));

    private readonly DocumentIndex<Key> index = DocumentIndex.Empty<Key>();

    private (DocumentReader<Key> reader, DocumentWriter<Key> writer) ReaderWriter() =>
        (new(index), new(index));

    [Fact]
    public void Find_WhenDocumentExists_ReturnsSome()
    {
        var (reader, writer) = ReaderWriter();
        var document = Document.Cons(Key.From("h"), Data);
        Assert.True(writer.Insert(document, CancellationToken.None).IsRight);
        var result = reader.Find(Key.From("h"));
        _ = result.Match(
            Some: doc => Assert.Equal(Data, doc.Data),
            None: () => Assert.Fail("Expected Some, got None"));
    }

    [Fact]
    public void Find_WhenDocumentDoesNotExist_ReturnsNone()
    {
        var (reader, _) = ReaderWriter();
        var result = reader.Find(Key.From("h"));
        Assert.True(result.IsNone);
    }
}
