using Results;
using System.Reflection;

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
    [InlineData("")]
    [InlineData("ABC")]
    [InlineData("abc1")]
    [InlineData("a b")]
    public void TryParse_InvalidInput_ReturnsFalseAndOutIsDefault(string input)
    {
        Assert.False(ValueParser.TryParse<TestValue>(input, out var parsed));
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

    [Fact]
    public void TryParse_IsDeclaredOnImplementorType()
    {
        // Reflection-based pipelines (ASP.NET Core parameter binding) discover TryParse on the concrete type; an inherited interface member would not be found.
        var method = typeof(TestValue).GetMethod(
            nameof(TestValue.TryParse),
            BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,
            [typeof(string), typeof(TestValue).MakeByRefType()]);
        Assert.NotNull(method);
    }
}
