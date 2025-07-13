using Kingo.Storage.Keys;

namespace Kingo.Storage.Tests.Keys;

public sealed class KeyRangeTests
{
    [Fact]
    public void Unbound_ReturnsUnboundInstance() => Assert.IsType<Unbound>(RangeKey.Unbound);

    [Fact]
    public void LowerBound_ReturnsLowerBoundInstance()
    {
        var key = Key.From("a");
        var since = RangeKey.Since<Key>(key);
        _ = Assert.IsType<LowerBound<Key>>(since);
        Assert.Equal(key, since.Key);
    }

    [Fact]
    public void UpperBound_ReturnsUpperBoundInstance()
    {
        var key = Key.From("a");
        var until = RangeKey.Until<Key>(key);
        _ = Assert.IsType<UpperBound<Key>>(until);
        Assert.Equal(key, until.Key);
    }

    [Fact]
    public void Between_ReturnsBetweenInstance()
    {
        var fromKey = Key.From("a");
        var toKey = Key.From("b");
        var between = RangeKey.Between<Key>(fromKey, toKey);
        _ = Assert.IsType<Between<Key>>(between);
        Assert.Equal(fromKey, between.LowerBound);
        Assert.Equal(toKey, between.UpperBound);
    }
}
