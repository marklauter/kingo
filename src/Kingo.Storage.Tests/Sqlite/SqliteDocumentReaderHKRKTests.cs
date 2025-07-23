//using Dapper;
//using Kingo.Storage.Db;
//using Kingo.Storage.Keys;
//using Kingo.Storage.Sqlite;
//using LanguageExt;

//namespace Kingo.Storage.Tests.Sqlite;

//public sealed class SqliteDocumentReaderCompositeKeyTests
//    : IAsyncLifetime
//{
//    private sealed record AddressDocument(string Street, string City, string State, int Zip, Revision Version)
//    : Document<string, string>(Street, City, Version);

//    public SqliteDocumentReaderCompositeKeyTests() =>
//        context = new(
//            new SqliteConnectionFactory(
//                new($"Data Source={dbName}", false)));

//    private readonly string dbName = $"{Guid.NewGuid()}.sqlite";
//    private readonly DbContext context;
//    private readonly Key tableName = Key.From("test");

//    private static readonly Key SomeKey = Key.From("SomeKey");
//    private static readonly string SomeValue = "SomeValue";
//    private static readonly Map<Key, object> TestData = Document.ConsData(SomeKey, SomeValue);

//    public async Task InitializeAsync() =>
//        await context.ApplyMigrationsAsync(migrations, CancellationToken.None);

//    public Task DisposeAsync()
//    {
//        context.ClearAllPools();
//        if (File.Exists(dbName))
//            File.Delete(dbName);

//        return Task.CompletedTask;
//    }

//    private readonly Migrations migrations = Migrations.Empty()
//        .Add(
//            "create-table-test_header",
//            """
//            CREATE TABLE test_header (
//                hashkey TEXT NOT NULL,
//                rangekey TEXT NOT NULL,
//                version INTEGER NOT NULL,
//                PRIMARY KEY (hashkey, rangekey)
//            )
//            """)
//        .Add(
//            "create-table-test_journal",
//            """
//            CREATE TABLE test_journal (
//                id INTEGER PRIMARY KEY AUTOINCREMENT,
//                hashkey TEXT NOT NULL,
//                rangekey TEXT NOT NULL,
//                version INTEGER NOT NULL,
//                data TEXT NOT NULL
//            )
//            """);

//    private SqliteDocumentReader<Key, Key> CreateReader() => new(context, tableName);

//    private async Task InsertTestDocument(Key hashKey, Key rangeKey, Map<Key, object> data, Revision version = default)
//    {
//        if (version == default)
//            version = Revision.Zero;

//        // Manually insert data for testing the reader
//        await context.ExecuteAsync(async (db, tx) =>
//        {
//            var id = await db.ExecuteScalarAsync<long>(
//                "INSERT INTO test_journal (hashkey, rangekey, version, data) VALUES (@HashKey, @RangeKey, @Version, @Data); SELECT last_insert_rowid();",
//                new { HashKey = hashKey.ToString(), RangeKey = rangeKey.ToString(), Version = version.ToString(), Data = data.Serialize() },
//                tx);

//            _ = await db.ExecuteAsync(
//                "INSERT INTO test_header (hashkey, rangekey, version) VALUES (@HashKey, @RangeKey, @Version);",
//                new { HashKey = hashKey.ToString(), RangeKey = rangeKey.ToString(), Version = version.ToString() },
//                tx);
//        }, CancellationToken.None);
//    }

//    [Fact]
//    public async Task FindAsync_WithSpecificRangeKey_WhenDocumentExists_ReturnsSome()
//    {
//        var reader = CreateReader();
//        var hashKey = Key.From("h");
//        var rangeKey = Key.From("r");

//        await InsertTestDocument(hashKey, rangeKey, TestData);

//        var result = await reader.FindAsync(hashKey, rangeKey, CancellationToken.None);

