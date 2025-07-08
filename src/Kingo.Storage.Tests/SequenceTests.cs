using Kingo.Storage.Clocks;
using Kingo.Storage.Indexing;
using Kingo.Storage.Keys;
using LanguageExt;

namespace Kingo.Storage.Tests;

public sealed class SequenceTests
{
    private readonly DocumentIndex<Key> index = DocumentIndex.Empty<Key>();

    private (DocumentReader<Key> reader, DocumentWriter<Key> writer) ReaderWriter() =>
        (new(index), new(index));

    [Fact]
    public void Next_ReturnsNextNumberInSequence_WhenCalled()
    {
        var (reader, writer) = ReaderWriter();
        var sequence = new Sequence<int>(reader, writer);
        var seqName = Key.From("my-seq");

        var result1 = sequence.Next(seqName, CancellationToken.None);
        var result2 = sequence.Next(seqName, CancellationToken.None);

        Assert.Equal(Prelude.Right(1), result1);
        Assert.Equal(Prelude.Right(2), result2);
    }

    [Fact]
    public void Next_ReturnsTimeoutError_WhenTokenIsCancelled()
    {
        var (reader, writer) = ReaderWriter();
        var sequence = new Sequence<int>(reader, writer);
        var seqName = Key.From("my-seq");
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var result = sequence.Next(seqName, tokenSource.Token);

        _ = result.IfLeft(error => Assert.Equal(ErrorCodes.TimeoutError, error.Code));
        _ = result.IfRight(_ => Assert.Fail("Expected an error but got a success value."));
    }

    [Fact]
    public void Next_Retries_OnVersionConflictError()
    {
        var (reader, writer) = ReaderWriter();
        var sequence = new Sequence<int>(reader, writer);
        var seqName = Key.From("my-seq");

        // first call to create the sequence
        _ = sequence.Next(seqName, CancellationToken.None);

        // Manually update the document to create a version conflict
        var maybeDoc = reader.Find(Key.From($"seq/{seqName}"));

        _ = maybeDoc.Match(
            Some: doc => _ = writer.Update(doc with { Version = doc.Version.Tick() }, CancellationToken.None),
            None: () => Assert.Fail("The document should exist at this point."));

        var result = sequence.Next(seqName, CancellationToken.None);

        Assert.Equal(Prelude.Right(2), result);
    }
}
