using dead_code.Storage.InMemory.Indexing;
using Kingo.Storage;
using Kingo.Storage.Keys;
using LanguageExt;

namespace dead_code.Storage.Tests;

public sealed class DocumentIndexTests
{
    [Fact]
    public void EmptyReturnsNewDocumentIndexOfHK()
    {
        var index = InMemory.Indexing.Index.Empty<Key>();
        Assert.NotNull(index);
        _ = Assert.IsType<Index<Key>>(index);
        var snapshot = index.Snapshot();
        Assert.NotNull(snapshot);
        Assert.Empty(snapshot.Map);
    }

    [Fact]
    public void EmptyReturnsNewDocumentIndexOfHKRK()
    {
        var index = InMemory.Indexing.Index.Empty<Key, Key>();
        Assert.NotNull(index);
        _ = Assert.IsType<Index<Key, Key>>(index);
        var snapshot = index.Snapshot();
        Assert.NotNull(snapshot);
        Assert.Empty(snapshot.Map);
    }

    [Fact]
    public void ExchangeReplacesSnapshotForDocumentIndexOfHK()
    {
        var index = InMemory.Indexing.Index.Empty<Key>();
        var original = index.Snapshot();
        var doc = Document.Cons(Key.From("h"), Map<Key, object>.Empty);
        var map = Map.create((Key.From("h"), doc));
        var replacement = Snapshot.Cons(map);
        var exchanged = index.Exchange(original, replacement);
        Assert.True(exchanged);
        Assert.Equal(replacement, index.Snapshot());
    }

    [Fact]
    public void ExchangeReplacesSnapshotForDocumentIndexOfHKRK()
    {
        var index = InMemory.Indexing.Index.Empty<Key, Key>();
        var original = index.Snapshot();
        var doc = Document.Cons(Key.From("h"), Key.From("r"), Map<Key, object>.Empty);
        var innerMap = Map.create((Key.From("r"), doc));
        var map = Map.create((Key.From("h"), innerMap));
        var replacement = Snapshot.Cons(map);
        var exchanged = index.Exchange(original, replacement);
        Assert.True(exchanged);
        Assert.Equal(replacement, index.Snapshot());
    }

    [Fact]
    public void ExchangeReturnsFalseIfSnapshotDoesNotMatchForDocumentIndexOfHK()
    {
        var index = InMemory.Indexing.Index.Empty<Key>();
        var original = index.Snapshot();
        var other = Snapshot.Cons(Map<Key, Document<Key>>.Empty);
        var replacement = Snapshot.Cons(Map<Key, Document<Key>>.Empty);
        var exchanged = index.Exchange(other, replacement);
        Assert.False(exchanged);
        Assert.Equal(original, index.Snapshot());
    }

    [Fact]
    public void ExchangeReturnsFalseIfSnapshotDoesNotMatchForDocumentIndexOfHKRK()
    {
        var index = InMemory.Indexing.Index.Empty<Key, Key>();
        var original = index.Snapshot();
        var other = Snapshot.Cons(Map<Key, Map<Key, Document<Key, Key>>>.Empty);
        var replacement = Snapshot.Cons(Map<Key, Map<Key, Document<Key, Key>>>.Empty);
        var exchanged = index.Exchange(other, replacement);
        Assert.False(exchanged);
        Assert.Equal(original, index.Snapshot());
    }
}
