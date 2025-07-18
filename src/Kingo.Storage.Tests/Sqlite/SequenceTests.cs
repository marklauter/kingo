using FluentAssertions;
using Kingo.Storage.Db;
using Kingo.Storage.Keys;
using Kingo.Storage.Sqlite;
using LanguageExt;

namespace Kingo.Storage.Tests.Sqlite;

public sealed class SequenceTests
    : IAsyncLifetime
{
    public SequenceTests() =>
        context = new(
            new SqliteConnectionFactory(
                new($"Data Source={dbName}", false)));

    private readonly string dbName = $"{Guid.NewGuid()}.sqlite";

    public async Task InitializeAsync() =>
        await context.ApplyMigrationsAsync(migrations, CancellationToken.None);

    public Task DisposeAsync()
    {
        context.ClearAllPools();
        if (File.Exists(dbName))
            File.Delete(dbName);

        return Task.CompletedTask;
    }

    private readonly DbContext context;

    private readonly Migrations migrations = Migrations.Cons()
        .Add(
        "create-table-seq_32",
        """
            CREATE TABLE seq_32 (
                key TEXT PRIMARY KEY,
                value INTEGER NOT NULL
            )
        """)
        .Add(
        "create-table-seq_64",
        """
            CREATE TABLE seq_64 (
                key TEXT PRIMARY KEY,
                value BIGINT NOT NULL
            )
        """);

    private readonly Key seqName = Key.From("my_seq");

    private Sequence<int> CreateIntSequence(Key name) => new(context, "seq_32", name);
    private Sequence<long> CreateLongSequence(Key name) => new(context, "seq_64", name);

    [Fact]
    public async Task NextAsync_ReturnsOne_WhenSequenceIsNew()
    {
        var sequence = CreateIntSequence(seqName);

        var result = await sequence.NextAsync(CancellationToken.None);

        _ = result.Should().Be(1);
    }

    [Fact]
    public async Task NextAsync_ReturnsConsecutiveNumbers_WhenCalledMultipleTimes()
    {

        var sequence = CreateIntSequence(seqName);

        _ = (await sequence.NextAsync(CancellationToken.None)).Should().Be(1);
        _ = (await sequence.NextAsync(CancellationToken.None)).Should().Be(2);
        _ = (await sequence.NextAsync(CancellationToken.None)).Should().Be(3);
    }

    [Fact]
    public async Task NextAsync_ReturnsIndependentSequences_WhenUsingDifferentNames()
    {

        var sequence1 = CreateIntSequence(Key.From("seq1"));
        var sequence2 = CreateIntSequence(Key.From("seq2"));

        var seq1_first = await sequence1.NextAsync(CancellationToken.None);
        var seq2_first = await sequence2.NextAsync(CancellationToken.None);
        var seq1_second = await sequence1.NextAsync(CancellationToken.None);
        var seq2_second = await sequence2.NextAsync(CancellationToken.None);

        _ = seq1_first.Should().Be(1);
        _ = seq2_first.Should().Be(1);
        _ = seq1_second.Should().Be(2);
        _ = seq2_second.Should().Be(2);
    }

    [Fact]
    public async Task NextAsync_ThrowsOperationCanceledException_WhenTokenIsCanceled()
    {

        var sequence = CreateIntSequence(seqName);
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        _ = await Assert.ThrowsAsync<OperationCanceledException>(() => sequence.NextAsync(tokenSource.Token));
    }

    [Fact]
    public async Task NextAsync_HandlesHighConcurrency_WhenMultipleTasksAccess()
    {

        var sequence = CreateIntSequence(seqName);
        var tasks = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => sequence.NextAsync(CancellationToken.None)))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        var distinctValues = results.Distinct().OrderBy(x => x).ToArray();

        _ = distinctValues.Should().HaveCount(100);
        _ = distinctValues.Should().BeEquivalentTo(Enumerable.Range(1, 100));
    }

    [Fact]
    public async Task NextAsync_WorksWithLongType_WhenCalled()
    {

        var sequence = CreateLongSequence(seqName);

        var first = await sequence.NextAsync(CancellationToken.None);
        var second = await sequence.NextAsync(CancellationToken.None);

        _ = first.Should().Be(1L);
        _ = second.Should().Be(2L);
    }

    [Fact]
    public async Task NextAsync_PersistsState_WhenSequenceIsRecreated()
    {

        var sequence1 = CreateIntSequence(seqName);
        _ = await sequence1.NextAsync(CancellationToken.None);
        _ = await sequence1.NextAsync(CancellationToken.None);

        var sequence2 = CreateIntSequence(seqName);
        var result = await sequence2.NextAsync(CancellationToken.None);

        _ = result.Should().Be(3);
    }

    [Fact]
    public async Task NextAsync_HandlesConcurrentAccess_WithMultipleSequenceInstances()
    {

        var sequence1 = CreateIntSequence(seqName);
        var sequence2 = CreateIntSequence(seqName);

        var task1 = Task.Run(async () =>
        {
            var results = new List<int>();
            for (var i = 0; i < 50; i++)
                results.Add(await sequence1.NextAsync(CancellationToken.None));
            return results;
        });

        var task2 = Task.Run(async () =>
        {
            var results = new List<int>();
            for (var i = 0; i < 50; i++)
                results.Add(await sequence2.NextAsync(CancellationToken.None));
            return results;
        });

        var results1 = await task1;
        var results2 = await task2;
        var allResults = results1.Concat(results2).OrderBy(x => x).ToArray();

        _ = allResults.Should().HaveCount(100);
        _ = allResults.Should().BeEquivalentTo(Enumerable.Range(1, 100));
    }

    [Fact]
    public async Task NextAsync_MaintainsConsistency_UnderHighContention()
    {

        var sequence = CreateIntSequence(seqName);
        var concurrencyLevel = 20;
        var iterationsPerTask = 50;

        var tasks = Enumerable.Range(0, concurrencyLevel)
            .Select(_ => Task.Run(async () =>
            {
                var results = new List<int>();
                for (var i = 0; i < iterationsPerTask; i++)
                    results.Add(await sequence.NextAsync(CancellationToken.None));
                return results;
            }))
            .ToArray();

        var allTaskResults = await Task.WhenAll(tasks);
        var allResults = allTaskResults.SelectMany(x => x).OrderBy(x => x).ToArray();

        var expectedCount = concurrencyLevel * iterationsPerTask;
        _ = allResults.Should().HaveCount(expectedCount);
        _ = allResults.Should().BeEquivalentTo(Enumerable.Range(1, expectedCount));
        _ = allResults.Should().OnlyHaveUniqueItems();
    }
}

