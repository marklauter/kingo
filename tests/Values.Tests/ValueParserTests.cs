using Results;

namespace Values.Tests;

public sealed class ValueParserTests
{
    [Theory]
    [InlineData("a")]
    [InlineData("abc")]
    public void TryParse_ValidInput_ReturnsTrueAndPopulatesOut(string input)
    {
        Assert.True(ValueParser.TryParse<TestValue>(input, out var parsed));
        Assert.Equal(input, parsed.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ABC")]
    [InlineData("abc1")]
    [InlineData("a b")]
    public void TryParse_NullOrInvalidInput_ReturnsFalseAndOutIsDefault(string? input)
    {
        // null reaches TryParse through reflection callers (see IParse); it projects to false + default
        Assert.False(ValueParser.TryParse<TestValue>(input!, out var parsed));
        Assert.Equal(default, parsed);
    }

    [Fact]
    public void TryParse_ProducesSameValueAsParse()
    {
        var viaParse = Assert.IsType<Result<TestValue>.Success>(TestValue.Parse("abc")).Value;
        Assert.True(ValueParser.TryParse<TestValue>("abc", out var viaTryParse));
        Assert.Equal(viaParse, viaTryParse);
    }

    [Fact]
    public void ImplementorDelegation_RoundTrips()
    {
        Assert.True(TestValue.TryParse("abc", out var parsed));
        Assert.Equal("abc", parsed.Value);
        Assert.False(TestValue.TryParse("NOPE", out _));
    }
}
