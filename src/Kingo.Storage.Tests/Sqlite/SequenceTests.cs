using FluentAssertions;
using Kingo.Storage.Keys;
using Kingo.Storage.Sqlite;
using Microsoft.Data.Sqlite;

namespace Kingo.Storage.Tests.Sqlite;

public sealed class SequenceTests
    : IDisposable
{
    private readonly string connectionString = $"Data Source={Guid.NewGuid()}.sqlite";

    private readonly SqliteConnection connection;
    private readonly Key seqName = Key.From("my_seq");

    public SequenceTests()
    {
        connection = new SqliteConnection(connectionString);
        connection.Open();
        CreateSequenceTable();
    }

    private SqliteConnection NewConnection()
    {
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return connection;
    }

    private void CreateSequenceTable()
    {
        var createTable = """
            CREATE TABLE IF NOT EXISTS seq (
                hashkey TEXT PRIMARY KEY,
                value INTEGER NOT NULL
            )
            """;
        using var command = new SqliteCommand(createTable, connection);
        _ = command.ExecuteNonQuery();
    }

    private Sequence<int> CreateSequence(Key name) => new(NewConnection(), name);

    [Fact]
    public async Task NextAsync_ReturnsOne_WhenSequenceIsNew()
    {
        var sequence = CreateSequence(seqName);

        var result = await sequence.NextAsync(CancellationToken.None);

        _ = result.Should().Be(1);
    }

    [Fact]
    public async Task NextAsync_ReturnsConsecutiveNumbers_WhenCalledMultipleTimes()
    {
        var sequence = CreateSequence(seqName);

        _ = (await sequence.NextAsync(CancellationToken.None)).Should().Be(1);
        _ = (await sequence.NextAsync(CancellationToken.None)).Should().Be(2);
        _ = (await sequence.NextAsync(CancellationToken.None)).Should().Be(3);
    }

    [Fact]
    public async Task NextAsync_ReturnsIndependentSequences_WhenUsingDifferentNames()
    {
        var sequence1 = CreateSequence(Key.From("seq1"));
        var sequence2 = CreateSequence(Key.From("seq2"));

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
        var sequence = CreateSequence(seqName);
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        _ = await Assert.ThrowsAsync<TaskCanceledException>(() => sequence.NextAsync(tokenSource.Token));
    }

    [Fact]
    public async Task NextAsync_HandlesHighConcurrency_WhenMultipleTasksAccess()
    {
        var sequence = CreateSequence(seqName);
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
        var sequence = new Sequence<long>(connection, seqName);

        var first = await sequence.NextAsync(CancellationToken.None);
        var second = await sequence.NextAsync(CancellationToken.None);

        _ = first.Should().Be(1L);
        _ = second.Should().Be(2L);
    }

    [Fact]
    public async Task NextAsync_WorksWithDecimalType_WhenCalled()
    {
        var sequence = new Sequence<decimal>(connection, seqName);

        var first = await sequence.NextAsync(CancellationToken.None);
        var second = await sequence.NextAsync(CancellationToken.None);

        _ = first.Should().Be(1M);
        _ = second.Should().Be(2M);
    }

    [Fact]
    public async Task NextAsync_PersistsState_WhenSequenceIsRecreated()
    {
        var sequence1 = CreateSequence(seqName);
        _ = await sequence1.NextAsync(CancellationToken.None);
        _ = await sequence1.NextAsync(CancellationToken.None);

        var sequence2 = CreateSequence(seqName);
        var result = await sequence2.NextAsync(CancellationToken.None);

        _ = result.Should().Be(3);
    }

    [Fact]
    public async Task NextAsync_HandlesConcurrentAccess_WithMultipleSequenceInstances()
    {
        var sequence1 = CreateSequence(seqName);
        var sequence2 = CreateSequence(seqName);

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
        var concurrencyLevel = 500;

        var tasks = Enumerable.Range(0, concurrencyLevel)
            .Select(_ => Task.Run(async () => await CreateSequence(seqName).NextAsync(CancellationToken.None)))
            .ToArray();

        var allResults = await Task.WhenAll(tasks);

        var expectedCount = concurrencyLevel;
        _ = allResults.Should().HaveCount(expectedCount);
        _ = allResults.Should().BeEquivalentTo(Enumerable.Range(1, expectedCount));
        _ = allResults.Should().OnlyHaveUniqueItems();
    }

    public void Dispose() => connection?.Dispose();
}
