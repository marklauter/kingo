
using Kingo.Storage.Indexing;
using Kingo.Storage.Keys;
using LanguageExt;

namespace Kingo.Storage.Tests;

public sealed class DocumentReaderHKRKTests
{
    private static readonly Key SomeKey = Key.From("SomeKey");
    private static readonly string SomeValue = "SomeValue";
    private static readonly Map<Key, string> Data = Map.create((SomeKey, SomeValue));

    private readonly DocumentIndex<Key, Key> index = DocumentIndex.Empty<Key, Key>();

    private (DocumentReader<Key, Key> reader, DocumentWriter<Key, Key> writer) ReaderWriter() =>
        (new(index), new(index));

    [Fact]
    public void Find_WhenDocumentExists_ReturnsSome()
    {
        var (reader, writer) = ReaderWriter();

        var document = Document.Cons(Key.From("h"), Key.From("r"), Data);
        Assert.True(writer.Insert(document, CancellationToken.None).IsRight);

        var result = reader.Find("h", "r");

        _ = result.Match(
            Some: doc => Assert.True(doc.Data.ContainsKey("SomeKey")),
            None: () => Assert.Fail("Expected Some, got None"));
    }

    [Fact]
    public void Find_WhenDocumentDoesNotExist_ReturnsNone()
    {
        var (reader, _) = ReaderWriter();
        var result = reader.Find("h", "r");
        Assert.True(result.IsNone);
    }

    [Fact]
    public void FindRange_Unbound_ReturnsAll()
    {
        var (reader, writer) = ReaderWriter();

        Assert.True(writer.Insert(Document.Cons(Key.From("h"), Key.From("a"), Map.create((Key.From("A"), "A"))), CancellationToken.None).IsRight);
        Assert.True(writer.Insert(Document.Cons(Key.From("h"), Key.From("b"), Map.create((Key.From("B"), "B"))), CancellationToken.None).IsRight);
        Assert.True(writer.Insert(Document.Cons(Key.From("h"), Key.From("c"), Map.create((Key.From("C"), "C"))), CancellationToken.None).IsRight);

        var docs = reader.Find(Key.From("h"), RangeKey.Unbound).ToArray();

        Assert.Equal(3, docs.Length);
        Assert.Contains(docs, d => d.Data.ContainsKey("A"));
        Assert.Contains(docs, d => d.Data.ContainsKey("B"));
        Assert.Contains(docs, d => d.Data.ContainsKey("C"));
    }

    [Fact]
    public void FindRange_Since_ReturnsCorrectRange()
    {
        var (reader, writer) = ReaderWriter();

        Assert.True(writer.Insert(Document.Cons(Key.From("h"), Key.From("a"), Map.create((Key.From("A"), "A"))), CancellationToken.None).IsRight);
        Assert.True(writer.Insert(Document.Cons(Key.From("h"), Key.From("b"), Map.create((Key.From("B"), "B"))), CancellationToken.None).IsRight);
        Assert.True(writer.Insert(Document.Cons(Key.From("h"), Key.From("c"), Map.create((Key.From("C"), "C"))), CancellationToken.None).IsRight);

        var docs = reader.Find(Key.From("h"), RangeKey.Since(Key.From("b"))).ToArray();

        Assert.Equal(2, docs.Length);
        Assert.Contains(docs, d => d.Data.ContainsKey("B"));
        Assert.Contains(docs, d => d.Data.ContainsKey("C"));
    }

    [Fact]
    public void FindRange_Until_ReturnsCorrectRange()
    {
        var (reader, writer) = ReaderWriter();

        Assert.True(writer.Insert(Document.Cons(Key.From("h"), Key.From("a"), Map.create((Key.From("A"), "A"))), CancellationToken.None).IsRight);
        Assert.True(writer.Insert(Document.Cons(Key.From("h"), Key.From("b"), Map.create((Key.From("B"), "B"))), CancellationToken.None).IsRight);
        Assert.True(writer.Insert(Document.Cons(Key.From("h"), Key.From("c"), Map.create((Key.From("C"), "C"))), CancellationToken.None).IsRight);

        var docs = reader.Find(Key.From("h"), RangeKey.Until(Key.From("b"))).ToArray();

        Assert.Equal(2, docs.Length);
        Assert.Contains(docs, d => d.Data.ContainsKey("A"));
        Assert.Contains(docs, d => d.Data.ContainsKey("B"));
    }

    [Fact]
    public void FindRange_Between_ReturnsCorrectRange()
    {
        var (reader, writer) = ReaderWriter();

        Assert.True(writer.Insert(Document.Cons(Key.From("h"), Key.From("a"), Map.create((Key.From("A"), "A"))), CancellationToken.None).IsRight);
        Assert.True(writer.Insert(Document.Cons(Key.From("h"), Key.From("b"), Map.create((Key.From("B"), "B"))), CancellationToken.None).IsRight);
        Assert.True(writer.Insert(Document.Cons(Key.From("h"), Key.From("c"), Map.create((Key.From("C"), "C"))), CancellationToken.None).IsRight);
        Assert.True(writer.Insert(Document.Cons(Key.From("h"), Key.From("d"), Map.create((Key.From("D"), "D"))), CancellationToken.None).IsRight);

        var docs = reader.Find(Key.From("h"), RangeKey.Between(Key.From("b"), Key.From("c"))).ToArray();

        Assert.Equal(2, docs.Length);
        Assert.Contains(docs, d => d.Data.ContainsKey("B"));
        Assert.Contains(docs, d => d.Data.ContainsKey("C"));
    }

    [Fact]
    public void Where_FiltersCorrectly()
    {
        var (reader, writer) = ReaderWriter();

        Assert.True(writer.Insert(Document.Cons(Key.From("h"), Key.From("a"), Map.create((Key.From("A"), "A"))), CancellationToken.None).IsRight);
        Assert.True(writer.Insert(Document.Cons(Key.From("h"), Key.From("b"), Map.create((Key.From("B"), "B"))), CancellationToken.None).IsRight);
        Assert.True(writer.Insert(Document.Cons(Key.From("h"), Key.From("c"), Map.create((Key.From("C"), "C"))), CancellationToken.None).IsRight);

        var docs = reader.Where(Key.From("h"), d => !d.Data.ContainsKey("B")).ToArray();

        Assert.Equal(2, docs.Length);
        Assert.Contains(docs, d => d.Data.ContainsKey("A"));
        Assert.Contains(docs, d => d.Data.ContainsKey("C"));
    }
}
