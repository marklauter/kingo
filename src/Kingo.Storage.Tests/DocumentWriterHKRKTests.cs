using Kingo.Storage.InMemory;
using Kingo.Storage.InMemory.Indexing;
using Kingo.Storage.Keys;
using LanguageExt;

namespace Kingo.Storage.Tests;

public sealed class DocumentWriterHKRKTests
{
    private static Map<Key, object> TestTuple(string key) =>
        Document.ConsData(key, key);

    private readonly Index<Key, Key> index = InMemory.Indexing.Index.Empty<Key, Key>();

    private (DocumentReader<Key, Key> reader, DocumentWriter<Key, Key> writer) ReaderWriter() =>
        (new(index), new(index));

    [Fact]
    public void Insert_WhenKeyDoesNotExist_Succeeds()
    {
        var (_, writer) = ReaderWriter();

        var document = Document.Cons(Key.From("h"), Key.From("r"), TestTuple("foo"));
        var result = writer.Insert(document, CancellationToken.None);
        Assert.True(result.IsRight);
    }

    [Fact]
    public void Insert_WhenKeyExists_Fails()
    {
        var (reader, writer) = ReaderWriter();

        var document = Document.Cons(Key.From("h"), Key.From("r"), TestTuple("foo"));
        Assert.True(writer.Insert(document, CancellationToken.None).IsRight);
        var result = writer.Insert(document, CancellationToken.None);
        Assert.True(result.IsLeft);
        _ = result.IfLeft(error => Assert.Equal(StorageErrorCodes.DuplicateKeyError, error.Code));
    }

    [Fact]
    public void Insert_WithCancelledToken_ReturnsTimeoutError()
    {
        var (reader, writer) = ReaderWriter();

        var document = Document.Cons(Key.From("h"), Key.From("r"), TestTuple("foo"));
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var result = writer.Insert(document, cts.Token);
        Assert.True(result.IsLeft);
        _ = result.IfLeft(error => Assert.Equal(StorageErrorCodes.TimeoutError, error.Code));
    }

    [Fact]
    public void Update_WhenKeyExistsAndVersionMatches_Succeeds()
    {
        var (reader, writer) = ReaderWriter();

        var document = Document.Cons(Key.From("h"), Key.From("r"), TestTuple("foo"));
        Assert.True(writer.Insert(document, CancellationToken.None).IsRight);

        var read = reader.Find("h", "r").IfNone(Fail<Document<Key, Key>>);
        var updated = read with { Data = TestTuple("bar") };

        var result = writer.Update(updated, CancellationToken.None);
        Assert.True(result.IsRight);

        var reread = reader.Find("h", "r").IfNone(Fail<Document<Key, Key>>);
        Assert.True(reread.Data.ContainsKey("bar"));
        Assert.True(reread.Version > read.Version);
    }

    [Fact]
    public void Update_WhenKeyDoesNotExist_Fails()
    {
        var (reader, writer) = ReaderWriter();

        var document = Document.Cons(Key.From("h"), Key.From("r"), TestTuple("foo"));
        var result = writer.Update(document, CancellationToken.None);
        Assert.True(result.IsLeft);
        _ = result.IfLeft(error => Assert.Equal(StorageErrorCodes.NotFoundError, error.Code));
    }

    [Fact]
    public void Update_WhenVersionDoesNotMatch_Fails()
    {
        var (reader, writer) = ReaderWriter();

        var document = Document.Cons(Key.From("h"), Key.From("r"), TestTuple("foo"));
        Assert.True(writer.Insert(document, CancellationToken.None).IsRight);

        var read = reader.Find("h", "r").IfNone(Fail<Document<Key, Key>>);
        var updated = read with { Version = read.Version.Tick(), Data = TestTuple("bar") };

        var result = writer.Update(updated, CancellationToken.None);
        Assert.True(result.IsLeft);
        _ = result.IfLeft(error => Assert.Equal(StorageErrorCodes.VersionConflictError, error.Code));
    }

    [Fact]
    public void Update_WithCancelledToken_ReturnsTimeoutError()
    {
        var (reader, writer) = ReaderWriter();

        var document = Document.Cons(Key.From("h"), Key.From("r"), TestTuple("foo"));
        Assert.True(writer.Insert(document, CancellationToken.None).IsRight);
        var read = reader.Find("h", "r").IfNone(Fail<Document<Key, Key>>);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = writer.Update(read, cts.Token);
        Assert.True(result.IsLeft);
        _ = result.IfLeft(error => Assert.Equal(StorageErrorCodes.TimeoutError, error.Code));
    }

    [Fact]
    public void InsertOrUpdate_WhenKeyDoesNotExist_Inserts()
    {
        var (reader, writer) = ReaderWriter();

        var document = Document.Cons(Key.From("h"), Key.From("r"), TestTuple("foo"));
        var result = writer.InsertOrUpdate(document, CancellationToken.None);
        Assert.True(result.IsRight);

        var read = reader.Find("h", "r").IfNone(Fail<Document<Key, Key>>);
        Assert.True(read.Data.ContainsKey("foo"));
        Assert.Equal(Clocks.Revision.Zero, read.Version);
    }

    [Fact]
    public void InsertOrUpdate_WhenKeyExists_Updates()
    {
        var (reader, writer) = ReaderWriter();

        var document = Document.Cons(Key.From("h"), Key.From("r"), TestTuple("foo"));
        Assert.True(writer.Insert(document, CancellationToken.None).IsRight);

        var read = reader.Find("h", "r").IfNone(Fail<Document<Key, Key>>);
        var updated = read with { Data = TestTuple("bar") };

        var result = writer.InsertOrUpdate(updated, CancellationToken.None);
        Assert.True(result.IsRight);

        var reread = reader.Find("h", "r").IfNone(Fail<Document<Key, Key>>);
        Assert.True(reread.Data.ContainsKey("bar"));
        Assert.True(reread.Version > read.Version);
    }

    [Fact]
    public void InsertOrUpdate_WhenKeyExistsButVersionMismatches_Fails()
    {
        var (reader, writer) = ReaderWriter();

        var document = Document.Cons(Key.From("h"), Key.From("r"), TestTuple("foo"));
        Assert.True(writer.Insert(document, CancellationToken.None).IsRight);

        var read = reader.Find("h", "r").IfNone(Fail<Document<Key, Key>>);
        var updated = read with { Version = read.Version.Tick(), Data = TestTuple("bar") };

        var result = writer.InsertOrUpdate(updated, CancellationToken.None);
        Assert.True(result.IsLeft);
        _ = result.IfLeft(error => Assert.Equal(StorageErrorCodes.VersionConflictError, error.Code));
    }

    [Fact]
    public void InsertOrUpdate_WithCancelledToken_ReturnsTimeoutError()
    {
        var (reader, writer) = ReaderWriter();

        var document = Document.Cons(Key.From("h"), Key.From("r"), TestTuple("foo"));
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var result = writer.InsertOrUpdate(document, cts.Token);
        Assert.True(result.IsLeft);
        _ = result.IfLeft(error => Assert.Equal(StorageErrorCodes.TimeoutError, error.Code));
    }

    private static T Fail<T>()
    {
        Assert.Fail("Expected Some, got None");
        return default!;
    }
}
