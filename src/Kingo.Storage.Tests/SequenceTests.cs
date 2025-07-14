using Kingo.Storage.Clocks;
using Kingo.Storage.InMemory.Indexing;
using Kingo.Storage.Keys;
using LanguageExt;

namespace Kingo.Storage.Tests;

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
                Fail: error => Assert.Equal(StorageErrorCodes.TimeoutError, error.Code),
                Succ: _ => Assert.Fail("Expected an error but got a success value."));
    }
}
