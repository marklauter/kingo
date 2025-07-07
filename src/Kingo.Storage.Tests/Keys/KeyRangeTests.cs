
using Kingo.Storage.Keys;

namespace Kingo.Storage.Tests.Keys;

public sealed class KeyRangeTests
{
    [Fact]
    public void Unbound_ReturnsUnboundInstance() => Assert.IsType<Unbound>(RangeKey.Unbound);

    [Fact]
    public void Since_ReturnsSinceInstance()
    {
        var key = Key.From("a");
        var since = RangeKey.Since<Key>(key);
        _ = Assert.IsType<Since<Key>>(since);
        Assert.Equal(key, since.FromKey);
    }

    [Fact]
    public void Until_ReturnsUntilInstance()
    {
        var key = Key.From("a");
        var until = RangeKey.Until<Key>(key);
        _ = Assert.IsType<Until<Key>>(until);
        Assert.Equal(key, until.ToKey);
    }

    [Fact]
    public void Between_ReturnsBetweenInstance()
    {
        var fromKey = Key.From("a");
        var toKey = Key.From("b");
        var between = RangeKey.Between<Key>(fromKey, toKey);
        _ = Assert.IsType<Between<Key>>(between);
        Assert.Equal(fromKey, between.FromKey);
        Assert.Equal(toKey, between.ToKey);
    }
}
