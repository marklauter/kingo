using Kingo.Storage;
using Kingo.Storage.Indexing;

namespace Kingo.DictionaryEncoding.Tests;

public class KeyEncoderTests
{
    private readonly DocumentIndex index = DocumentIndex.Empty();
    private readonly DocumentReader reader;
    private readonly DocumentWriter writer;
    private readonly KeyEncoder encoder;

    public KeyEncoderTests()
    {
        reader = new DocumentReader(index);
        writer = new DocumentWriter(index);
        encoder = new KeyEncoder(reader, writer);
    }

    [Fact]
    public void PackWhenCalledFirstTimeGeneratesAndPacksIds()
    {
        var resource = new Resource("mynamespace", "myresource");
        var relationship = Relationship.From("myrelationship");
        var token = CancellationToken.None;

        var result = encoder.Pack(resource, relationship, token);

        Assert.True(result.IsRight);

        // Pack again to ensure the same key is returned
        var secondResult = encoder.Pack(resource, relationship, token);
        Assert.Equal(result, secondResult);
    }

    [Fact]
    public void PackThenUnpackReturnsOriginalIds()
    {
        var resource = new Resource("mynamespace", "myresource");
        var relationship = Relationship.From("myrelationship");
        var token = CancellationToken.None;

        var result = encoder.Pack(resource, relationship, token);

        Assert.True(result.IsRight);

        _ = result.IfRight(packedKey =>
        {
            var (nsId, relId, resId) = KeyEncoder.Unpack(packedKey);
            Assert.Equal(1UL, nsId);
            Assert.Equal(1UL, resId);
            Assert.Equal(1UL, relId);
        });
    }

    [Fact]
    public void PackWithMultipleResourcesGeneratesCorrectIds()
    {
        var resource1 = new Resource("ns1", "res1");
        var relationship1 = Relationship.From("rel1");
        var token = CancellationToken.None;

        var result1 = encoder.Pack(resource1, relationship1, token);
        Assert.True(result1.IsRight);

        _ = result1.IfRight(packedKey1 =>
        {
            var (nsId1, relId1, resId1) = KeyEncoder.Unpack(packedKey1);
            Assert.Equal(1UL, nsId1);
            Assert.Equal(1UL, resId1);
            Assert.Equal(1UL, relId1);
        });

        var resource2 = new Resource("ns2", "res2");
        var relationship2 = Relationship.From("rel2");

        var result2 = encoder.Pack(resource2, relationship2, token);
        Assert.True(result2.IsRight);

        _ = result2.IfRight(packedKey2 =>
        {
            var (nsId2, relId2, resId2) = KeyEncoder.Unpack(packedKey2);
            Assert.Equal(2UL, nsId2);
            Assert.Equal(2UL, resId2);
            Assert.Equal(2UL, relId2);
        });

        // Pack the first one again to make sure we get the same key
        var result3 = encoder.Pack(resource1, relationship1, token);
        Assert.True(result3.IsRight);
        Assert.Equal(result1, result3);
    }

    [Fact]
    public void PackWhenTokenIsCancelledReturnsTimeoutError()
    {
        var resource = new Resource("mynamespace", "myresource");
        var relationship = Relationship.From("myrelationship");
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var result = encoder.Pack(resource, relationship, tokenSource.Token);

        Assert.True(result.IsLeft);
        _ = result.IfLeft(error => Assert.Equal(ErrorCodes.TimeoutError, error.Code));
    }
}
