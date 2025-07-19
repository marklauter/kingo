using dead_code.Storage.InMemory;
using dead_code.Storage.InMemory.Indexing;
using Kingo.Storage.Keys;
using LanguageExt;
using LanguageExt.Common;

namespace dead_code.Storage.Tests;

public sealed class SequenceTests
{
    private readonly Index<Key> index = InMemory.Indexing.Index.Empty<Key>();

    private Sequence<int> Sequence() =>
        new(new(index), new(index));

    private readonly Key seqName = Key.From("my_seq");

    [Fact]
    public void Next_ReturnsNextNumberInSequence_WhenCalled()
    {
        var sequence = Sequence();

        Assert.Equal(1, sequence.Next($"{seqName}/1", CancellationToken.None).Run());
        Assert.Equal(2, sequence.Next($"{seqName}/1", CancellationToken.None).Run());
        Assert.Equal(1, sequence.Next($"{seqName}/2", CancellationToken.None).Run());
        Assert.Equal(2, sequence.Next($"{seqName}/2", CancellationToken.None).Run());
    }

    [Fact]
    public void Next_ReturnsNextNumberInSequence_WhenCalled_WithNewSequence()
    {
        Assert.Equal(1, Sequence().Next(seqName, CancellationToken.None).Run());
        Assert.Equal(2, Sequence().Next(seqName, CancellationToken.None).Run());
        Assert.Equal(3, Sequence().Next(seqName, CancellationToken.None).Run());
        Assert.Equal(4, Sequence().Next(seqName, CancellationToken.None).Run());
    }

    [Fact]
    public void Next_ReturnsTimeoutError_WhenTokenIsCancelled()
    {
        var sequence = Sequence();

        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var result = sequence.Next(seqName, tokenSource.Token).Run()
            .Match(
                Fail: error => Assert.Equal(Errors.TimedOutCode, error.Code),
                Succ: _ => Assert.Fail("Expected an error but got a success value."));
    }

    [Fact]
    public async Task Next_HandlesHighConcurrency_WhenMultipleThreadsAccess()
    {
        var sequence = Sequence();
        var tasks = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => sequence.Next(seqName, CancellationToken.None).Run(EnvIO.New(token: CancellationToken.None))))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        var successfulValues = results
            .Select(r => r.Match(
                Succ: value => value,
                Fail: e => throw new InvalidOperationException($"Sequence generation failed {e.Message}")))
            .Distinct()
            .ToArray();

        Assert.Equal(100, successfulValues.Length);
        Assert.Equal(Enumerable.Range(1, 100), successfulValues.OrderBy(x => x));
    }

    [Fact]
    public void Next_WorksWithLongType_WhenCalled()
    {
        var sequence = new Sequence<long>(new DocumentReader<Key>(index), new DocumentWriter<Key>(index));

        Assert.Equal(1L, sequence.Next(seqName, CancellationToken.None).Run());
        Assert.Equal(2L, sequence.Next(seqName, CancellationToken.None).Run());
    }
}
