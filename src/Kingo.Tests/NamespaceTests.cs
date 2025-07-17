namespace Kingo.Tests;

public sealed class NamespaceTests
{
    [Fact]
    public void From_Throws_IfValueIsNull() =>
        Assert.Throws<ArgumentNullException>(() => NamespaceIdentifier.From(null!));

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void From_Throws_IfValueIsWhitespace(string value) =>
        Assert.Throws<ArgumentException>(() => NamespaceIdentifier.From(value));

    [Theory]
    [InlineData("a-b")]
    [InlineData("a$b")]
    [InlineData("a b")]
    [InlineData("a.b")]
    public void From_Throws_IfValueContainsInvalidCharacters(string value)
    {
        var exception = Assert.Throws<ArgumentException>(nameof(value), () => NamespaceIdentifier.From(value));
        Assert.StartsWith("value contains invalid characters", exception.Message);
        Assert.Contains(value, exception.Message);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("_a")]
    [InlineData("a_b")]
    [InlineData("a0")]
    public void From_ReturnsPolicyIfValueIsValid(string value)
    {
        var policy = NamespaceIdentifier.From(value);
        Assert.Equal(value, policy.ToString());
    }

    [Fact]
    public void Equals_ReturnsTrue_IfValuesAreEqual()
    {
        var policy1 = NamespaceIdentifier.From("a");
        var policy2 = NamespaceIdentifier.From("a");

        Assert.True(policy1.Equals(policy2));
        Assert.True(policy1.Equals((object)policy2));
        Assert.True(policy1 == policy2);
        Assert.False(policy1 != policy2);
        Assert.True(policy1.Equals("a"));
    }

    [Fact]
    public void Equals_ReturnsFalse_IfValuesAreNotEqual()
    {
        var policy1 = NamespaceIdentifier.From("a");
        var policy2 = NamespaceIdentifier.From("b");

        Assert.False(policy1.Equals(policy2));
        Assert.False(policy1.Equals((object)policy2));
        Assert.False(policy1 == policy2);
        Assert.True(policy1 != policy2);
        Assert.False(policy1.Equals("b"));
    }

    [Fact]
    public void CompareTo_ReturnsCorrectValue()
    {
        var policy1 = NamespaceIdentifier.From("a");
        var policy2 = NamespaceIdentifier.From("b");

        Assert.True(policy1.CompareTo(policy2) < 0);
        Assert.True(policy1 < policy2);
        Assert.True(policy1 <= policy2);
        Assert.False(policy1 > policy2);
        Assert.False(policy1 >= policy2);
        Assert.True(policy1.CompareTo("b") < 0);
    }

    [Fact]
    public void GetHashCode_ReturnsSameValue_ForEqualPolicies()
    {
        var policy1 = NamespaceIdentifier.From("a");
        var policy2 = NamespaceIdentifier.From("a");
        Assert.Equal(policy1.GetHashCode(), policy2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        const string value = "a_b_c";
        var policy = NamespaceIdentifier.From(value);
        Assert.Equal(value, policy.ToString());
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        const string value = "a_b_c";
        var policy = NamespaceIdentifier.From(value);
        string s = policy;
        Assert.Equal(value, s);
    }

    [Fact]
    public void ImplicitConversion_ToPolicy_ReturnsPolicy()
    {
        const string value = "a_b_c";
        NamespaceIdentifier policy = value;
        Assert.Equal(value, policy.ToString());
    }

    [Fact]
    public void Empty_Throws() =>
        Assert.Throws<ArgumentException>(() => NamespaceIdentifier.Empty());
}
