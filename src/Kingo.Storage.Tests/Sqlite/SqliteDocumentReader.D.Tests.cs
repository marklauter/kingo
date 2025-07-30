using Kingo.Storage.Context;
using Kingo.Storage.Sqlite;
using LanguageExt;

namespace Kingo.Storage.Tests.Sqlite;

public sealed class SqliteDocumentReaderDTests
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
        // Constructor for Dapper with SQLite (Int64 -> Int32 conversion)
        private Color(string name, long version, long r, long g, long b)
            : this(name, Convert.ToInt32(version), Convert.ToInt32(r), Convert.ToInt32(g), Convert.ToInt32(b)) { }

        public static Color Red() => new("Red", 0, 255, 0, 0);
        public static Color Green() => new("Green", 0, 0, 255, 0);
        public static Color Blue() => new("Blue", 0, 0, 255, 255);
        public static Color White() => new("White", 0, 255, 255, 255);
        public static Color Black() => new("Black", 0, 0, 0, 0);
        public static Color Gray() => new("Gray", 0, 127, 127, 127);
    }

    // Non-versioned document with range key for testing single table pattern
    private sealed record Animal(
        [property: HashKey]
        string Species,
        [property: RangeKey]
        string Name,
        string Habitat,
        int Weight)
    {
        // Constructor for Dapper with SQLite (Int64 -> Int32 conversion)
        private Animal(string species, string name, string habitat, long weight)
            : this(species, name, habitat, Convert.ToInt32(weight)) { }

        public static Animal Lion() => new("Lion", "Simba", "Savanna", 190);
        public static Animal Tiger() => new("Tiger", "Rajah", "Jungle", 220);
        public static Animal Bear() => new("Bear", "Baloo", "Forest", 300);
        public static Animal Elephant() => new("Elephant", "Dumbo", "Savanna", 6000);
    }

    // Non-versioned document without range key
    private sealed record Shape(
        [property: HashKey]
        string Name,
        int Sides,
        string Type)
    {
        // Constructor for Dapper with SQLite (Int64 -> Int32 conversion)
        private Shape(string name, long sides, string type)
            : this(name, Convert.ToInt32(sides), type) { }

        public static Shape Triangle() => new("Triangle", 3, "Polygon");
        public static Shape Square() => new("Square", 4, "Polygon");
        public static Shape Circle() => new("Circle", 0, "Curved");
    }

    private readonly string dbName = $"{Guid.NewGuid()}.sqlite";
    private readonly DbContext context;

    public SqliteDocumentReaderDTests() =>
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
        // Non-versioned document with range key (Animal)
        .Add(
            "create-animal-table",
            $"""
            CREATE TABLE IF NOT EXISTS {DocumentTypeCache<Animal>.Name} (
                species TEXT NOT NULL,
                name TEXT NOT NULL,
                habitat TEXT NOT NULL,
                weight INT NOT NULL,
                PRIMARY KEY (species, name)
            )
            """)
        // Non-versioned document without range key (Shape)
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
    private SqliteDocumentReader<Color> CreateVersionedReader() => new(context);
    private SqliteDocumentWriter<Animal> CreateAnimalWriter() => new(context);
    private SqliteDocumentReader<Animal> CreateAnimalReader() => new(context);
    private SqliteDocumentWriter<Shape> CreateShapeWriter() => new(context);
    private SqliteDocumentReader<Shape> CreateShapeReader() => new(context);

    // Versioned document tests
    [Fact]
    public async Task FindAsync_VersionedDocument_WhenDocumentExists_ReturnsSome()
    {
        var writer = CreateVersionedWriter();
        var reader = CreateVersionedReader();
        var color = Color.Red();

        await writer.InsertAsync(color, CancellationToken.None);

        var result = await reader.FindAsync(color.Name, CancellationToken.None);

        Assert.True(result.IsSome);
        var retrievedColor = result.IfNone(() => throw new KeyNotFoundException("Document not found"));
        Assert.Equal("Red", retrievedColor.Name);
        Assert.Equal(255, retrievedColor.R);
        Assert.Equal(0, retrievedColor.G);
        Assert.Equal(0, retrievedColor.B);
        Assert.Equal(0, retrievedColor.Version);
    }

    [Fact]
    public async Task FindAsync_VersionedDocument_WhenDocumentDoesNotExist_ReturnsNone()
    {
        var reader = CreateVersionedReader();
        var result = await reader.FindAsync("nonexistent", CancellationToken.None);
        Assert.True(result.IsNone);
    }

    [Fact]
    public async Task FindAsync_VersionedDocument_ReturnsCorrectVersionAfterUpdate()
    {
        var writer = CreateVersionedWriter();
        var reader = CreateVersionedReader();
        var color = Color.Red();

        await writer.InsertAsync(color, CancellationToken.None);

        color = (await reader.FindAsync(color.Name, CancellationToken.None))
            .IfNone(() => throw new InvalidOperationException("Document not found"));

        var updatedColor = color with { R = 128, G = 64 };
        await writer.UpdateAsync(updatedColor, CancellationToken.None);

        color = (await reader.FindAsync(color.Name, CancellationToken.None))
            .IfNone(() => throw new InvalidOperationException("Document not found"));

        Assert.Equal(1, color.Version);
        Assert.Equal("Red", color.Name);
        Assert.Equal(128, color.R);
        Assert.Equal(64, color.G);
        Assert.Equal(0, color.B);
    }

    [Fact]
    public async Task FindAsync_VersionedDocument_ReturnsLatestVersionWhenMultipleUpdates()
    {
        var writer = CreateVersionedWriter();
        var reader = CreateVersionedReader();
        var color = Color.Blue();

        await writer.InsertAsync(color, CancellationToken.None);

        for (var i = 1; i <= 3; i++)
        {
            color = (await reader.FindAsync(color.Name, CancellationToken.None))
                .IfNone(() => throw new InvalidOperationException("Document not found"));

            color = color with { R = i * 50, G = i * 30 };
            await writer.UpdateAsync(color, CancellationToken.None);
        }

        color = (await reader.FindAsync(color.Name, CancellationToken.None))
            .IfNone(() => throw new InvalidOperationException("Document not found"));

        Assert.Equal(3, color.Version);
        Assert.Equal("Blue", color.Name);
        Assert.Equal(150, color.R);
        Assert.Equal(90, color.G);
        Assert.Equal(255, color.B);
    }

    // Non-versioned document with range key tests
    [Fact]
    public async Task FindAsync_NonVersionedWithRangeKey_WhenDocumentExists_ReturnsSome()
    {
        var writer = CreateAnimalWriter();
        var reader = CreateAnimalReader();
        var animal = Animal.Lion();

        await writer.InsertAsync(animal, CancellationToken.None);

        var result = await reader.FindAsync(animal.Species, animal.Name, CancellationToken.None);

        Assert.True(result.IsSome);
        var retrievedAnimal = result.IfNone(() => throw new KeyNotFoundException("Document not found"));
        Assert.Equal("Lion", retrievedAnimal.Species);
        Assert.Equal("Simba", retrievedAnimal.Name);
        Assert.Equal("Savanna", retrievedAnimal.Habitat);
        Assert.Equal(190, retrievedAnimal.Weight);
    }

    [Fact]
    public async Task FindAsync_NonVersionedWithRangeKey_WhenDocumentDoesNotExist_ReturnsNone()
    {
        var reader = CreateAnimalReader();
        var result = await reader.FindAsync("Dragon", "Smaug", CancellationToken.None);
        Assert.True(result.IsNone);
    }

    // Non-versioned document without range key tests
    [Fact]
    public async Task FindAsync_NonVersionedNoRangeKey_WhenDocumentExists_ReturnsSome()
    {
        var writer = CreateShapeWriter();
        var reader = CreateShapeReader();
        var shape = Shape.Triangle();

        await writer.InsertAsync(shape, CancellationToken.None);

        var result = await reader.FindAsync(shape.Name, CancellationToken.None);

        Assert.True(result.IsSome);
        var retrievedShape = result.IfNone(() => throw new KeyNotFoundException("Document not found"));
        Assert.Equal("Triangle", retrievedShape.Name);
        Assert.Equal(3, retrievedShape.Sides);
        Assert.Equal("Polygon", retrievedShape.Type);
    }

    [Fact]
    public async Task FindAsync_NonVersionedNoRangeKey_WhenDocumentDoesNotExist_ReturnsNone()
    {
        var reader = CreateShapeReader();
        var result = await reader.FindAsync("Hexagon", CancellationToken.None);
        Assert.True(result.IsNone);
    }

    // Query tests for range key scenarios
    [Fact]
    public async Task QueryAsync_WithRangeKeyEquals_ReturnsMatchingDocuments()
    {
        var writer = CreateAnimalWriter();
        var reader = CreateAnimalReader();

        var animals = new[] { Animal.Lion(), Animal.Tiger(), Animal.Bear() };
        await Task.WhenAll(animals.Select(a => writer.InsertAsync(a, CancellationToken.None)));

        var query = new Query<Animal, string>("Lion", RangeKeyCondition.IsEqualTo("Simba"));
        var results = await reader.QueryAsync(query, CancellationToken.None);

        _ = Assert.Single(results);
        var animal = results.Head.IfNone(() => throw new InvalidOperationException("Expected animal"));
        Assert.Equal("Lion", animal.Species);
        Assert.Equal("Simba", animal.Name);
    }

    [Fact]
    public async Task QueryAsync_WithRangeKeyGreaterThan_ReturnsMatchingDocuments()
    {
        var writer = CreateAnimalWriter();
        var reader = CreateAnimalReader();

        // Insert animals with same species but different names for range key testing
        var lion1 = new Animal("Lion", "Alpha", "Savanna", 180);
        var lion2 = new Animal("Lion", "Beta", "Savanna", 190);
        var lion3 = new Animal("Lion", "Gamma", "Savanna", 200);

        await Task.WhenAll(
            writer.InsertAsync(lion1, CancellationToken.None),
            writer.InsertAsync(lion2, CancellationToken.None),
            writer.InsertAsync(lion3, CancellationToken.None));

        var query = new Query<Animal, string>("Lion", RangeKeyCondition.IsGreaterThan("Beta"));
        var results = await reader.QueryAsync(query, CancellationToken.None);

        _ = Assert.Single(results);
        var animal = results.Head.IfNone(() => throw new InvalidOperationException("Expected animal"));
        Assert.Equal("Gamma", animal.Name);
    }

    [Fact]
    public async Task QueryAsync_WithRangeKeyBetween_ReturnsMatchingDocuments()
    {
        var writer = CreateAnimalWriter();
        var reader = CreateAnimalReader();

        var elephants = new[]
        {
            new Animal("Elephant", "Alice", "Savanna", 5000),
            new Animal("Elephant", "Bob", "Savanna", 5500),
            new Animal("Elephant", "Charlie", "Savanna", 6000),
            new Animal("Elephant", "David", "Savanna", 6500)
        };

        await Task.WhenAll(elephants.Select(e => writer.InsertAsync(e, CancellationToken.None)));

        var query = new Query<Animal, string>("Elephant", RangeKeyCondition.IsBetweenInclusive("Bob", "Charlie"));
        var results = await reader.QueryAsync(query, CancellationToken.None);

        Assert.Equal(2, results.Count);
        var names = results.Select(a => a.Name).ToArray();
        Assert.Contains("Bob", names);
        Assert.Contains("Charlie", names);
    }

    // Cancellation and error handling tests
    [Fact]
    public async Task FindAsync_WithCancelledToken_ThrowsTaskCanceledException()
    {
        var reader = CreateVersionedReader();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _ = await Assert.ThrowsAsync<TaskCanceledException>(() =>
            reader.FindAsync("test", cts.Token));
    }

    // Concurrency tests
    [Fact]
    public async Task ConcurrentReads_ReturnConsistentResults()
    {
        var writer = CreateVersionedWriter();
        var reader = CreateVersionedReader();
        var color = Color.Green();

        await writer.InsertAsync(color, CancellationToken.None);

        var tasks = Enumerable.Range(0, 50)
            .Select(_ => reader.FindAsync(color.Name, CancellationToken.None));

        var results = await Task.WhenAll(tasks);

        Assert.All(results, result =>
        {
            Assert.True(result.IsSome);
            var retrievedColor = result.IfNone(() => throw new InvalidOperationException("Expected color"));
            Assert.Equal("Green", retrievedColor.Name);
            Assert.Equal(0, retrievedColor.R);
            Assert.Equal(255, retrievedColor.G);
            Assert.Equal(0, retrievedColor.B);
        });
    }

    // Multiple document tests
    [Fact]
    public async Task FindAsync_MultipleDocuments_AllRetrievable()
    {
        var writer = CreateVersionedWriter();
        var reader = CreateVersionedReader();

        var colors = new[] { Color.Red(), Color.Green(), Color.Blue(), Color.White(), Color.Black() };
        await Task.WhenAll(colors.Select(c => writer.InsertAsync(c, CancellationToken.None)));

        var results = await Task.WhenAll(colors.Select(c => reader.FindAsync(c.Name, CancellationToken.None)));

        Assert.All(results, result => Assert.True(result.IsSome));

        var retrievedNames = results
            .Select(r => r.IfNone(() => throw new InvalidOperationException("Expected color")).Name)
            .ToArray();

        Assert.Equal(colors.Select(c => c.Name).OrderBy(n => n), retrievedNames.OrderBy(n => n));
    }

    private static readonly string[] Values = ["Baloo", "Paddington", "Yogi"];

    [Fact]
    public async Task QueryAsync_NoRangeKeyCondition_ReturnsAllForHashKey()
    {
        var writer = CreateAnimalWriter();
        var reader = CreateAnimalReader();

        var bears = new[]
        {
            new Animal("Bear", "Baloo", "Forest", 250),
            new Animal("Bear", "Yogi", "Forest", 200),
            new Animal("Bear", "Paddington", "City", 150)
        };

        await Task.WhenAll(bears.Select(b => writer.InsertAsync(b, CancellationToken.None)));

        var query = new Query<Animal, string>("Bear", Prelude.None);
        var results = await reader.QueryAsync(query, CancellationToken.None);

        Assert.Equal(3, results.Count);
        var names = results.Select(b => b.Name).OrderBy(n => n).ToArray();

        Assert.Equal(Values, names);
    }
}