//        Assert.True(result.IsSome);
//        var doc = result.IfNone(() => throw new InvalidOperationException("Document not found"));
//        Assert.Equal(hashKey, doc.HashKey);
//        Assert.Equal(rangeKey, doc.RangeKey);
//        Assert.True(doc.Data.ContainsKey(SomeKey));
//        Assert.Equal(Revision.Zero, doc.Version);
//    }

//    [Fact]
//    public async Task FindAsync_WithSpecificRangeKey_WhenDocumentDoesNotExist_ReturnsNone()
//    {
//        var reader = CreateReader();

//        var result = await reader.FindAsync(Key.From("nonexistent"), Key.From("r"), CancellationToken.None);

//        Assert.True(result.IsNone);
//    }

//    [Fact]
//    public async Task FindAsync_WithRangeKey_WithCancelledToken_ThrowsTaskCanceledException()
//    {
//        var reader = CreateReader();

//        using var cts = new CancellationTokenSource();
//        cts.Cancel();

//        _ = await Assert.ThrowsAsync<TaskCanceledException>(() =>
//            reader.FindAsync(Key.From("h"), Key.From("r"), cts.Token));
//    }

//    [Fact]
//    public async Task FindAsync_WithRangeKeyFilter_UnboundRange_ReturnsAll()
//    {
//        var reader = CreateReader();
//        var hashKey = Key.From("h");

//        await InsertTestDocument(hashKey, Key.From("a"), Document.ConsData(Key.From("Field"), "A"));
//        await InsertTestDocument(hashKey, Key.From("b"), Document.ConsData(Key.From("Field"), "B"));
//        await InsertTestDocument(hashKey, Key.From("c"), Document.ConsData(Key.From("Field"), "C"));

//        var result = await reader.FindAsync(hashKey, RangeKey.Unbound, CancellationToken.None);

//        var docs = result.ToArray();
//        Assert.Equal(3, docs.Length);
//        Assert.Contains(docs, d => d.Field<string>(Key.From("Field")).IfNone("") == "A");
//        Assert.Contains(docs, d => d.Field<string>(Key.From("Field")).IfNone("") == "B");
//        Assert.Contains(docs, d => d.Field<string>(Key.From("Field")).IfNone("") == "C");
//    }

//    [Fact]
//    public async Task FindAsync_WithRangeKeyFilter_LowerBound_ReturnsCorrectRange()
//    {
//        var reader = CreateReader();
//        var hashKey = Key.From("h");

//        await InsertTestDocument(hashKey, Key.From("a"), Document.ConsData(Key.From("Field"), "A"));
//        await InsertTestDocument(hashKey, Key.From("b"), Document.ConsData(Key.From("Field"), "B"));
//        await InsertTestDocument(hashKey, Key.From("c"), Document.ConsData(Key.From("Field"), "C"));
//        await InsertTestDocument(hashKey, Key.From("d"), Document.ConsData(Key.From("Field"), "D"));

//        var result = await reader.FindAsync(hashKey, RangeKey.Lower(Key.From("c")), CancellationToken.None);

//        var docs = result.ToArray();
//        Assert.Equal(2, docs.Length);
//        Assert.Contains(docs, d => d.Field<string>(Key.From("Field")).IfNone("") == "C");
//        Assert.Contains(docs, d => d.Field<string>(Key.From("Field")).IfNone("") == "D");
//        Assert.DoesNotContain(docs, d => d.Field<string>(Key.From("Field")).IfNone("") == "A");
//        Assert.DoesNotContain(docs, d => d.Field<string>(Key.From("Field")).IfNone("") == "B");
//    }

//    [Fact]
//    public async Task FindAsync_WithRangeKeyFilter_UpperBound_ReturnsCorrectRange()
//    {
//        var reader = CreateReader();
//        var hashKey = Key.From("h");

