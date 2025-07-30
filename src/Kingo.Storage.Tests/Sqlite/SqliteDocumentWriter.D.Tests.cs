using Dapper;
using Kingo.Storage.Context;
using Kingo.Storage.Sqlite;
using LanguageExt;

namespace Kingo.Storage.Tests.Sqlite;

public sealed class SqliteDocumentWriterDTests
    : IAsyncLifetime
{
    // Versioned document for testing header/journal pattern
    private sealed record Color(
        [property: HashKey]
        string Name,
        [property: Version]
        int Version,
        int R,
        int G,
        int B)
    {
        // required for dapper and microsoft.data.sqlite
        private Color(string name, int version, long r, long g, long b)
            : this(name, version, Convert.ToInt32(r), Convert.ToInt32(g), Convert.ToInt32(b)) { }

        public static Color Red() => new("Red", 0, 255, 0, 0);
        public static Color Green() => new("Green", 0, 0, 255, 0);
        public static Color Blue() => new("Blue", 0, 0, 255, 255);
    }

    // Non-versioned document for testing single table pattern
    private sealed record Shape(
        [property: HashKey]
        string Name,
        int Sides,
        string Type)
    {
        // required for dapper and microsoft.data.sqlite
        private Shape(string name, long sides, string type)
            : this(name, Convert.ToInt32(sides), type) { }

        public static Shape Triangle() => new("Triangle", 3, "Polygon");
        public static Shape Square() => new("Square", 4, "Polygon");
        public static Shape Circle() => new("Circle", 0, "Curved");
    }

    private readonly string dbName = $"{Guid.NewGuid()}.sqlite";
    private readonly DbContext context;

    public SqliteDocumentWriterDTests() =>
        context = new(
            new SqliteConnectionFactory(
                new($"Data Source={dbName}", false)));

    private readonly Migrations migrations = Migrations.Empty()
        // Versioned document tables (Color)
        .Add(
            "create-color-journal",
            $"""
            CREATE TABLE IF NOT EXISTS {DocumentTypeCache<Color>.Name}_journal (
                name TEXT NOT NULL,
                version INT NOT NULL,
                r INT NOT NULL,
                g INT NOT NULL,
                b INT NOT NULL,
                PRIMARY KEY (name, version)
            )
            """)
        .Add(
            "create-color-header",
            $"""
            CREATE TABLE IF NOT EXISTS {DocumentTypeCache<Color>.Name}_header (
                name TEXT NOT NULL,
                version INT NOT NULL,
                PRIMARY KEY (name, version),
                FOREIGN KEY (name, version) REFERENCES {DocumentTypeCache<Color>.Name}_journal (name, version)
            )
            """)
        // Non-versioned document table (Shape)
        .Add(
            "create-shape-table",
            $"""
            CREATE TABLE IF NOT EXISTS {DocumentTypeCache<Shape>.Name} (
                name TEXT NOT NULL,
                sides INT NOT NULL,
                type TEXT NOT NULL,
                PRIMARY KEY (name)
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

    private SqliteDocumentWriter<Color> CreateVersionedWriter() => new(context);
    private SqliteDocumentWriter<Shape> CreateNonVersionedWriter() => new(context);

    // Helper methods for direct SQL queries
    private async Task<(int headerCount, int journalCount)> GetVersionedRecordCountsAsync(string name)
    {
        var tableName = DocumentTypeCache<Color>.Name;
        return await context.ExecuteAsync(async (db, tx) =>
        {
            var headerCount = await db.QuerySingleAsync<int>(
                $"SELECT COUNT(*) FROM {tableName}_header WHERE name = @name",
                new { name }, tx);
            var journalCount = await db.QuerySingleAsync<int>(
                $"SELECT COUNT(*) FROM {tableName}_journal WHERE name = @name",
                new { name }, tx);
            return (headerCount, journalCount);
        }, CancellationToken.None);
    }

    private async Task<(string name, int version, int r, int g, int b)?> GetVersionedRecordAsync(string name)
    {
        var tableName = DocumentTypeCache<Color>.Name;
        return await context.ExecuteAsync(async (db, tx) =>
        {
            var sql = $"""
                SELECT b.name, b.version, b.r, b.g, b.b 
                FROM {tableName}_header a
                JOIN {tableName}_journal b ON b.name = a.name AND b.version = a.version
                WHERE a.name = @name
                """;
            return await db.QuerySingleOrDefaultAsync<(string, int, int, int, int)?>(sql, new { name }, tx);
        }, CancellationToken.None);
    }

    private async Task<int> GetNonVersionedRecordCountAsync(string name)
    {
        var tableName = DocumentTypeCache<Shape>.Name;
        return await context.ExecuteAsync(async (db, tx) =>
            await db.QuerySingleAsync<int>(
                $"SELECT COUNT(*) FROM {tableName} WHERE name = @name",
                new { name }, tx), CancellationToken.None);
    }

    private async Task<(string name, int sides, string type)?> GetNonVersionedRecordAsync(string name)
    {
        var tableName = DocumentTypeCache<Shape>.Name;
        return await context.ExecuteAsync(async (db, tx) =>
            await db.QuerySingleOrDefaultAsync<(string, int, string)?>(
                $"SELECT name, sides, type FROM {tableName} WHERE name = @name",
                new { name }, tx), CancellationToken.None);
    }

    // Versioned document tests
    [Fact]
    public async Task InsertAsync_VersionedDocument_CreatesRecordsInBothTables()
    {
        var writer = CreateVersionedWriter();
        var color = Color.Red();

        await writer.InsertAsync(color, CancellationToken.None);

        var (headerCount, journalCount) = await GetVersionedRecordCountsAsync("Red");
        Assert.Equal(1, headerCount);
        Assert.Equal(1, journalCount);

        var record = await GetVersionedRecordAsync("Red");
        _ = Assert.NotNull(record);
        Assert.Equal("Red", record.Value.name);
        Assert.Equal(0, record.Value.version); // Should be zeroed on insert
        Assert.Equal(255, record.Value.r);
        Assert.Equal(0, record.Value.g);
        Assert.Equal(0, record.Value.b);
    }

    [Fact]
    public async Task InsertAsync_VersionedDocumentDuplicate_ThrowsException()
    {
        var writer = CreateVersionedWriter();
        var color = Color.Red();

        await writer.InsertAsync(color, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<DocumentWriterException>(() =>
            writer.InsertAsync(color, CancellationToken.None));

        Assert.Equal(StorageErrorCodes.DuplicateKeyError, ex.Code);
        Assert.Contains("duplicate key", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_VersionedDocument_IncrementsVersionAndCreatesJournalEntry()
    {
        var writer = CreateVersionedWriter();
        var color = Color.Red();

        await writer.InsertAsync(color, CancellationToken.None);

        var updatedColor = color with { R = 128, G = 64 };
        await writer.UpdateAsync(updatedColor, CancellationToken.None);

        var (headerCount, journalCount) = await GetVersionedRecordCountsAsync("Red");
        Assert.Equal(1, headerCount); // Header should still be 1 record
        Assert.Equal(2, journalCount); // Journal should have 2 entries

        var record = await GetVersionedRecordAsync("Red");
        _ = Assert.NotNull(record);
        Assert.Equal("Red", record.Value.name);
        Assert.Equal(1, record.Value.version); // Version should be incremented
        Assert.Equal(128, record.Value.r);
        Assert.Equal(64, record.Value.g);
        Assert.Equal(0, record.Value.b);
    }

    [Fact]
    public async Task UpdateAsync_VersionedDocumentNotFound_ThrowsException()
    {
        var writer = CreateVersionedWriter();
        var color = Color.Red();

        var ex = await Assert.ThrowsAsync<DocumentWriterException>(() =>
            writer.UpdateAsync(color, CancellationToken.None));

        Assert.Equal(StorageErrorCodes.NotFoundError, ex.Code);
        Assert.Contains("key not found", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_VersionedDocumentVersionConflict_ThrowsException()
    {
        var writer = CreateVersionedWriter();
        var color = Color.Red();

        await writer.InsertAsync(color, CancellationToken.None);

        var conflictingColor = color with { Version = 5, R = 128 };
        var ex = await Assert.ThrowsAsync<DocumentWriterException>(() =>
            writer.UpdateAsync(conflictingColor, CancellationToken.None));

        Assert.Equal(StorageErrorCodes.VersionConflictError, ex.Code);
        Assert.Contains("version conflict", ex.Message);
    }

    [Fact]
    public async Task InsertOrUpdateAsync_VersionedDocumentNotExists_InsertsRecord()
    {
        var writer = CreateVersionedWriter();
        var color = Color.Blue();

        await writer.InsertOrUpdateAsync(color, CancellationToken.None);

        var (headerCount, journalCount) = await GetVersionedRecordCountsAsync("Blue");
        Assert.Equal(1, headerCount);
        Assert.Equal(1, journalCount);

        var record = await GetVersionedRecordAsync("Blue");
        _ = Assert.NotNull(record);
        Assert.Equal(0, record.Value.version);
    }

    [Fact]
    public async Task InsertOrUpdateAsync_VersionedDocumentExists_UpdatesRecord()
    {
        var writer = CreateVersionedWriter();
        var color = Color.Green();

        await writer.InsertAsync(color, CancellationToken.None);

        var updatedColor = color with { R = 100 };
        await writer.InsertOrUpdateAsync(updatedColor, CancellationToken.None);

        var record = await GetVersionedRecordAsync("Green");
        _ = Assert.NotNull(record);
        Assert.Equal(1, record.Value.version);
        Assert.Equal(100, record.Value.r);
    }

    [Fact]
    public async Task InsertAsync_WithCancelledToken_ThrowsTaskCanceledException()
    {
        var writer = CreateVersionedWriter();
        var color = Color.Red();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _ = await Assert.ThrowsAsync<TaskCanceledException>(() =>
            writer.InsertAsync(color, cts.Token));
    }

    // Non-versioned document tests
    [Fact]
    public async Task InsertAsync_NonVersionedDocument_CreatesRecordInSingleTable()
    {
        var writer = CreateNonVersionedWriter();
        var shape = Shape.Triangle();

        await writer.InsertAsync(shape, CancellationToken.None);

        var count = await GetNonVersionedRecordCountAsync("Triangle");
        Assert.Equal(1, count);

        var record = await GetNonVersionedRecordAsync("Triangle");
        _ = Assert.NotNull(record);
        Assert.Equal("Triangle", record.Value.name);
        Assert.Equal(3, record.Value.sides);
        Assert.Equal("Polygon", record.Value.type);
    }

    [Fact]
    public async Task InsertAsync_NonVersionedDocumentDuplicate_ThrowsException()
    {
        var writer = CreateNonVersionedWriter();
        var shape = Shape.Square();

        await writer.InsertAsync(shape, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<DocumentWriterException>(() =>
            writer.InsertAsync(shape, CancellationToken.None));

        Assert.Equal(StorageErrorCodes.DuplicateKeyError, ex.Code);
    }

    [Fact]
    public async Task UpdateAsync_NonVersionedDocument_UpdatesRecord()
    {
        var writer = CreateNonVersionedWriter();
        var shape = Shape.Circle();

        await writer.InsertAsync(shape, CancellationToken.None);

        var updatedShape = shape with { Type = "Round" };
        await writer.UpdateAsync(updatedShape, CancellationToken.None);

        var record = await GetNonVersionedRecordAsync("Circle");
        _ = Assert.NotNull(record);
        Assert.Equal("Circle", record.Value.name);
        Assert.Equal(0, record.Value.sides);
        Assert.Equal("Round", record.Value.type);
    }

    [Fact]
    public async Task UpdateAsync_NonVersionedDocumentNotFound_ThrowsException()
    {
        var writer = CreateNonVersionedWriter();
        var shape = Shape.Triangle();

        var ex = await Assert.ThrowsAsync<DocumentWriterException>(() =>
            writer.UpdateAsync(shape, CancellationToken.None));

        Assert.Equal(StorageErrorCodes.NotFoundError, ex.Code);
    }

    [Fact]
    public async Task InsertOrUpdateAsync_NonVersionedDocumentNotExists_InsertsRecord()
    {
        var writer = CreateNonVersionedWriter();
        var shape = Shape.Square();

        await writer.InsertOrUpdateAsync(shape, CancellationToken.None);

        var count = await GetNonVersionedRecordCountAsync("Square");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task InsertOrUpdateAsync_NonVersionedDocumentExists_UpdatesRecord()
    {
        var writer = CreateNonVersionedWriter();
        var shape = Shape.Triangle();

        await writer.InsertAsync(shape, CancellationToken.None);

        var updatedShape = shape with { Sides = 6, Type = "Hexagon" };
        await writer.InsertOrUpdateAsync(updatedShape, CancellationToken.None);

        var record = await GetNonVersionedRecordAsync("Triangle");
        _ = Assert.NotNull(record);
        Assert.Equal(6, record.Value.sides);
        Assert.Equal("Hexagon", record.Value.type);
    }

    [Fact]
    public async Task MultipleOperations_VersionedDocument_MaintainsConsistency()
    {
        var writer = CreateVersionedWriter();
        var colors = new[] { Color.Red(), Color.Green(), Color.Blue() };

        // Insert all colors
        await Task.WhenAll(colors.Select(c => writer.InsertAsync(c, CancellationToken.None)));

        // Update each color
        var updates = colors.Select(c => c with { R = c.R / 2 });
        await Task.WhenAll(updates.Select(c => writer.UpdateAsync(c, CancellationToken.None)));

        // Verify all have version 1
        foreach (var color in colors)
        {
            var record = await GetVersionedRecordAsync(color.Name);
            _ = Assert.NotNull(record);
            Assert.Equal(1, record.Value.version);
            Assert.Equal(color.R / 2, record.Value.r);
        }

        // Verify journal has 6 total entries (3 inserts + 3 updates)
        var tableName = DocumentTypeCache<Color>.Name;
        var totalJournalCount = await context.ExecuteAsync(async (db, tx) =>
            await db.QuerySingleAsync<int>($"SELECT COUNT(*) FROM {tableName}_journal", tx), CancellationToken.None);
        Assert.Equal(6, totalJournalCount);
    }
}
