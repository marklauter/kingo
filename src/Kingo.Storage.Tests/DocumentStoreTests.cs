using Kingo.Storage.Ranges;

namespace Kingo.Storage.Tests;

public sealed class DocumentStoreTests
{
    private sealed record TestTuple(string Value);

    [Fact]
    public void TryPutSucceedsAndCanReadBack()
    {
        var store = DocumentStore.Empty();
        var doc = Document.Cons("h", "r", new TestTuple("foo"));
        Assert.Equal(DocumentStore.PutResponse.Success, store.TryPut(doc, CancellationToken.None));

        _ = store.Find<TestTuple>("h", "r")
            .Match(
                None: () => Assert.Fail("no read"),
                Some: d => Assert.Equal("foo", d.Record.Value));
    }

    [Fact]
    public void TryPutDuplicateFails()
    {
        var store = DocumentStore.Empty();
        var doc = Document.Cons("h", "r", new TestTuple("foo"));
        Assert.Equal(DocumentStore.PutResponse.Success, store.TryPut(doc, CancellationToken.None));
        Assert.Equal(DocumentStore.PutResponse.DuplicateKeyError, store.TryPut(doc, CancellationToken.None));
    }

    [Fact]
    public void TryUpdateSucceedsWithCorrectVersion()
    {
        var store = DocumentStore.Empty();
        Assert.Equal(DocumentStore.PutResponse.Success, store.TryPut(Document.Cons("h", "r", new TestTuple("foo")), CancellationToken.None));
        _ = store.Find<TestTuple>("h", "r")
            .Match(
                None: () => throw new Exception("no read"),
                Some: read =>
                {
                    var updated = read with { Record = new TestTuple("bar") };
                    Assert.Equal(DocumentStore.UpdateResponse.Success, store.TryUpdate(updated, CancellationToken.None));

                    _ = store.Find<TestTuple>("h", "r")
                        .Match(
                            None: () => throw new Exception("no read"),
                            Some: d =>
                            {
                                Assert.Equal("bar", d.Record.Value);
                                Assert.True(d.Version.Value > read.Version.Value);
                            });
                });
    }

    [Fact]
    public void TryUpdateFailsWithWrongVersion()
    {
        var store = DocumentStore.Empty();
        Assert.Equal(DocumentStore.PutResponse.Success, store.TryPut(Document.Cons("h", "r", new TestTuple("foo")), CancellationToken.None));
        _ = store.Find<TestTuple>("h", "r")
            .Match(
                None: () => throw new Exception("no read"),
                Some: read =>
                {
                    var updated = read with { Record = new TestTuple("bar") };
                    Assert.Equal(DocumentStore.UpdateResponse.Success, store.TryUpdate(updated, CancellationToken.None));
                    Assert.Equal(DocumentStore.UpdateResponse.VersionCheckFailedError, store.TryUpdate(updated, CancellationToken.None));
                });
    }

    [Fact]
    public void ReadReturnsNoneIfNotFound()
    {
        var store = DocumentStore.Empty();
        Assert.True(store.Find<TestTuple>("h", "r").IsNone);
    }

    [Fact]
    public void ReadRangeUnboundReturnsAll()
    {
        var store = DocumentStore.Empty();
        Assert.Equal(DocumentStore.PutResponse.Success, store.TryPut(Document.Cons("h", "a", new TestTuple("A")), CancellationToken.None));
        Assert.Equal(DocumentStore.PutResponse.Success, store.TryPut(Document.Cons("h", "b", new TestTuple("B")), CancellationToken.None));
        Assert.Equal(DocumentStore.PutResponse.Success, store.TryPut(Document.Cons("h", "c", new TestTuple("C")), CancellationToken.None));
        var docs = store
            .FindRange<TestTuple>("h", Unbound.Unbound())
            .ToArray();
        Assert.Equal(3, docs.Length);
        Assert.Contains(docs, d => d.Record.Value == "A");
        Assert.Contains(docs, d => d.Record.Value == "B");
        Assert.Contains(docs, d => d.Record.Value == "C");
    }

    [Fact]
    public void ReadRangeSinceReturnsCorrect()
    {
        var store = DocumentStore.Empty();
        Assert.Equal(DocumentStore.PutResponse.Success, store.TryPut(Document.Cons("h", "a", new TestTuple("A")), CancellationToken.None));
        Assert.Equal(DocumentStore.PutResponse.Success, store.TryPut(Document.Cons("h", "b", new TestTuple("B")), CancellationToken.None));
        Assert.Equal(DocumentStore.PutResponse.Success, store.TryPut(Document.Cons("h", "c", new TestTuple("C")), CancellationToken.None));
        var docs = store
            .FindRange<TestTuple>("h", Unbound.Since("b"))
            .ToArray();
        Assert.Equal(2, docs.Length);
        Assert.Contains(docs, d => d.Record.Value == "B");
        Assert.Contains(docs, d => d.Record.Value == "C");
    }

    [Fact]
    public void ReadRangeUntilReturnsCorrect()
    {
        var store = DocumentStore.Empty();
        Assert.Equal(DocumentStore.PutResponse.Success, store.TryPut(Document.Cons("h", "a", new TestTuple("A")), CancellationToken.None));
        Assert.Equal(DocumentStore.PutResponse.Success, store.TryPut(Document.Cons("h", "b", new TestTuple("B")), CancellationToken.None));
        Assert.Equal(DocumentStore.PutResponse.Success, store.TryPut(Document.Cons("h", "c", new TestTuple("C")), CancellationToken.None));
        var docs = store
            .FindRange<TestTuple>("h", Unbound.Until("b"))
            .ToArray();
        Assert.Equal(2, docs.Length);
        Assert.Contains(docs, d => d.Record.Value == "A");
        Assert.Contains(docs, d => d.Record.Value == "B");
    }

    [Fact]
    public void ReadRangeSpanReturnsCorrect()
    {
        var store = DocumentStore.Empty();
        Assert.Equal(DocumentStore.PutResponse.Success, store.TryPut(Document.Cons("h", "a", new TestTuple("A")), CancellationToken.None));
        Assert.Equal(DocumentStore.PutResponse.Success, store.TryPut(Document.Cons("h", "b", new TestTuple("B")), CancellationToken.None));
        Assert.Equal(DocumentStore.PutResponse.Success, store.TryPut(Document.Cons("h", "c", new TestTuple("C")), CancellationToken.None));
        var docs = store
            .FindRange<TestTuple>("h", Unbound.Between("a", "b"))
            .ToArray();
        Assert.Equal(2, docs.Length);
        Assert.Contains(docs, d => d.Record.Value == "A");
        Assert.Contains(docs, d => d.Record.Value == "B");
    }

    [Fact]
    public void WhereFiltersCorrectly()
    {
        var store = DocumentStore.Empty();
        Assert.Equal(DocumentStore.PutResponse.Success, store.TryPut(Document.Cons("h", "a", new TestTuple("A")), CancellationToken.None));
        Assert.Equal(DocumentStore.PutResponse.Success, store.TryPut(Document.Cons("h", "b", new TestTuple("B")), CancellationToken.None));
        Assert.Equal(DocumentStore.PutResponse.Success, store.TryPut(Document.Cons("h", "c", new TestTuple("C")), CancellationToken.None));
        var docs = store
            .Where<TestTuple>("h", d => d.Record.Value != "B")
            .ToArray();
        Assert.Equal(2, docs.Length);
        Assert.Contains(docs, d => d.Record.Value == "A");
        Assert.Contains(docs, d => d.Record.Value == "C");
    }
}
