namespace Kingo.Tests;

public sealed class RelationshipTests
{
    [Fact]
    public void From_Throws_IfValueIsNull() =>
        Assert.Throws<ArgumentNullException>(() => RelationName.From(null!));

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void From_Throws_IfValueIsWhitespace(string value) =>
        Assert.Throws<ArgumentException>(() => RelationName.From(value));

    [Theory]
    [InlineData("a-b")]
    [InlineData("a$b")]
    [InlineData("a b")]
    public void From_Throws_IfValueContainsInvalidCharacters(string value)
    {
        var exception = Assert.Throws<ArgumentException>("value", () => RelationName.From(value));
        Assert.StartsWith("value contains invalid characters", exception.Message);
        Assert.Contains(value, exception.Message);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("a.b")]
    [InlineData("a_b")]
    [InlineData("a0")]
    [InlineData("...")]
    public void From_ReturnsRelationship_IfValueIsValid(string value)
    {
        var relationship = RelationName.From(value);
        Assert.Equal(value, relationship.ToString());
    }

    [Fact]
    public void Equals_ReturnsTrue_IfValuesAreEqual()
    {
        var relationship1 = RelationName.From("a");
        var relationship2 = RelationName.From("a");

        Assert.True(relationship1.Equals(relationship2));
        Assert.True(relationship1.Equals((object)relationship2));
        Assert.True(relationship1 == relationship2);
        Assert.False(relationship1 != relationship2);
        Assert.True(relationship1.Equals("a"));
    }

    [Fact]
    public void Equals_ReturnsFalse_IfValuesAreNotEqual()
    {
        var relationship1 = RelationName.From("a");
        var relationship2 = RelationName.From("b");

        Assert.False(relationship1.Equals(relationship2));
        Assert.False(relationship1.Equals((object)relationship2));
        Assert.False(relationship1 == relationship2);
        Assert.True(relationship1 != relationship2);
        Assert.False(relationship1.Equals("b"));
    }

    [Fact]
    public void CompareTo_ReturnsCorrectValue()
    {
        var relationship1 = RelationName.From("a");
        var relationship2 = RelationName.From("b");

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
        var relationship1 = RelationName.From("a");
        var relationship2 = RelationName.From("a");
        Assert.Equal(relationship1.GetHashCode(), relationship2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        const string value = "a_b_c";
        var relationship = RelationName.From(value);
        Assert.Equal(value, relationship.ToString());
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        const string value = "a_b_c";
        var relationship = RelationName.From(value);
        string s = relationship;
        Assert.Equal(value, s);
    }

    [Fact]
    public void ImplicitConversion_ToRelationship_ReturnsRelationship()
    {
        const string value = "a_b_c";
        RelationName relationship = value;
        Assert.Equal(value, relationship.ToString());
    }

    [Fact]
    public void Empty_Throws() =>
        Assert.Throws<ArgumentException>(() => RelationName.Empty());

    [Fact]
    public void Nothing_ReturnsCorrectValue()
    {
        var nothing = RelationName.Nothing;
        Assert.Equal("...", nothing.ToString());
    }
}
