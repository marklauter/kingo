using Kingo.Storage.Keys;
using Kingo.Storage.Sqlite;

namespace Kingo.Storage.Tests.Sqlite;

public sealed class SequenceTests
    : SqliteTests
{
    public SequenceTests()
    {
        AddMigration(
            "create-table-seq_32",
            """
                CREATE TABLE seq_32 (
                    key TEXT PRIMARY KEY,
                    value INTEGER NOT NULL
                )
            """);

        AddMigration(
            "create-table-seq_64",
            """
                CREATE TABLE seq_64 (
                    key TEXT PRIMARY KEY,
                    value INTEGER NOT NULL
                )
            """);
    }

    private readonly Key seqName = Key.From("my_seq");

    private SqliteSequence<int> CreateIntSequence() =>
        new(Context, "seq_32");

    private SqliteSequence<long> CreateLongSequence() =>
        new(Context, "seq_64");

    [Fact]
    public async Task NextAsync_ReturnsOne_WhenSequenceIsNew()
    {
        var sequence = CreateIntSequence();

        var result = await sequence.NextAsync(seqName, CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task NextAsync_ReturnsConsecutiveNumbers_WhenCalledMultipleTimes()
    {
        var sequence = CreateIntSequence();

        Assert.Equal(1, await sequence.NextAsync(seqName, CancellationToken.None));
        Assert.Equal(2, await sequence.NextAsync(seqName, CancellationToken.None));
        Assert.Equal(3, await sequence.NextAsync(seqName, CancellationToken.None));
    }

    [Fact]
    public async Task NextAsync_ReturnsIndependentSequences_WhenUsingDifferentNames()
    {
        var sequence = CreateIntSequence();

        Assert.Equal(1, await sequence.NextAsync("seq1", CancellationToken.None));
        Assert.Equal(1, await sequence.NextAsync("seq2", CancellationToken.None));
        Assert.Equal(2, await sequence.NextAsync("seq1", CancellationToken.None));
        Assert.Equal(2, await sequence.NextAsync("seq2", CancellationToken.None));
    }

    [Fact]
    public async Task NextAsync_ReturnsIndependentSequences_WhenUsingDifferentNames_AndGenerators()
    {
        var sequence1 = CreateIntSequence();
        var sequence2 = CreateIntSequence();

        Assert.Equal(1, await sequence1.NextAsync("seq1", CancellationToken.None));
        Assert.Equal(1, await sequence2.NextAsync("seq2", CancellationToken.None));
        Assert.Equal(2, await sequence1.NextAsync("seq1", CancellationToken.None));
        Assert.Equal(2, await sequence2.NextAsync("seq2", CancellationToken.None));
    }

    [Fact]
    public async Task NextAsync_ThrowsTaskCanceledException_WhenTokenIsCanceled()
    {

        var sequence = CreateIntSequence();
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        _ = await Assert.ThrowsAsync<OperationCanceledException>(() => sequence.NextAsync(seqName, tokenSource.Token));
    }

    [Fact]
    public async Task NextAsync_HandlesHighConcurrency_WhenMultipleTasksAccess()
    {
        var sequence = CreateIntSequence();
        var tasks = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => sequence.NextAsync(seqName, CancellationToken.None)))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        var distinctValues = results.Distinct().OrderBy(x => x).ToArray();

        Assert.Equal(100, distinctValues.Length);
        Assert.Equivalent(Enumerable.Range(1, 100), distinctValues);
    }

    [Fact]
    public async Task NextAsync_WorksWithLongType_WhenCalled()
    {
        var sequence = CreateLongSequence();

        Assert.Equal(1L, await sequence.NextAsync(seqName, CancellationToken.None));
        Assert.Equal(2L, await sequence.NextAsync(seqName, CancellationToken.None));
    }

    [Fact]
    public async Task NextAsync_PersistsState_WhenSequenceIsRecreated()
    {
        Assert.Equal(1, await CreateIntSequence().NextAsync(seqName, CancellationToken.None));
        Assert.Equal(2, await CreateIntSequence().NextAsync(seqName, CancellationToken.None));
        Assert.Equal(3, await CreateIntSequence().NextAsync(seqName, CancellationToken.None));
    }

    [Fact]
    public async Task NextAsync_HandlesConcurrentAccess_WithMultipleSequenceInstances()
    {
        var sequence1 = CreateIntSequence();
        var sequence2 = CreateIntSequence();

        var t = new List<Task<int>>();
        for (var i = 0; i < 50; i++)
        {
            t.Add(sequence1.NextAsync(seqName, CancellationToken.None));
            t.Add(sequence2.NextAsync(seqName, CancellationToken.None));
        }

        var r = await Task.WhenAll(t);

        Assert.Equal(100, r.Length);
        Assert.Equivalent(Enumerable.Range(1, 100), r);
    }

    [Fact]
    public async Task NextAsync_MaintainsConsistency_UnderHighContention()
    {
        // this will achieve between 40 and 60 concurrent threads before crashing on the rocks of Sqlite, which is fine
        var sequence = CreateIntSequence();
        var concurrencyLevel = 50;
        var iterationsPerTask = 50;

        var tasks = Enumerable.Range(0, concurrencyLevel)
            .Select(_ => Task.Run(async () =>
            {
                var results = new List<int>();
                for (var i = 0; i < iterationsPerTask; i++)
                    results.Add(await sequence.NextAsync(seqName, CancellationToken.None));
                return results;
            }))
            .ToArray();

        var allTaskResults = await Task.WhenAll(tasks);
        var allResults = allTaskResults.SelectMany(x => x).OrderBy(x => x).ToArray();

        var expectedCount = concurrencyLevel * iterationsPerTask;
        Assert.Equal(expectedCount, allResults.Length);
        Assert.Equivalent(Enumerable.Range(1, expectedCount), allResults);
        Assert.Equal(expectedCount, allResults.Distinct().Count());
    }
}

