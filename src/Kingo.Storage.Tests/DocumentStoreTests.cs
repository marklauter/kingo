using Kingo.Storage.Keys;

namespace Kingo.Storage.Tests;

public sealed class DocumentStoreTests
{
    private sealed record TestTuple(string Value);
    private sealed record AnotherTestTuple(string Value);

    [Fact]
    public void TryPutSucceedsAndCanReadBack()
    {
        var store = DocumentStore.Empty();
        var doc = Document.Cons("h", "r", new TestTuple("foo"));
        Assert.Equal(DocumentStore.InsertStatus.Success, store.Insert(doc, CancellationToken.None));

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
        Assert.Equal(DocumentStore.InsertStatus.Success, store.Insert(doc, CancellationToken.None));
        Assert.Equal(DocumentStore.InsertStatus.DuplicateKeyError, store.Insert(doc, CancellationToken.None));
    }

    [Fact]
    public void TryUpdateSucceedsWithCorrectVersion()
    {
        var store = DocumentStore.Empty();
        Assert.Equal(DocumentStore.InsertStatus.Success, store.Insert(Document.Cons("h", "r", new TestTuple("foo")), CancellationToken.None));
        _ = store.Find<TestTuple>("h", "r")
            .Match(
                None: () => throw new Exception("no read"),
                Some: read =>
                {
                    var updated = read with { Record = new TestTuple("bar") };
                    Assert.Equal(DocumentStore.UpdateStatus.Success, store.Update(updated, CancellationToken.None));

                    _ = store.Find<TestTuple>("h", "r")
                        .Match(
                            None: () => throw new Exception("no read"),
                            Some: d =>
                            {
                                Assert.Equal("bar", d.Record.Value);
                                Assert.True(d.Version > read.Version);
                            });
                });
    }

