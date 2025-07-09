using Kingo.Storage.Clocks;
using Kingo.Storage.Indexing;
using Kingo.Storage.Keys;

namespace Kingo.Storage.Tests;

public sealed class SequenceTests
{
    private readonly DocumentIndex<Key> index = DocumentIndex.Empty<Key>();

    private Sequence<int> Sequence() =>
        new(new(index), new(index));

    private readonly Key seqName = Key.From("my-seq");

    [Fact]
    public void Next_ReturnsNextNumberInSequence_WhenCalled()
    {
        var sequence = Sequence();

        Assert.Equal(1, sequence.Next($"{seqName}-1", CancellationToken.None));
        Assert.Equal(2, sequence.Next($"{seqName}-1", CancellationToken.None));
        Assert.Equal(1, sequence.Next($"{seqName}-2", CancellationToken.None));
        Assert.Equal(2, sequence.Next($"{seqName}-2", CancellationToken.None));
    }

    [Fact]
    public void Next_ReturnsNextNumberInSequence_WhenCalled_WithNewSequence()
    {
        Assert.Equal(1, Sequence().Next(seqName, CancellationToken.None));
        Assert.Equal(2, Sequence().Next(seqName, CancellationToken.None));
        Assert.Equal(3, Sequence().Next(seqName, CancellationToken.None));
        Assert.Equal(4, Sequence().Next(seqName, CancellationToken.None));
    }

    [Fact]
    public void Next_ReturnsTimeoutError_WhenTokenIsCancelled()
    {
        var sequence = Sequence();

        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var result = sequence.Next(seqName, tokenSource.Token)
            .Match(
                Left: error => Assert.Equal(ErrorCodes.TimeoutError, error.Code),
                Right: _ => Assert.Fail("Expected an error but got a success value."));
    }
}
