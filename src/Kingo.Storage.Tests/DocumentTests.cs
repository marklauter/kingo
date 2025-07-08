using Kingo.Storage.Clocks;
using Kingo.Storage.Keys;
using LanguageExt;

namespace Kingo.Storage.Tests;

public sealed class DocumentTests
{
    private static readonly Key SomeKey = Key.From("SomeKey");
    private static readonly string SomeValue = "SomeValue";
    private static readonly Map<Key, object> SomeData = Document.ConsData(SomeKey, SomeValue);

    [Fact]
    public void Cons_CreatesDocumentWithHashKeyAndZeroVersion()
    {
        var hashKey = Key.From("h");
        var doc = Document.Cons(hashKey, SomeData);

        Assert.Equal(hashKey, doc.HashKey);
        Assert.Equal(Revision.Zero, doc.Version);
        Assert.Equal(SomeData, doc.Data);
    }

    [Fact]
    public void Cons_WithVersion_CreatesDocumentWithHashKeyAndSpecifiedVersion()
    {
        var hashKey = Key.From("h");
        var version = Revision.From(123);
        var doc = Document.Cons(hashKey, version, SomeData);

        Assert.Equal(hashKey, doc.HashKey);
        Assert.Equal(version, doc.Version);
        Assert.Equal(SomeData, doc.Data);
    }

    [Fact]
    public void Cons_CreatesDocumentWithHashKeyAndRangeKeyAndZeroVersion()
    {
        var hashKey = Key.From("h");
        var rangeKey = Key.From("r");
        var doc = Document.Cons(hashKey, rangeKey, SomeData);

        Assert.Equal(hashKey, doc.HashKey);
        Assert.Equal(rangeKey, doc.RangeKey);
        Assert.Equal(Revision.Zero, doc.Version);
        Assert.Equal(SomeData, doc.Data);
    }

    [Fact]
    public void Cons_WithVersion_CreatesDocumentWithHashKeyAndRangeKeyAndSpecifiedVersion()
    {
        var hashKey = Key.From("h");
        var rangeKey = Key.From("r");
        var version = Revision.From(123);
        var doc = Document.Cons(hashKey, rangeKey, version, SomeData);

        Assert.Equal(hashKey, doc.HashKey);
        Assert.Equal(rangeKey, doc.RangeKey);
        Assert.Equal(version, doc.Version);
        Assert.Equal(SomeData, doc.Data);
    }

    [Fact]
    public void DocumentOfHK_IsSubclassOfDocument()
    {
        var doc = Document.Cons(Key.From("h"), SomeData);
        _ = Assert.IsType<Document>(doc, exactMatch: false);
    }

    [Fact]
    public void DocumentOfHKRK_IsSubclassOfDocumentOfHK()
    {
        var doc = Document.Cons(Key.From("h"), Key.From("r"), SomeData);
        _ = Assert.IsType<Document<Key>>(doc, exactMatch: false);
    }
}
