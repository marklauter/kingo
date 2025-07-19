using FluentAssertions;
using Kingo.Storage.Db;
using Kingo.Storage.Keys;
using Kingo.Storage.Sqlite;
using LanguageExt;

namespace Kingo.Storage.Tests.Sqlite;

public sealed class SqliteDocumentReaderTests
    : IAsyncLifetime
{
    public SqliteDocumentReaderTests() =>
        context = new(
            new SqliteConnectionFactory(
                new($"Data Source={dbName}", false)));

    private readonly string dbName = $"{Guid.NewGuid()}.sqlite";
    private readonly DbContext context;
    private readonly Key tableName = Key.From("test");

    private static readonly Key SomeKey = Key.From("SomeKey");
    private static readonly string SomeValue = "SomeValue";
    private static readonly Map<Key, object> TestData = Document.ConsData(SomeKey, SomeValue);

    public async Task InitializeAsync() =>
        await context.ApplyMigrationsAsync(migrations, CancellationToken.None);

    public Task DisposeAsync()
    {
        context.ClearAllPools();
        if (File.Exists(dbName))
            File.Delete(dbName);

        return Task.CompletedTask;
    }

    private readonly Migrations migrations = Migrations.Empty()
        .Add(
            "create-table-test_header",
            """
            CREATE TABLE test_header (
                hashkey TEXT PRIMARY KEY,
                version INTEGER NOT NULL
            )
            """)
        .Add(
            "create-table-test_journal",
            """
            CREATE TABLE test_journal (
                hashkey TEXT NOT NULL,
                version INTEGER NOT NULL,
                data TEXT NOT NULL
            )
            """);

    private SqliteDocumentWriter<Key> CreateWriter() => new(context, tableName);
    private SqliteDocumentReader<Key> CreateReader() => new(context, tableName);

    [Fact]
    public async Task FindAsync_WhenDocumentExists_ReturnsSome()
    {
        var writer = CreateWriter();
        var reader = CreateReader();
        var document = Document.Cons(Key.From("h"), TestData);

        await writer.InsertAsync(document, CancellationToken.None);

        var result = await reader.FindAsync(Key.From("h"), CancellationToken.None);

        Assert.True(result.IsSome);
        var doc = result.IfNone(() => throw new InvalidOperationException("Document not found"));
        Assert.Equal(Key.From("h"), doc.HashKey);
        Assert.True(doc.Data.ContainsKey(SomeKey));
        Assert.Equal(Revision.Zero, doc.Version);
    }

    [Fact]
    public async Task FindAsync_WhenDocumentDoesNotExist_ReturnsNone()
    {
        var reader = CreateReader();

        var result = await reader.FindAsync(Key.From("nonexistent"), CancellationToken.None);

        Assert.True(result.IsNone);
    }

    [Fact]
    public async Task FindAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        var reader = CreateReader();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _ = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            reader.FindAsync(Key.From("h"), cts.Token));
    }

    [Fact]
    public async Task FindAsync_ReturnsCorrectVersionAfterUpdate()
    {
        var writer = CreateWriter();
        var reader = CreateReader();
        var document = Document.Cons(Key.From("h"), TestData);

        await writer.InsertAsync(document, CancellationToken.None);

        var read = (await reader.FindAsync(Key.From("h"), CancellationToken.None))
            .IfNone(() => throw new InvalidOperationException("Document not found"));
        var updated = read with { Data = Document.ConsData(Key.From("UpdatedKey"), "UpdatedValue") };

        await writer.UpdateAsync(updated, CancellationToken.None);

        var result = await reader.FindAsync(Key.From("h"), CancellationToken.None);

        Assert.True(result.IsSome);
        var doc = result.IfNone(() => throw new InvalidOperationException("Document not found"));
        Assert.True(doc.Version > Revision.Zero);
        Assert.True(doc.Data.ContainsKey(Key.From("UpdatedKey")));
    }

    [Fact]
    public async Task FindAsync_ReturnsLatestVersionWhenMultipleUpdates()
    {
        var writer = CreateWriter();
        var reader = CreateReader();
        var document = Document.Cons(Key.From("h"), TestData);

        await writer.InsertAsync(document, CancellationToken.None);

        // Perform multiple updates
        for (var i = 1; i <= 3; i++)
        {
            var read = (await reader.FindAsync(Key.From("h"), CancellationToken.None))
                .IfNone(() => throw new InvalidOperationException("Document not found"));
            var updated = read with { Data = Document.ConsData(Key.From($"Key{i}"), $"Value{i}") };
            await writer.UpdateAsync(updated, CancellationToken.None);
        }

        var result = await reader.FindAsync(Key.From("h"), CancellationToken.None);

        Assert.True(result.IsSome);
        var doc = result.IfNone(() => throw new InvalidOperationException("Document not found"));
        Assert.Equal(Revision.From(3), doc.Version);
        Assert.True(doc.Data.ContainsKey(Key.From("Key3")));
        Assert.False(doc.Data.ContainsKey(Key.From("Key1")));
        Assert.False(doc.Data.ContainsKey(Key.From("Key2")));
    }

    [Fact]
    public async Task FindAsync_HandlesMultipleDocuments()
    {
        var writer = CreateWriter();
        var reader = CreateReader();

        // Insert multiple documents
        var documents = new[]
        {
            Document.Cons(Key.From("doc1"), Document.ConsData(Key.From("Field1"), "Value1")),
            Document.Cons(Key.From("doc2"), Document.ConsData(Key.From("Field2"), "Value2")),
            Document.Cons(Key.From("doc3"), Document.ConsData(Key.From("Field3"), "Value3"))
        };

        foreach (var doc in documents)
        {
            await writer.InsertAsync(doc, CancellationToken.None);
        }

        // Verify each document can be found
        foreach (var doc in documents)
        {
            var result = await reader.FindAsync(doc.HashKey, CancellationToken.None);
            Assert.True(result.IsSome);
            var found = result.IfNone(() => throw new InvalidOperationException("Document not found"));
            Assert.Equal(doc.HashKey, found.HashKey);
            _ = found.Data.Should().BeEquivalentTo(doc.Data);
        }
    }

    [Fact]
    public async Task FindAsync_ReturnsCorrectDataTypes()
    {
        var writer = CreateWriter();
        var reader = CreateReader();

        var complexData = Document.ConsData(Key.From("StringField"), "test")
            .Add(Key.From("IntField"), 42)
            .Add(Key.From("BoolField"), true);

        var document = Document.Cons(Key.From("complex"), complexData);
        await writer.InsertAsync(document, CancellationToken.None);

        var result = await reader.FindAsync(Key.From("complex"), CancellationToken.None);

        Assert.True(result.IsSome);
        var doc = result.IfNone(() => throw new InvalidOperationException("Document not found"));

        Assert.Equal("test", doc.Field<string>(Key.From("StringField")).IfNone(""));
        Assert.Equal(42, doc.Field<int>(Key.From("IntField")).IfNone(0));
        Assert.True(doc.Field<bool>(Key.From("BoolField")).IfNone(false));
    }

    [Fact]
    public async Task ConcurrentReads_ReturnConsistentResults()
    {
        var writer = CreateWriter();
        var reader = CreateReader();
        var document = Document.Cons(Key.From("concurrent"), TestData);

        await writer.InsertAsync(document, CancellationToken.None);

        var tasks = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(() => reader.FindAsync(Key.From("concurrent"), CancellationToken.None)))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // All reads should succeed and return the same result
        foreach (var result in results)
        {
            Assert.True(result.IsSome);
            var doc = result.IfNone(() => throw new InvalidOperationException("Document not found"));
            Assert.Equal(Key.From("concurrent"), doc.HashKey);
            Assert.True(doc.Data.ContainsKey(SomeKey));
        }
    }
}
