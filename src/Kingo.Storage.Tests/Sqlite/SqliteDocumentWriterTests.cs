using FluentAssertions;
using Kingo.Storage.Keys;
using Kingo.Storage.Sqlite;
using LanguageExt;

namespace Kingo.Storage.Tests.Sqlite;

public sealed class SqliteDocumentWriterTests
    : SqliteTests
{
    public SqliteDocumentWriterTests()
    {
        AddMigration(
            "create-table-test_header",
            """
            CREATE TABLE test_header (
                hashkey TEXT PRIMARY KEY,
                version INTEGER NOT NULL
            )
            """);
        AddMigration(
            "create-table-test_journal",
            """
            CREATE TABLE test_journal (
                hashkey TEXT NOT NULL,
                version INTEGER NOT NULL,
                data TEXT NOT NULL
            )
            """);
    }

    private readonly Key tableName = Key.From("test");

    private static readonly Key SomeKey = Key.From("SomeKey");
    private static readonly string SomeValue = "SomeValue";
    private static readonly Map<Key, object> TestData = Document.ConsData(SomeKey, SomeValue);

    private SqliteDocumentWriter<Key> CreateWriter() => new(Context, tableName);
    private SqliteDocumentReader<Key> CreateReader() => new(Context, tableName);

    [Fact]
    public async Task InsertAsync_WhenKeyDoesNotExist_Succeeds()
    {
        var writer = CreateWriter();
        var document = Document.Cons(Key.From("h"), TestData);

        await writer.InsertAsync(document, CancellationToken.None);

        var reader = CreateReader();
        var result = await reader.FindAsync(Key.From("h"), CancellationToken.None);

        _ = result.IsSome.Should().BeTrue();
        var doc = result.IfNone(() => throw new InvalidOperationException("Document not found"));
        _ = doc.HashKey.Should().Be(Key.From("h"));
        _ = doc.Version.Should().Be(Revision.Zero);
        _ = doc.Data.ContainsKey(SomeKey).Should().BeTrue();
    }

    [Fact]
    public async Task InsertAsync_WhenKeyExists_ThrowsDocumentWriterException()
    {
        var writer = CreateWriter();
        var document = Document.Cons(Key.From("h"), TestData);

        await writer.InsertAsync(document, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<DocumentWriterException>(() =>
            writer.InsertAsync(document, CancellationToken.None));

        _ = exception.Code.Should().Be(StorageErrorCodes.DuplicateKeyError);
        _ = exception.Message.Should().Contain("duplicate key h");
    }

    [Fact]
    public async Task InsertAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        var writer = CreateWriter();
        var document = Document.Cons(Key.From("h"), TestData);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _ = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            writer.InsertAsync(document, cts.Token));
    }

    [Fact]
    public async Task UpdateAsync_WhenKeyExistsAndVersionMatches_Succeeds()
    {
        var writer = CreateWriter();
        var reader = CreateReader();
        var document = Document.Cons(Key.From("h"), TestData);

        await writer.InsertAsync(document, CancellationToken.None);

        var read = (await reader.FindAsync(Key.From("h"), CancellationToken.None)).IfNone(() => throw new InvalidOperationException("Document not found"));
        var updated = read with { Data = Document.ConsData(Key.From("NewKey"), "NewValue") };

        await writer.UpdateAsync(updated, CancellationToken.None);

        var reread = (await reader.FindAsync(Key.From("h"), CancellationToken.None)).IfNone(() => throw new InvalidOperationException("Document not found"));
        _ = reread.Data.ContainsKey(Key.From("NewKey")).Should().BeTrue();
        _ = reread.Version.CompareTo(read.Version).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task UpdateAsync_WhenKeyDoesNotExist_ThrowsDocumentWriterException()
    {
        var writer = CreateWriter();
        var document = Document.Cons(Key.From("h"), TestData);

        var exception = await Assert.ThrowsAsync<DocumentWriterException>(() =>
            writer.UpdateAsync(document, CancellationToken.None));

        _ = exception.Code.Should().Be(StorageErrorCodes.NotFoundError);
        _ = exception.Message.Should().Contain("key not found h");
    }

    [Fact]
    public async Task UpdateAsync_WhenVersionDoesNotMatch_ThrowsDocumentWriterException()
    {
        var writer = CreateWriter();
        var reader = CreateReader();
        var document = Document.Cons(Key.From("h"), TestData);

        await writer.InsertAsync(document, CancellationToken.None);

        var read = (await reader.FindAsync(Key.From("h"), CancellationToken.None)).IfNone(() => throw new InvalidOperationException("Document not found"));
        var updated = read with { Version = read.Version.Tick(), Data = Document.ConsData(Key.From("NewKey"), "NewValue") };

        var exception = await Assert.ThrowsAsync<DocumentWriterException>(() =>
            writer.UpdateAsync(updated, CancellationToken.None));

        _ = exception.Code.Should().Be(StorageErrorCodes.VersionConflictError);
        _ = exception.Message.Should().Contain("version conflict");
    }

    [Fact]
    public async Task UpdateAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        var writer = CreateWriter();
        var reader = CreateReader();
        var document = Document.Cons(Key.From("h"), TestData);

        await writer.InsertAsync(document, CancellationToken.None);
        var read = (await reader.FindAsync(Key.From("h"), CancellationToken.None)).IfNone(() => throw new InvalidOperationException("Document not found"));

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _ = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            writer.UpdateAsync(read, cts.Token));
    }

    [Fact]
    public async Task InsertOrUpdateAsync_WhenKeyDoesNotExist_Inserts()
    {
        var writer = CreateWriter();
        var reader = CreateReader();
        var document = Document.Cons(Key.From("h"), TestData);

        await writer.InsertOrUpdateAsync(document, CancellationToken.None);

        var result = await reader.FindAsync(Key.From("h"), CancellationToken.None);
        _ = result.IsSome.Should().BeTrue();
        var doc = result.IfNone(() => throw new InvalidOperationException("Document not found"));
        _ = doc.Data.ContainsKey(SomeKey).Should().BeTrue();
        _ = doc.Version.Should().Be(Revision.Zero);
    }

    [Fact]
    public async Task InsertOrUpdateAsync_WhenKeyExistsAndVersionMatches_Updates()
    {
        var writer = CreateWriter();
        var reader = CreateReader();
        var document = Document.Cons(Key.From("h"), TestData);

        await writer.InsertAsync(document, CancellationToken.None);

        var read = (await reader.FindAsync(Key.From("h"), CancellationToken.None)).IfNone(() => throw new InvalidOperationException("Document not found"));
        var updated = read with { Data = Document.ConsData(Key.From("UpdatedKey"), "UpdatedValue") };

        await writer.InsertOrUpdateAsync(updated, CancellationToken.None);

        var reread = (await reader.FindAsync(Key.From("h"), CancellationToken.None)).IfNone(() => throw new InvalidOperationException("Document not found"));
        _ = reread.Data.ContainsKey(Key.From("UpdatedKey")).Should().BeTrue();
        _ = reread.Version.CompareTo(read.Version).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task InsertOrUpdateAsync_WhenKeyExistsButVersionMismatches_ThrowsDocumentWriterException()
    {
        var writer = CreateWriter();
        var reader = CreateReader();
        var document = Document.Cons(Key.From("h"), TestData);

        await writer.InsertAsync(document, CancellationToken.None);

        var read = (await reader.FindAsync(Key.From("h"), CancellationToken.None)).IfNone(() => throw new InvalidOperationException("Document not found"));
        var updated = read with { Version = read.Version.Tick(), Data = Document.ConsData(Key.From("UpdatedKey"), "UpdatedValue") };

        var exception = await Assert.ThrowsAsync<DocumentWriterException>(() =>
            writer.InsertOrUpdateAsync(updated, CancellationToken.None));

        _ = exception.Code.Should().Be(StorageErrorCodes.VersionConflictError);
        _ = exception.Message.Should().Contain("version conflict");
    }

    [Fact]
    public async Task InsertOrUpdateAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        var writer = CreateWriter();
        var document = Document.Cons(Key.From("h"), TestData);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _ = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            writer.InsertOrUpdateAsync(document, cts.Token));
    }

    [Fact]
    public async Task ConcurrentOperations_MaintainConsistency()
    {
        var writer = CreateWriter();
        var reader = CreateReader();
        var hashKey = Key.From("concurrent");

        // Insert initial document
        var document = Document.Cons(hashKey, TestData);
        await writer.InsertAsync(document, CancellationToken.None);

        // Run concurrent updates
        var tasks = Enumerable.Range(0, 10)
            .Select(i => Task.Run(async () =>
            {
                try
                {
                    var read = await reader.FindAsync(hashKey, CancellationToken.None);
                    if (read.IsSome)
                    {
                        var doc = read.IfNone(() => throw new InvalidOperationException());
                        var updated = doc with { Data = Document.ConsData(Key.From($"Update{i}"), $"Value{i}") };
                        await writer.UpdateAsync(updated, CancellationToken.None);
                        return true;
                    }

                    return false;
                }
                catch (DocumentWriterException ex) when (ex.Code == StorageErrorCodes.VersionConflictError)
                {
                    // Version conflicts are expected in concurrent scenarios
                    return false;
                }
            }))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // At least one update should succeed
        Assert.Contains(true, results);

        // Final document should have a version greater than zero
        var final = (await reader.FindAsync(hashKey, CancellationToken.None)).IfNone(() => throw new InvalidOperationException("Document not found"));
        _ = final.Version.CompareTo(Revision.Zero).Should().BeGreaterThan(0);
    }
}
