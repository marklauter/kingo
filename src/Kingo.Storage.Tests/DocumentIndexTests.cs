using Kingo.Storage.Indexing;
using Kingo.Storage.Keys;
using LanguageExt;

namespace Kingo.Storage.Tests;

public sealed class DocumentIndexTests
{
    [Fact]
    public void Empty_ReturnsNewDocumentIndexOfHK()
    {
        var index = DocumentIndex.Empty<Key>();
        Assert.NotNull(index);
        _ = Assert.IsType<DocumentIndex<Key>>(index);
        var snapshot = index.Snapshot();
        Assert.NotNull(snapshot);
        Assert.Empty(snapshot.Map);
    }

    [Fact]
    public void Empty_ReturnsNewDocumentIndexOfHKRK()
    {
        var index = DocumentIndex.Empty<Key, Key>();
        Assert.NotNull(index);
        _ = Assert.IsType<DocumentIndex<Key, Key>>(index);
        var snapshot = index.Snapshot();
        Assert.NotNull(snapshot);
        Assert.Empty(snapshot.Map);
    }

    [Fact]
    public void Exchange_ReplacesSnapshot_ForDocumentIndexOfHK()
    {
        var index = DocumentIndex.Empty<Key>();
        var original = index.Snapshot();
        var doc = Document.Cons(Key.From("h"), Map<Key, string>.Empty);
        var map = Map.create((Key.From("h"), doc));
        var replacement = Snapshot.From(map);
        var exchanged = index.Exchange(original, replacement);
        Assert.True(exchanged);
        Assert.Equal(replacement, index.Snapshot());
    }

    [Fact]
    public void Exchange_ReplacesSnapshot_ForDocumentIndexOfHKRK()
    {
        var index = DocumentIndex.Empty<Key, Key>();
        var original = index.Snapshot();
        var doc = Document.Cons(Key.From("h"), Key.From("r"), Map<Key, string>.Empty);
        var innerMap = Map.create((Key.From("r"), doc));
        var map = Map.create((Key.From("h"), innerMap));
        var replacement = Snapshot.From(map);
        var exchanged = index.Exchange(original, replacement);
        Assert.True(exchanged);
        Assert.Equal(replacement, index.Snapshot());
    }

    [Fact]
    public void Exchange_ReturnsFalse_IfSnapshotDoesNotMatch_ForDocumentIndexOfHK()
    {
        var index = DocumentIndex.Empty<Key>();
        var original = index.Snapshot();
        var other = Snapshot.From(Map<Key, Document<Key>>.Empty);
        var replacement = Snapshot.From(Map<Key, Document<Key>>.Empty);
        var exchanged = index.Exchange(other, replacement);
        Assert.False(exchanged);
        Assert.Equal(original, index.Snapshot());
    }

    [Fact]
    public void Exchange_ReturnsFalse_IfSnapshotDoesNotMatch_ForDocumentIndexOfHKRK()
    {
        var index = DocumentIndex.Empty<Key, Key>();
        var original = index.Snapshot();
        var other = Snapshot.From(Map<Key, Map<Key, Document<Key, Key>>>.Empty);
        var replacement = Snapshot.From(Map<Key, Map<Key, Document<Key, Key>>>.Empty);
        var exchanged = index.Exchange(other, replacement);
        Assert.False(exchanged);
        Assert.Equal(original, index.Snapshot());
    }
}
