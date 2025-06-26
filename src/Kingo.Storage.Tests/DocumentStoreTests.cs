using Kingo.Storage.Ranges;

namespace Kingo.Storage.Tests;

public sealed class DocumentStoreTests
{
    private sealed record TestTuple(string Value);

    [Fact]
    public void TryPutSucceedsAndCanReadBack()
    {
        var store = new DocumentStore();
        var doc = Document.Cons("h", "r", new TestTuple("foo"));
        var result = store.TryPut(doc, CancellationToken.None);
        Assert.True(result);
        var read = store.Read<TestTuple>("h", "r");
        Assert.True(read.IsSome);
        Assert.Equal("foo", read.IfNone(() => null)?.Tuple.Value);
    }

    [Fact]
    public void TryPutDuplicateFails()
    {
        var store = new DocumentStore();
        var doc = Document.Cons("h", "r", new TestTuple("foo"));
        Assert.True(store.TryPut(doc, CancellationToken.None));
        Assert.False(store.TryPut(doc, CancellationToken.None));
    }

    [Fact]
    public void TryUpdateSucceedsWithCorrectVersion()
    {
        var store = new DocumentStore();
        var doc = Document.Cons("h", "r", new TestTuple("foo"));
        Assert.True(store.TryPut(doc, CancellationToken.None));
        var read = store.Read<TestTuple>("h", "r").IfNone(() => null);
        var updated = read with { Tuple = new TestTuple("bar") };
        Assert.True(store.TryUpdate(updated, CancellationToken.None));
        var read2 = store.Read<TestTuple>("h", "r").IfNone(() => null);
        Assert.Equal("bar", read2.Tuple.Value);
        Assert.True(read2.Version.Value > read.Version.Value);
    }

    [Fact]
    public void TryUpdateFailsWithWrongVersion()
    {
        var store = new DocumentStore();
        var doc = Document.Cons("h", "r", new TestTuple("foo"));
        Assert.True(store.TryPut(doc, CancellationToken.None));
        var read = store.Read<TestTuple>("h", "r").IfNone(() => null);
        var updated = read with { Tuple = new TestTuple("bar"), Version = read.Version.Tick().Tick() };
        Assert.False(store.TryUpdate(updated, CancellationToken.None));
    }

    [Fact]
    public void ReadReturnsNoneIfNotFound()
    {
        var store = new DocumentStore();
        var read = store.Read<TestTuple>("h", "r");
        Assert.True(read.IsNone);
    }

    [Fact]
    public void ReadRangeUnboundReturnsAll()
    {
        var store = new DocumentStore();
        _ = store.TryPut(Document.Cons("h", "a", new TestTuple("A")), CancellationToken.None);
        _ = store.TryPut(Document.Cons("h", "b", new TestTuple("B")), CancellationToken.None);
        _ = store.TryPut(Document.Cons("h", "c", new TestTuple("C")), CancellationToken.None);
        var docs = store.Read<TestTuple>("h", UnboundRange.Unbound()).ToArray();
        Assert.Equal(3, docs.Length);
        Assert.Contains(docs, d => d.Tuple.Value == "A");
        Assert.Contains(docs, d => d.Tuple.Value == "B");
        Assert.Contains(docs, d => d.Tuple.Value == "C");
    }

    [Fact]
    public void ReadRangeSinceReturnsCorrect()
    {
        var store = new DocumentStore();
        _ = store.TryPut(Document.Cons("h", "a", new TestTuple("A")), CancellationToken.None);
        _ = store.TryPut(Document.Cons("h", "b", new TestTuple("B")), CancellationToken.None);
        _ = store.TryPut(Document.Cons("h", "c", new TestTuple("C")), CancellationToken.None);
        var docs = store.Read<TestTuple>("h", UnboundRange.Since("b")).ToArray();
        Assert.Equal(2, docs.Length);
        Assert.Contains(docs, d => d.Tuple.Value == "B");
        Assert.Contains(docs, d => d.Tuple.Value == "C");
    }

    [Fact]
    public void ReadRangeUntilReturnsCorrect()
    {
        var store = new DocumentStore();
        _ = store.TryPut(Document.Cons("h", "a", new TestTuple("A")), CancellationToken.None);
        _ = store.TryPut(Document.Cons("h", "b", new TestTuple("B")), CancellationToken.None);
        _ = store.TryPut(Document.Cons("h", "c", new TestTuple("C")), CancellationToken.None);
        var docs = store.Read<TestTuple>("h", UnboundRange.Until("b")).ToArray();
        Assert.Equal(2, docs.Length);
        Assert.Contains(docs, d => d.Tuple.Value == "A");
        Assert.Contains(docs, d => d.Tuple.Value == "B");
    }

    [Fact]
    public void ReadRangeSpanReturnsCorrect()
    {
        var store = new DocumentStore();
        _ = store.TryPut(Document.Cons("h", "a", new TestTuple("A")), CancellationToken.None);
        _ = store.TryPut(Document.Cons("h", "b", new TestTuple("B")), CancellationToken.None);
        _ = store.TryPut(Document.Cons("h", "c", new TestTuple("C")), CancellationToken.None);
        var docs = store.Read<TestTuple>("h", UnboundRange.Span("a", "b")).ToArray();
        Assert.Equal(2, docs.Length);
        Assert.Contains(docs, d => d.Tuple.Value == "A");
        Assert.Contains(docs, d => d.Tuple.Value == "B");
    }

    [Fact]
    public void WhereFiltersCorrectly()
    {
        var store = new DocumentStore();
        _ = store.TryPut(Document.Cons("h", "a", new TestTuple("A")), CancellationToken.None);
        _ = store.TryPut(Document.Cons("h", "b", new TestTuple("B")), CancellationToken.None);
        _ = store.TryPut(Document.Cons("h", "c", new TestTuple("C")), CancellationToken.None);
        var docs = store.Where<TestTuple>("h", d => d.Tuple.Value != "B").ToArray();
        Assert.Equal(2, docs.Length);
        Assert.Contains(docs, d => d.Tuple.Value == "A");
        Assert.Contains(docs, d => d.Tuple.Value == "C");
    }
}
