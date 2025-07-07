using Kingo.Storage.Clocks;
using Kingo.Storage.Indexing;
using Kingo.Storage.Keys;

namespace Kingo.Storage.Tests;

public sealed class DocumentWriterTests
{
    private sealed record TestTuple(string Value);

    private readonly DocumentIndex<Key, Key> index = DocumentIndex.Empty<Key, Key>();

    private (DocumentReader<Key, Key> reader, DocumentWriter<Key, Key> writer) ReaderWriter() =>
        (new(index), new(index));

    [Fact]
    public void Insert_WhenKeyDoesNotExist_Succeeds()
    {
        var (_, writer) = ReaderWriter();

        var document = Document.Cons(Key.From("h"), Key.From("r"), new TestTuple("foo"));
        var result = writer.Insert(document, CancellationToken.None);
        Assert.True(result.IsRight);
    }

    [Fact]
    public void Insert_WhenKeyExists_Fails()
    {
        var (reader, writer) = ReaderWriter();

        var document = Document.Cons(Key.From("h"), Key.From("r"), new TestTuple("foo"));
        Assert.True(writer.Insert(document, CancellationToken.None).IsRight);
        var result = writer.Insert(document, CancellationToken.None);
        Assert.True(result.IsLeft);
        _ = result.IfLeft(error => Assert.Equal(ErrorCodes.DuplicateKeyError, error.Code));
    }

    [Fact]
    public void Insert_WithCancelledToken_ReturnsTimeoutError()
    {
        var (reader, writer) = ReaderWriter();

        var document = Document.Cons(Key.From("h"), Key.From("r"), new TestTuple("foo"));
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var result = writer.Insert(document, cts.Token);
        Assert.True(result.IsLeft);
        _ = result.IfLeft(error => Assert.Equal(ErrorCodes.TimeoutError, error.Code));
    }

    [Fact]
    public void Update_WhenKeyExistsAndVersionMatches_Succeeds()
    {
        var (reader, writer) = ReaderWriter();

        var document = Document.Cons(Key.From("h"), Key.From("r"), new TestTuple("foo"));
        Assert.True(writer.Insert(document, CancellationToken.None).IsRight);

        var read = reader.Find<TestTuple>("h", "r").IfNone(Fail<Document<Key, Key, TestTuple>>);
        var updated = read with { Record = new TestTuple("bar") };

        var result = writer.Update(updated, CancellationToken.None);
        Assert.True(result.IsRight);

        var reread = reader.Find<TestTuple>("h", "r").IfNone(Fail<Document<Key, Key, TestTuple>>);
        Assert.Equal("bar", reread.Record.Value);
        Assert.True(reread.Version > read.Version);
    }

    [Fact]
    public void Update_WhenKeyDoesNotExist_Fails()
    {
        var (reader, writer) = ReaderWriter();

        var document = Document.Cons(Key.From("h"), Key.From("r"), new TestTuple("foo"));
        var result = writer.Update(document, CancellationToken.None);
        Assert.True(result.IsLeft);
        _ = result.IfLeft(error => Assert.Equal(ErrorCodes.NotFoundError, error.Code));
    }

    [Fact]
    public void Update_WhenVersionDoesNotMatch_Fails()
    {
        var (reader, writer) = ReaderWriter();

        var document = Document.Cons(Key.From("h"), Key.From("r"), new TestTuple("foo"));
        Assert.True(writer.Insert(document, CancellationToken.None).IsRight);

        var read = reader.Find<TestTuple>("h", "r").IfNone(Fail<Document<Key, Key, TestTuple>>);
        var updated = read with { Version = read.Version.Tick(), Record = new TestTuple("bar") };

        var result = writer.Update(updated, CancellationToken.None);
        Assert.True(result.IsLeft);
        _ = result.IfLeft(error => Assert.Equal(ErrorCodes.VersionConflictError, error.Code));
    }

    [Fact]
    public void Update_WithCancelledToken_ReturnsTimeoutError()
    {
        var (reader, writer) = ReaderWriter();

        var document = Document.Cons(Key.From("h"), Key.From("r"), new TestTuple("foo"));
        Assert.True(writer.Insert(document, CancellationToken.None).IsRight);
        var read = reader.Find<TestTuple>("h", "r").IfNone(Fail<Document<Key, Key, TestTuple>>);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = writer.Update(read, cts.Token);
        Assert.True(result.IsLeft);
        _ = result.IfLeft(error => Assert.Equal(ErrorCodes.TimeoutError, error.Code));
    }

    [Fact]
    public void InsertOrUpdate_WhenKeyDoesNotExist_Inserts()
    {
        var (reader, writer) = ReaderWriter();

        var document = Document.Cons(Key.From("h"), Key.From("r"), new TestTuple("foo"));
        var result = writer.InsertOrUpdate(document, CancellationToken.None);
        Assert.True(result.IsRight);

        var read = reader.Find<TestTuple>("h", "r").IfNone(Fail<Document<Key, Key, TestTuple>>);
        Assert.Equal("foo", read.Record.Value);
        Assert.Equal(Revision.Zero, read.Version);
    }

    [Fact]
    public void InsertOrUpdate_WhenKeyExists_Updates()
    {
        var (reader, writer) = ReaderWriter();

        var document = Document.Cons(Key.From("h"), Key.From("r"), new TestTuple("foo"));
        Assert.True(writer.Insert(document, CancellationToken.None).IsRight);

        var read = reader.Find<TestTuple>("h", "r").IfNone(Fail<Document<Key, Key, TestTuple>>);
        var updated = read with { Record = new TestTuple("bar") };

        var result = writer.InsertOrUpdate(updated, CancellationToken.None);
        Assert.True(result.IsRight);

        var reread = reader.Find<TestTuple>("h", "r").IfNone(Fail<Document<Key, Key, TestTuple>>);
        Assert.Equal("bar", reread.Record.Value);
        Assert.True(reread.Version > read.Version);
    }

    [Fact]
    public void InsertOrUpdate_WhenKeyExistsButVersionMismatches_Fails()
    {
        var (reader, writer) = ReaderWriter();

        var document = Document.Cons(Key.From("h"), Key.From("r"), new TestTuple("foo"));
        Assert.True(writer.Insert(document, CancellationToken.None).IsRight);

        var read = reader.Find<TestTuple>("h", "r").IfNone(Fail<Document<Key, Key, TestTuple>>);
        var updated = read with { Version = read.Version.Tick(), Record = new TestTuple("bar") };

        var result = writer.InsertOrUpdate(updated, CancellationToken.None);
        Assert.True(result.IsLeft);
        _ = result.IfLeft(error => Assert.Equal(ErrorCodes.VersionConflictError, error.Code));
    }

    [Fact]
    public void InsertOrUpdate_WithCancelledToken_ReturnsTimeoutError()
    {
        var (reader, writer) = ReaderWriter();

        var document = Document.Cons(Key.From("h"), Key.From("r"), new TestTuple("foo"));
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var result = writer.InsertOrUpdate(document, cts.Token);
        Assert.True(result.IsLeft);
        _ = result.IfLeft(error => Assert.Equal(ErrorCodes.TimeoutError, error.Code));
    }

    private static T Fail<T>()
    {
        Assert.Fail("Expected Some, got None");
        return default!;
    }
}