//        await InsertTestDocument(hashKey, Key.From("a"), Document.ConsData(Key.From("Field"), "A"));
//        await InsertTestDocument(hashKey, Key.From("b"), Document.ConsData(Key.From("Field"), "B"));
//        await InsertTestDocument(hashKey, Key.From("c"), Document.ConsData(Key.From("Field"), "C"));
//        await InsertTestDocument(hashKey, Key.From("d"), Document.ConsData(Key.From("Field"), "D"));

//        var result = await reader.FindAsync(hashKey, RangeKey.Upper(Key.From("b")), CancellationToken.None);

//        var docs = result.ToArray();
//        Assert.Equal(2, docs.Length);
//        Assert.Contains(docs, d => d.Field<string>(Key.From("Field")).IfNone("") == "A");
//        Assert.Contains(docs, d => d.Field<string>(Key.From("Field")).IfNone("") == "B");
//        Assert.DoesNotContain(docs, d => d.Field<string>(Key.From("Field")).IfNone("") == "C");
//        Assert.DoesNotContain(docs, d => d.Field<string>(Key.From("Field")).IfNone("") == "D");
//    }

//    [Fact]
//    public async Task FindAsync_WithRangeKeyFilter_Between_ReturnsCorrectRange()
//    {
//        var reader = CreateReader();
//        var hashKey = Key.From("h");

//        await InsertTestDocument(hashKey, Key.From("a"), Document.ConsData(Key.From("Field"), "A"));
//        await InsertTestDocument(hashKey, Key.From("b"), Document.ConsData(Key.From("Field"), "B"));
//        await InsertTestDocument(hashKey, Key.From("c"), Document.ConsData(Key.From("Field"), "C"));
//        await InsertTestDocument(hashKey, Key.From("d"), Document.ConsData(Key.From("Field"), "D"));
//        await InsertTestDocument(hashKey, Key.From("e"), Document.ConsData(Key.From("Field"), "E"));

//        var result = await reader.FindAsync(hashKey, RangeKey.Between(Key.From("b"), Key.From("d")), CancellationToken.None);

//        var docs = result.ToArray();
//        Assert.Equal(3, docs.Length);
//        Assert.Contains(docs, d => d.Field<string>(Key.From("Field")).IfNone("") == "B");
//        Assert.Contains(docs, d => d.Field<string>(Key.From("Field")).IfNone("") == "C");
//        Assert.Contains(docs, d => d.Field<string>(Key.From("Field")).IfNone("") == "D");
//        Assert.DoesNotContain(docs, d => d.Field<string>(Key.From("Field")).IfNone("") == "A");
//        Assert.DoesNotContain(docs, d => d.Field<string>(Key.From("Field")).IfNone("") == "E");
//    }

//    [Fact]
//    public async Task FindAsync_WithRangeKeyFilter_EmptyResult_WhenNoDocumentsMatchRange()
//    {
//        var reader = CreateReader();
//        var hashKey = Key.From("h");

//        await InsertTestDocument(hashKey, Key.From("a"), Document.ConsData(Key.From("Field"), "A"));
//        await InsertTestDocument(hashKey, Key.From("b"), Document.ConsData(Key.From("Field"), "B"));

//        var result = await reader.FindAsync(hashKey, RangeKey.Lower(Key.From("z")), CancellationToken.None);

//        Assert.Empty(result);
//    }

//    [Fact]
//    public async Task FindAsync_WithRangeKeyFilter_WhenHashKeyDoesNotExist_ReturnsEmpty()
//    {
//        var reader = CreateReader();

//        var result = await reader.FindAsync(Key.From("nonexistent"), RangeKey.Unbound, CancellationToken.None);

//        Assert.Empty(result);
//    }

//    [Fact]
//    public async Task WhereAsync_WithPredicate_ReturnsFilteredResults()
//    {
//        var reader = CreateReader();
//        var hashKey = Key.From("h");

//        await InsertTestDocument(hashKey, Key.From("r1"), Document.ConsData(Key.From("Value"), 10));
//        await InsertTestDocument(hashKey, Key.From("r2"), Document.ConsData(Key.From("Value"), 20));
//        await InsertTestDocument(hashKey, Key.From("r3"), Document.ConsData(Key.From("Value"), 30));

