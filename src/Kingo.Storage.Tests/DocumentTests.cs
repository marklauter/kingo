using Kingo.Storage.Clocks;
using Kingo.Storage.Keys;

namespace Kingo.Storage.Tests;

public sealed class DocumentTests
{
    private sealed record TestTuple(string Value);

    [Fact]
    public void Cons_CreatesDocumentWithZeroVersionAndCurrentTimestamp()
    {
        var hashKey = Key.From("h");
        var rangeKey = Key.From("r");
        var record = new TestTuple("foo");
        var before = DateTime.UtcNow;
        var doc = Document.Cons(hashKey, rangeKey, record);
        var after = DateTime.UtcNow;

        Assert.Equal(hashKey, doc.HashKey);
        Assert.Equal(rangeKey, doc.RangeKey);
        Assert.Equal(record, doc.Record);
        Assert.Equal(VersionClock.Zero, doc.Version);
        Assert.InRange(doc.Timestamp, before, after);
    }

    [Fact]
    public void Cons_WithVersion_CreatesDocumentWithSpecifiedVersionAndCurrentTimestamp()
    {
        var hashKey = Key.From("h");
        var rangeKey = Key.From("r");
        var version = VersionClock.From(123);
        var record = new TestTuple("foo");
        var before = DateTime.UtcNow;
        var doc = Document.Cons(hashKey, rangeKey, version, record);
        var after = DateTime.UtcNow;

        Assert.Equal(hashKey, doc.HashKey);
        Assert.Equal(rangeKey, doc.RangeKey);
        Assert.Equal(record, doc.Record);
        Assert.Equal(version, doc.Version);
        Assert.InRange(doc.Timestamp, before, after);
    }

    [Fact]
    public void DocumentOfT_IsSubclassOfDocument()
    {
        var docT = Document.Cons("h", "r", new TestTuple("foo"));
        _ = Assert.IsType<Document>(docT, exactMatch: false);
        _ = Assert.IsType<Document<TestTuple>>(docT, exactMatch: false);
    }
}
