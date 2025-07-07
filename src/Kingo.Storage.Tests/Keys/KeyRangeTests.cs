
using Kingo.Storage.Keys;

namespace Kingo.Storage.Tests.Keys;

public sealed class KeyRangeTests
{
    [Fact]
    public void Unbound_ReturnsUnboundInstance() => Assert.IsType<Unbound>(Storage.Keys.RangeKey.Unbound);

    [Fact]
    public void Since_ReturnsSinceInstance()
    {
        var key = Key.From("a");
        var since = Storage.Keys.RangeKey.Since(key);
        _ = Assert.IsType<Since>(since);
        Assert.Equal(key, since.FromKey);
    }

    [Fact]
    public void Until_ReturnsUntilInstance()
    {
        var key = Key.From("a");
        var until = Storage.Keys.RangeKey.Until(key);
        _ = Assert.IsType<Until>(until);
        Assert.Equal(key, until.ToKey);
    }

    [Fact]
    public void Between_ReturnsBetweenInstance()
    {
        var fromKey = Key.From("a");
        var toKey = Key.From("b");
        var between = Storage.Keys.RangeKey.Between(fromKey, toKey);
        _ = Assert.IsType<Between>(between);
        Assert.Equal(fromKey, between.FromKey);
        Assert.Equal(toKey, between.ToKey);
    }
}
