using Kingo.Storage.Db;
using Kingo.Storage.Keys;
using Kingo.Storage.Sqlite;

namespace Kingo.Storage.Tests.Sqlite;

public sealed class SqliteDocumentHKRKReaderTests
    : IAsyncLifetime
{
    private sealed record Color(Key HashKey, Key RangeKey, Revision Version, string Name, int R, int G, int B)
        : Document<Key, Key>(HashKey, RangeKey, Version)
    {
        // required for dapper and microsoft.data.sqlite
        private Color(Key hashKey, Key rangeKey, Revision version, string name, long r, long g, long b)
            : this(hashKey, rangeKey, version, name, Convert.ToInt32(r), Convert.ToInt32(g), Convert.ToInt32(b)) { }

        public static Color Red(Key pageId, Key buttonId) => new(pageId, buttonId, Revision.Zero, "Red", 255, 0, 0);
        public static Color Green(Key pageId, Key buttonId) => new(pageId, buttonId, Revision.Zero, "Green", 0, 255, 0);
        public static Color Blue(Key pageId, Key buttonId) => new(pageId, buttonId, Revision.Zero, "Blue", 0, 0, 255);
        public static Color White(Key pageId, Key buttonId) => new(pageId, buttonId, Revision.Zero, "White", 255, 255, 255);
        public static Color Black(Key pageId, Key buttonId) => new(pageId, buttonId, Revision.Zero, "Black", 0, 0, 0);
        public static Color Gray(Key pageId, Key buttonId) => new(pageId, buttonId, Revision.Zero, "White", 127, 127, 127);
    }

    private readonly string dbName = $"{Guid.NewGuid()}.sqlite";
    private readonly DbContext context;

    public SqliteDocumentHKRKReaderTests() =>
        context = new(
            new SqliteConnectionFactory(
                new($"Data Source={dbName}", false)));

    private readonly Migrations migrations = Migrations.Empty()
        .Add(
            "create-table-test_journal",
            $"""
            CREATE TABLE IF NOT EXISTS {DocumentTypeCache<Color>.TypeName}_journal (
                hashkey TEXT NOT NULL,
                rangekey TEXT NOT NULL,
                version INT NOT NULL,
                name TEXT NOT NULL,
                r INT NOT NULL,
                g INT NOT NULL,
                b INT NOT NULL,
                PRIMARY KEY (hashkey, rangekey, version)
            )
            """)
        .Add(
            "create-table-test_header",
            $"""
            CREATE TABLE IF NOT EXISTS {DocumentTypeCache<Color>.TypeName}_header (
                hashkey TEXT NOT NULL,
                rangekey TEXT NOT NULL,
                version INT NOT NULL,
                PRIMARY KEY (hashkey, rangekey, version),
                FOREIGN KEY (hashkey, rangekey, version) REFERENCES {DocumentTypeCache<Color>.TypeName}_journal (hashkey, rangekey, version)
            )
            """);

    public async Task InitializeAsync() =>
        await context.ApplyMigrationsAsync(migrations, CancellationToken.None);

    public Task DisposeAsync()
    {
        context.ClearAllPools();
        if (File.Exists(dbName))
            File.Delete(dbName);

        return Task.CompletedTask;
    }

    private SqliteDocumentWriter<Color, Key, Key> CreateWriter() => new(context);
    private SqliteDocumentReader<Color, Key, Key> CreateReader() => new(context);

    [Fact]
    public async Task FindAsync_WhenDocumentExists_ReturnsSome()
    {
        var writer = CreateWriter();
        var reader = CreateReader();

        var hashKey = Key.From("approval-page");
        var rangeKey = Key.From("cancel-button");
        var color = Color.Red(hashKey, rangeKey);

        await writer.InsertAsync(color, CancellationToken.None);

        var result = await reader.FindAsync(hashKey, rangeKey, CancellationToken.None);

        Assert.True(result.IsSome);
        color = result.IfNone(() => throw new KeyNotFoundException("Document not found"));
        Assert.Equal(hashKey, color.HashKey);
        Assert.Equal(rangeKey, color.RangeKey);
        Assert.Equal("Red", color.Name);
        Assert.Equal(255, color.R);
        Assert.Equal(Revision.Zero, color.Version);
    }

    [Fact]
    public async Task FindAsync_WhenDocumentDoesNotExist_ReturnsNone() =>
        Assert.True((await CreateReader().FindAsync("nonexistent", "nonexistent", CancellationToken.None)).IsNone);

    [Fact]
    public async Task FindAsync_WithCancelledToken_ThrowsTaskCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _ = await Assert.ThrowsAsync<TaskCanceledException>(() =>
            CreateReader().FindAsync("nonexistent", "nonexistent", cts.Token));
    }

    [Fact]
    public async Task FindAsync_ReturnsCorrectVersionAfterUpdate()
    {
        var writer = CreateWriter();
        var reader = CreateReader();
        var hashKey = Key.From("approval-page");
        var rangeKey = Key.From("cancel-button");
        var color = Color.Red(hashKey, rangeKey);

        await writer.InsertAsync(color, CancellationToken.None);

        color = (await reader.FindAsync(hashKey, rangeKey, CancellationToken.None))
            .IfNone(() => throw new InvalidOperationException("Document not found"));

        color = color with { Name = "Purple", B = 255 };

        await writer.UpdateAsync(color, CancellationToken.None);

        color = (await reader.FindAsync(hashKey, rangeKey, CancellationToken.None))
            .IfNone(() => throw new InvalidOperationException("Document not found"));

        Assert.Equal(Revision.Zero.Tick(), color.Version);
        Assert.Equal(hashKey, color.HashKey);
        Assert.Equal(rangeKey, color.RangeKey);
        Assert.Equal("Purple", color.Name);
        Assert.Equal(255, color.R);
        Assert.Equal(255, color.B);
        Assert.Equal(0, color.G);
    }

    [Fact]
    public async Task FindAsync_ReturnsLatestVersionWhenMultipleUpdates()
    {
        var writer = CreateWriter();
        var reader = CreateReader();
        var hashKey = Key.From("approval-page");
        var rangeKey = Key.From("cancel-button");
        var color = Color.Red(hashKey, rangeKey);

        await writer.InsertAsync(color, CancellationToken.None);

        for (var i = 1; i <= 3; i++)
        {
            color = (await reader.FindAsync(hashKey, rangeKey, CancellationToken.None))
                .IfNone(() => throw new InvalidOperationException("Document not found"));

            color = color with { Name = $"Purplish-{i}", B = (int)(i * (255.0 / 3)) };
            await writer.UpdateAsync(color, CancellationToken.None);
        }

        color = (await reader.FindAsync(hashKey, rangeKey, CancellationToken.None))
            .IfNone(() => throw new InvalidOperationException("Document not found"));

        Assert.Equal(Revision.From(3), color.Version);
        Assert.Equal("Purplish-3", color.Name);
        Assert.Equal(255, color.R);
        Assert.Equal(255, color.B);
        Assert.Equal(0, color.G);
    }

    [Fact]
    public async Task FindAsync_HandlesMultipleDocumentsAsync()
    {
        var writer = CreateWriter();
        var reader = CreateReader();

        var hashKey = Key.From("approval-page");
        var cancelButtonKey = Key.From("cancel-button");
        var okayButtonKey = Key.From("ok-button");
        var disabledButtonKey = Key.From("disabled-button");

        var colors = new[]
        {
            Color.Red(hashKey, cancelButtonKey),
            Color.Green(hashKey, okayButtonKey),
            Color.Gray(hashKey, disabledButtonKey),
        };

        await Task.WhenAll(colors.Select(c => writer.InsertAsync(c, CancellationToken.None)));

        var colorOptions = await Task.WhenAll(colors.Select(c => reader.FindAsync(c.HashKey, c.RangeKey, CancellationToken.None)));
        foreach (var op in colorOptions)
        {
            Assert.True(op.IsSome);
        }
    }

    [Fact]
    public async Task ConcurrentReads_ReturnConsistentResults()
    {
        var writer = CreateWriter();
        var reader = CreateReader();
        var hashKey = Key.From("approval-page");
        var rangeKey = Key.From("cancel-button");
        var color = Color.Red(hashKey, rangeKey);

        await writer.InsertAsync(color, CancellationToken.None);

        var limit = 50;
        var options = await Task.WhenAll(Enumerable.Range(0, limit)
            .Select(_ => reader.FindAsync(hashKey, rangeKey, CancellationToken.None)));

        var count = options.Count(op =>
        {
            Assert.True(op.IsSome);
            color = op.IfNone(() => throw new InvalidOperationException("Document not found"));
            Assert.Equal(hashKey, color.HashKey);
            Assert.Equal(rangeKey, color.RangeKey);
            Assert.Equal("Red", color.Name);
            Assert.Equal(255, color.R);
            Assert.Equal(Revision.Zero, color.Version);
            return op.IsSome;
        });

        Assert.Equal(limit, count);
    }
}
