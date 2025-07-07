
using Kingo.Storage.Indexing;
using Kingo.Storage.Keys;

namespace Kingo.Storage.Tests;

public sealed class DocumentReaderTests
{
    private sealed record TestTuple(string Value);
    private sealed record AnotherTestTuple(string Value);

    private readonly DocumentIndex index = DocumentIndex.Empty();

    private (DocumentReader reader, DocumentWriter writer) ReaderWriter() =>
        (new(index), new(index));

    [Fact]
    public void Find_WhenDocumentExists_ReturnsSome()
    {
        var (reader, writer) = ReaderWriter();

        var document = Document.Cons("h", "r", new TestTuple("foo"));
        Assert.True(writer.Insert(document, CancellationToken.None).IsRight);

        var result = reader.Find<TestTuple>("h", "r");

        _ = result.Match(
            Some: doc => Assert.Equal("foo", doc.Record.Value),
            None: () => Assert.Fail("Expected Some, got None"));
    }

    [Fact]
    public void Find_WhenDocumentDoesNotExist_ReturnsNone()
    {
        var (reader, _) = ReaderWriter();
        var result = reader.Find<TestTuple>("h", "r");
        Assert.True(result.IsNone);
    }

    [Fact]
    public void Find_WhenTypeMismatch_ReturnsNone()
    {
        var (reader, writer) = ReaderWriter();

        var document = Document.Cons("h", "r", new TestTuple("foo"));
        Assert.True(writer.Insert(document, CancellationToken.None).IsRight);

        var result = reader.Find<AnotherTestTuple>("h", "r");
        Assert.True(result.IsNone);
    }

    [Fact]
    public void FindRange_Unbound_ReturnsAll()
    {
        var (reader, writer) = ReaderWriter();

        Assert.True(writer.Insert(Document.Cons("h", "a", new TestTuple("A")), CancellationToken.None).IsRight);
        Assert.True(writer.Insert(Document.Cons("h", "b", new TestTuple("B")), CancellationToken.None).IsRight);
        Assert.True(writer.Insert(Document.Cons("h", "c", new TestTuple("C")), CancellationToken.None).IsRight);

        var docs = reader.Find<TestTuple>("h", Storage.Keys.RangeKey.Unbound).ToArray();

        Assert.Equal(3, docs.Length);
        Assert.Contains(docs, d => d.Record.Value == "A");
        Assert.Contains(docs, d => d.Record.Value == "B");
        Assert.Contains(docs, d => d.Record.Value == "C");
    }

    [Fact]
    public void FindRange_Since_ReturnsCorrectRange()
    {
        var (reader, writer) = ReaderWriter();

        Assert.True(writer.Insert(Document.Cons("h", "a", new TestTuple("A")), CancellationToken.None).IsRight);
        Assert.True(writer.Insert(Document.Cons("h", "b", new TestTuple("B")), CancellationToken.None).IsRight);
        Assert.True(writer.Insert(Document.Cons("h", "c", new TestTuple("C")), CancellationToken.None).IsRight);

        var docs = reader.Find<TestTuple>("h", Storage.Keys.RangeKey.Since("b")).ToArray();

        Assert.Equal(2, docs.Length);
        Assert.Contains(docs, d => d.Record.Value == "B");
        Assert.Contains(docs, d => d.Record.Value == "C");
    }

    [Fact]
    public void FindRange_Until_ReturnsCorrectRange()
    {
        var (reader, writer) = ReaderWriter();

        Assert.True(writer.Insert(Document.Cons("h", "a", new TestTuple("A")), CancellationToken.None).IsRight);
        Assert.True(writer.Insert(Document.Cons("h", "b", new TestTuple("B")), CancellationToken.None).IsRight);
        Assert.True(writer.Insert(Document.Cons("h", "c", new TestTuple("C")), CancellationToken.None).IsRight);

        var docs = reader.Find<TestTuple>("h", Storage.Keys.RangeKey.Until("b")).ToArray();

        Assert.Equal(2, docs.Length);
        Assert.Contains(docs, d => d.Record.Value == "A");
        Assert.Contains(docs, d => d.Record.Value == "B");
    }

    [Fact]
    public void FindRange_Between_ReturnsCorrectRange()
    {
        var (reader, writer) = ReaderWriter();

        Assert.True(writer.Insert(Document.Cons("h", "a", new TestTuple("A")), CancellationToken.None).IsRight);
        Assert.True(writer.Insert(Document.Cons("h", "b", new TestTuple("B")), CancellationToken.None).IsRight);
        Assert.True(writer.Insert(Document.Cons("h", "c", new TestTuple("C")), CancellationToken.None).IsRight);

        var docs = reader.Find<TestTuple>("h", Storage.Keys.RangeKey.Between("a", "c")).ToArray();

        Assert.Equal(3, docs.Length);
        Assert.Contains(docs, d => d.Record.Value == "A");
        Assert.Contains(docs, d => d.Record.Value == "B");
        Assert.Contains(docs, d => d.Record.Value == "C");
    }

    [Fact]
    public void Where_FiltersCorrectly()
    {
        var (reader, writer) = ReaderWriter();

        Assert.True(writer.Insert(Document.Cons("h", "a", new TestTuple("A")), CancellationToken.None).IsRight);
        Assert.True(writer.Insert(Document.Cons("h", "b", new TestTuple("B")), CancellationToken.None).IsRight);
        Assert.True(writer.Insert(Document.Cons("h", "c", new TestTuple("C")), CancellationToken.None).IsRight);

        var docs = reader.Where<TestTuple>("h", d => d.Record.Value != "B").ToArray();

        Assert.Equal(2, docs.Length);
        Assert.Contains(docs, d => d.Record.Value == "A");
        Assert.Contains(docs, d => d.Record.Value == "C");
    }
}