    [Fact]
    public void TryUpdateFailsWithWrongVersion()
    {
        var store = DocumentStore.Empty();
        Assert.Equal(DocumentStore.InsertStatus.Success, store.Insert(Document.Cons("h", "r", new TestTuple("foo")), CancellationToken.None));
        _ = store.Find<TestTuple>("h", "r")
            .Match(
                None: () => throw new Exception("no read"),
                Some: read =>
                {
                    var updated = read with { Record = new TestTuple("bar") };
                    Assert.Equal(DocumentStore.UpdateStatus.Success, store.Update(updated, CancellationToken.None));
                    Assert.Equal(DocumentStore.UpdateStatus.VersionConflictError, store.Update(updated, CancellationToken.None));
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
        Assert.Equal(DocumentStore.InsertStatus.Success, store.Insert(Document.Cons("h", "a", new TestTuple("A")), CancellationToken.None));
        Assert.Equal(DocumentStore.InsertStatus.Success, store.Insert(Document.Cons("h", "b", new TestTuple("B")), CancellationToken.None));
        Assert.Equal(DocumentStore.InsertStatus.Success, store.Insert(Document.Cons("h", "c", new TestTuple("C")), CancellationToken.None));
        var docs = store
            .Find<TestTuple>("h", KeyRange.Unbound)
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
        Assert.Equal(DocumentStore.InsertStatus.Success, store.Insert(Document.Cons("h", "a", new TestTuple("A")), CancellationToken.None));
        Assert.Equal(DocumentStore.InsertStatus.Success, store.Insert(Document.Cons("h", "b", new TestTuple("B")), CancellationToken.None));
        Assert.Equal(DocumentStore.InsertStatus.Success, store.Insert(Document.Cons("h", "c", new TestTuple("C")), CancellationToken.None));
        var docs = store
            .Find<TestTuple>("h", KeyRange.Since("b"))
            .ToArray();
        Assert.Equal(2, docs.Length);
        Assert.Contains(docs, d => d.Record.Value == "B");
        Assert.Contains(docs, d => d.Record.Value == "C");
    }

    [Fact]
    public void ReadRangeUntilReturnsCorrect()
    {
        var store = DocumentStore.Empty();
        Assert.Equal(DocumentStore.InsertStatus.Success, store.Insert(Document.Cons("h", "a", new TestTuple("A")), CancellationToken.None));
        Assert.Equal(DocumentStore.InsertStatus.Success, store.Insert(Document.Cons("h", "b", new TestTuple("B")), CancellationToken.None));
        Assert.Equal(DocumentStore.InsertStatus.Success, store.Insert(Document.Cons("h", "c", new TestTuple("C")), CancellationToken.None));
        var docs = store
            .Find<TestTuple>("h", KeyRange.Until("b"))
            .ToArray();
        Assert.Equal(2, docs.Length);
        Assert.Contains(docs, d => d.Record.Value == "A");
        Assert.Contains(docs, d => d.Record.Value == "B");
    }

    [Fact]
    public void ReadRangeSpanReturnsCorrect()
    {
        var store = DocumentStore.Empty();
        Assert.Equal(DocumentStore.InsertStatus.Success, store.Insert(Document.Cons("h", "a", new TestTuple("A")), CancellationToken.None));
        Assert.Equal(DocumentStore.InsertStatus.Success, store.Insert(Document.Cons("h", "b", new TestTuple("B")), CancellationToken.None));
        Assert.Equal(DocumentStore.InsertStatus.Success, store.Insert(Document.Cons("h", "c", new TestTuple("C")), CancellationToken.None));
        var docs = store
            .Find<TestTuple>("h", KeyRange.Between("a", "b"))
            .ToArray();
        Assert.Equal(2, docs.Length);
        Assert.Contains(docs, d => d.Record.Value == "A");
        Assert.Contains(docs, d => d.Record.Value == "B");
    }

    [Fact]
    public void WhereFiltersCorrectly()
    {
        var store = DocumentStore.Empty();
        Assert.Equal(DocumentStore.InsertStatus.Success, store.Insert(Document.Cons("h", "a", new TestTuple("A")), CancellationToken.None));
        Assert.Equal(DocumentStore.InsertStatus.Success, store.Insert(Document.Cons("h", "b", new TestTuple("B")), CancellationToken.None));
        Assert.Equal(DocumentStore.InsertStatus.Success, store.Insert(Document.Cons("h", "c", new TestTuple("C")), CancellationToken.None));
        var docs = store
            .Where<TestTuple>("h", d => d.Record.Value != "B")
            .ToArray();
        Assert.Equal(2, docs.Length);
        Assert.Contains(docs, d => d.Record.Value == "A");
        Assert.Contains(docs, d => d.Record.Value == "C");
    }

    [Fact]
    public void TryPut_WithCancelledToken_ReturnsTimeoutError()
    {
        var store = DocumentStore.Empty();
        var doc = Document.Cons("h", "r", new TestTuple("foo"));
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.Equal(DocumentStore.InsertStatus.TimeoutError, store.Insert(doc, cts.Token));
    }

    [Fact]
    public void TryPutOrUpdate_WithCancelledToken_ReturnsTimeoutError()
    {
        var store = DocumentStore.Empty();
        var doc = Document.Cons("h", "r", new TestTuple("foo"));
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.Equal(DocumentStore.UpdateStatus.TimeoutError, store.Update(doc, cts.Token));
    }

    [Fact]
    public void Find_WithNonExistentHashKey_ReturnsNone()
    {
        var store = DocumentStore.Empty();
        Assert.True(store.Find<TestTuple>("non-existent", "r").IsNone);
    }

    [Fact]
    public void Find_WithRangeAndNonExistentHashKey_ReturnsEmpty()
    {
        var store = DocumentStore.Empty();
        var docs = store.Find<TestTuple>("non-existent", KeyRange.Unbound);
        Assert.Empty(docs);
    }

    [Fact]
    public void Where_WithNonExistentHashKey_ReturnsEmpty()
    {
        var store = DocumentStore.Empty();
        var docs = store.Where<TestTuple>("non-existent", _ => true);
        Assert.Empty(docs);
    }

    [Fact]
    public void Find_WithTypeMismatch_ReturnsNone()
    {
        var store = DocumentStore.Empty();
        var doc = Document.Cons("h", "r", new TestTuple("foo"));
        _ = store.Insert(doc, CancellationToken.None);
        Assert.True(store.Find<AnotherTestTuple>("h", "r").IsNone);
    }

    [Fact]
    public void FindRange_WithTypeMismatch_ReturnsEmpty()
    {
        var store = DocumentStore.Empty();
        _ = store.Insert(Document.Cons("h", "a", new TestTuple("A")), CancellationToken.None);
        _ = store.Insert(Document.Cons("h", "b", new AnotherTestTuple("B")), CancellationToken.None);
        var docs = store.Find<TestTuple>("h", KeyRange.Unbound);
        var doc = Assert.Single(docs);
        Assert.Equal("A", doc.Record.Value);
    }
}
