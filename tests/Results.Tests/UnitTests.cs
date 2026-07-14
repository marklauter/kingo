namespace Results.Tests;

public sealed class UnitTests
{
    [Fact]
    public void Value_ReturnsSingleton() => Assert.Equal(Unit.Value, Unit.Value);

    [Fact]
    public void Default_EqualsValue() => Assert.Equal(Unit.Value, default);

    [Fact]
    public void TwoInstances_AreEqual()
    {
        var a = new Unit();
        var b = new Unit();
        Assert.Equal(a, b);
    }
}
