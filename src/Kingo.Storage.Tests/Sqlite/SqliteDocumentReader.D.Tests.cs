using Kingo.Storage.Context;
using Kingo.Storage.Sqlite;
using LanguageExt;

namespace Kingo.Storage.Tests.Sqlite;

public sealed class SqliteDocumentReaderDTests
    : IAsyncLifetime
{
    private sealed record Color(
        [property: HashKey]
        string Name,
        [property: Version]
        Revision Version,
        int R,
        int G,
        int B)
    {
        // required for dapper and microsoft.data.sqlite
        private Color(string name, Revision version, long r, long g, long b)
            : this(name, version, Convert.ToInt32(r), Convert.ToInt32(g), Convert.ToInt32(b)) { }

        public static Color Red() => new("Red", Revision.Zero, 255, 0, 0);
        public static Color Green() => new("Green", Revision.Zero, 0, 255, 0);
        public static Color Blue() => new("Blue", Revision.Zero, 0, 255, 255);
        public static Color White() => new("White", Revision.Zero, 255, 255, 255);
        public static Color Black() => new("Black", Revision.Zero, 0, 0, 0);
        public static Color Gray() => new("White", Revision.Zero, 127, 127, 127);
    }

    private readonly string dbName = $"{Guid.NewGuid()}.sqlite";
    private readonly DbContext context;

    public SqliteDocumentReaderDTests() =>
        context = new(
            new SqliteConnectionFactory(
                new($"Data Source={dbName}", false)));

    private readonly Migrations migrations = Migrations.Empty()
        .Add(
            "create-table-test_journal",
            $"""
            CREATE TABLE IF NOT EXISTS {DocumentTypeCache<Color>.Name}_journal (
                hashkey TEXT NOT NULL,
                version INT NOT NULL,
                name TEXT NOT NULL,
                r INT NOT NULL,
                g INT NOT NULL,
                b INT NOT NULL,
                PRIMARY KEY (hashkey, version)
            )
            """)
        .Add(
            "create-table-test_header",
            $"""
            CREATE TABLE IF NOT EXISTS {DocumentTypeCache<Color>.Name}_header (
                hashkey TEXT NOT NULL,
                version INT NOT NULL,
                PRIMARY KEY (hashkey, version),
                FOREIGN KEY (hashkey, version) REFERENCES {DocumentTypeCache<Color>.Name}_journal (hashkey, version)
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

    private SqliteDocumentWriter<Color> CreateWriter() => new(context);
    private SqliteDocumentReader<Color> CreateReader() => new(context);

    [Fact]
    public async Task FindAsync_WhenDocumentExists_ReturnsSome()
    {
        var writer = CreateWriter();
        var reader = CreateReader();
        var key = Color.Red().Name;
        var color = Color.Red();

        await writer.InsertAsync(color, CancellationToken.None);

        var result = await reader.FindAsync(key, CancellationToken.None);

        Assert.True(result.IsSome);
        color = result.IfNone(() => throw new KeyNotFoundException("Document not found"));
        Assert.Equal(key, color.Name);
        Assert.Equal("Red", color.Name);
        Assert.Equal(255, color.R);
        Assert.Equal(Revision.Zero, color.Version);
    }

    [Fact]
    public async Task FindAsync_WhenDocumentDoesNotExist_ReturnsNone() =>
        Assert.True((await CreateReader().FindAsync("nonexistent", CancellationToken.None)).IsNone);

    [Fact]
    public async Task FindAsync_WithCancelledToken_ThrowsTaskCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _ = await Assert.ThrowsAsync<TaskCanceledException>(() =>
            CreateReader().FindAsync("nonexistent", cts.Token));
    }

    [Fact]
    public async Task FindAsync_ReturnsCorrectVersionAfterUpdate()
    {
        var writer = CreateWriter();
        var reader = CreateReader();
        var key = Color.Red().Name;
        var color = Color.Red();

        await writer.InsertAsync(color, CancellationToken.None);

        color = (await reader.FindAsync(key, CancellationToken.None))
            .IfNone(() => throw new InvalidOperationException("Document not found"));

        color = color with { Name = "Purple", B = 255 };

        await writer.UpdateAsync(color, CancellationToken.None);

        color = (await reader.FindAsync(key, CancellationToken.None))
            .IfNone(() => throw new InvalidOperationException("Document not found"));

        Assert.Equal(Revision.Zero.Tick(), color.Version);
        Assert.Equal(key, color.Name);
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
        var key = Color.Red().Name;
        var color = Color.Red();

        await writer.InsertAsync(color, CancellationToken.None);

        for (var i = 1; i <= 3; i++)
        {
            color = (await reader.FindAsync(key, CancellationToken.None))
                .IfNone(() => throw new InvalidOperationException("Document not found"));

            color = color with { Name = $"Purplish-{i}", B = (int)(i * (255.0 / 3)) };
            await writer.UpdateAsync(color, CancellationToken.None);
        }

        color = (await reader.FindAsync(key, CancellationToken.None))
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

        var redKey = Color.Red().Name;
        var greenKey = Color.Green().Name;
        var grayKey = Color.Gray().Name;

        var colors = new[]
        {
            Color.Red(),
            Color.Green(),
            Color.Gray(),
        };

        await Task.WhenAll(colors.Select(c => writer.InsertAsync(c, CancellationToken.None)));

        var colorOptions = await Task.WhenAll(colors.Select(c => reader.FindAsync(c.Name, CancellationToken.None)));
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
        var key = Color.Red().Name;
        var color = Color.Red();

        await writer.InsertAsync(color, CancellationToken.None);

        var limit = 50;
        var options = await Task.WhenAll(Enumerable.Range(0, limit)
            .Select(_ => reader.FindAsync(key, CancellationToken.None)));

        var count = options.Count(op =>
        {
            Assert.True(op.IsSome);
            color = op.IfNone(() => throw new InvalidOperationException("Document not found"));
            Assert.Equal(key, color.Name);
            Assert.Equal("Red", color.Name);
            Assert.Equal(255, color.R);
            Assert.Equal(Revision.Zero, color.Version);
            return op.IsSome;
        });

        Assert.Equal(limit, count);
    }
}