//        var result = await reader.WhereAsync(hashKey, doc => doc.Field<int>(Key.From("Value")).IfNone(0) > 15, CancellationToken.None);

//        var docs = result.ToArray();
//        Assert.Equal(2, docs.Length);
//        Assert.Contains(docs, d => d.Field<int>(Key.From("Value")).IfNone(0) == 20);
//        Assert.Contains(docs, d => d.Field<int>(Key.From("Value")).IfNone(0) == 30);
//        Assert.DoesNotContain(docs, d => d.Field<int>(Key.From("Value")).IfNone(0) == 10);
//    }

//    [Fact]
//    public async Task WhereAsync_WithPredicateThatMatchesNone_ReturnsEmpty()
//    {
//        var reader = CreateReader();
//        var hashKey = Key.From("h");

//        await InsertTestDocument(hashKey, Key.From("r1"), Document.ConsData(Key.From("Value"), 10));
//        await InsertTestDocument(hashKey, Key.From("r2"), Document.ConsData(Key.From("Value"), 20));

//        var result = await reader.WhereAsync(hashKey, doc => doc.Field<int>(Key.From("Value")).IfNone(0) > 100, CancellationToken.None);

//        Assert.Empty(result);
//    }

//    [Fact]
//    public async Task WhereAsync_WhenHashKeyDoesNotExist_ReturnsEmpty()
//    {
//        var reader = CreateReader();

//        var result = await reader.WhereAsync(Key.From("nonexistent"), _ => true, CancellationToken.None);

//        Assert.Empty(result);
//    }

//    [Fact]
//    public async Task WhereAsync_WithCancelledToken_ThrowsTaskCanceledException()
//    {
//        var reader = CreateReader();

//        using var cts = new CancellationTokenSource();
//        cts.Cancel();

//        _ = await Assert.ThrowsAsync<TaskCanceledException>(() =>
//            reader.WhereAsync(Key.From("h"), _ => true, cts.Token));
//    }

//    [Fact]
//    public async Task FindAsync_HandlesMultipleHashKeys()
//    {
//        var reader = CreateReader();

//        await InsertTestDocument(Key.From("h1"), Key.From("r1"), Document.ConsData(Key.From("Field"), "H1R1"));
//        await InsertTestDocument(Key.From("h1"), Key.From("r2"), Document.ConsData(Key.From("Field"), "H1R2"));
//        await InsertTestDocument(Key.From("h2"), Key.From("r1"), Document.ConsData(Key.From("Field"), "H2R1"));
//        await InsertTestDocument(Key.From("h2"), Key.From("r2"), Document.ConsData(Key.From("Field"), "H2R2"));

//        var h1Results = await reader.FindAsync(Key.From("h1"), RangeKey.Unbound, CancellationToken.None);
//        var h2Results = await reader.FindAsync(Key.From("h2"), RangeKey.Unbound, CancellationToken.None);

//        var h1Docs = h1Results.ToArray();
//        var h2Docs = h2Results.ToArray();

//        Assert.Equal(2, h1Docs.Length);
//        Assert.Equal(2, h2Docs.Length);

//        foreach (var doc in h1Docs)
//            Assert.Equal(Key.From("h1"), doc.HashKey);

//        foreach (var doc in h2Docs)
//            Assert.Equal(Key.From("h2"), doc.HashKey);

//        Assert.Contains(h1Docs, d => d.Field<string>(Key.From("Field")).IfNone("") == "H1R1");
//        Assert.Contains(h1Docs, d => d.Field<string>(Key.From("Field")).IfNone("") == "H1R2");
//        Assert.Contains(h2Docs, d => d.Field<string>(Key.From("Field")).IfNone("") == "H2R1");
//        Assert.Contains(h2Docs, d => d.Field<string>(Key.From("Field")).IfNone("") == "H2R2");
//    }
//}
