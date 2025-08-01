namespace Kingo.Policies.Tests;

public sealed class RelationIdentifierTests
{
    [Fact]
    public void From_Throws_IfValueIsNull() =>
        Assert.Throws<ArgumentNullException>(() => RelationIdentifier.From(null!));

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void From_Throws_IfValueIsWhitespace(string value) =>
        Assert.Throws<ArgumentException>(() => RelationIdentifier.From(value));

    [Theory]
    [InlineData("a-b")]
    [InlineData("a$b")]
    [InlineData("a b")]
    [InlineData("a.b")]
    public void From_Throws_IfValueContainsInvalidCharacters(string value)
    {
        var exception = Assert.Throws<ArgumentException>("value", () => RelationIdentifier.From(value));
        Assert.StartsWith("value contains invalid characters", exception.Message);
        Assert.Contains(value, exception.Message);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("a_b")]
    [InlineData("a0")]
    [InlineData("...")]
    public void From_ReturnsRelationship_IfValueIsValid(string value)
    {
        var relationship = RelationIdentifier.From(value);
        Assert.Equal(value, relationship.ToString());
    }

    [Fact]
    public void Equals_ReturnsTrue_IfValuesAreEqual()
    {
        var relationship1 = RelationIdentifier.From("a");
        var relationship2 = RelationIdentifier.From("a");

        Assert.True(relationship1.Equals(relationship2));
        Assert.True(relationship1.Equals((object)relationship2));
        Assert.True(relationship1 == relationship2);
        Assert.False(relationship1 != relationship2);
        Assert.True(relationship1.Equals("a"));
    }

    [Fact]
    public void Equals_ReturnsFalse_IfValuesAreNotEqual()
    {
        var relationship1 = RelationIdentifier.From("a");
        var relationship2 = RelationIdentifier.From("b");

        Assert.False(relationship1.Equals(relationship2));
        Assert.False(relationship1.Equals((object)relationship2));
        Assert.False(relationship1 == relationship2);
        Assert.True(relationship1 != relationship2);
        Assert.False(relationship1.Equals("b"));
    }

    [Fact]
    public void CompareTo_ReturnsCorrectValue()
    {
        var relationship1 = RelationIdentifier.From("a");
        var relationship2 = RelationIdentifier.From("b");

        Assert.True(relationship1.CompareTo(relationship2) < 0);
        Assert.True(relationship1 < relationship2);
        Assert.True(relationship1 <= relationship2);
        Assert.False(relationship1 > relationship2);
        Assert.False(relationship1 >= relationship2);
        Assert.True(relationship1.CompareTo("b") < 0);
    }

    [Fact]
    public void GetHashCode_ReturnsSameValue_ForEqualRelationships()
    {
        var relationship1 = RelationIdentifier.From("a");
        var relationship2 = RelationIdentifier.From("a");
        Assert.Equal(relationship1.GetHashCode(), relationship2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        const string value = "a_b_c";
        var relationship = RelationIdentifier.From(value);
        Assert.Equal(value, relationship.ToString());
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        const string value = "a_b_c";
        var relationship = RelationIdentifier.From(value);
        string s = relationship;
        Assert.Equal(value, s);
    }

    [Fact]
    public void ImplicitConversion_ToRelationship_ReturnsRelationship()
    {
        const string value = "a_b_c";
        RelationIdentifier relationship = value;
        Assert.Equal(value, relationship.ToString());
    }

    [Fact]
    public void Empty_Throws() =>
        Assert.Throws<ArgumentException>(() => RelationIdentifier.Empty());

    [Fact]
    public void Nothing_Returns_Three_Dots()
    {
        var nothing = RelationIdentifier.Nothing;
        Assert.Equal("...", nothing.ToString());
    }
}
