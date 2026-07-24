using Results;

namespace Kingo.Tests;

public sealed class SpecNameTests
{
    [Theory]
    [InlineData("acme")]
    [InlineData("acme_corp")]
    [InlineData("_private")]
    [InlineData("a1")]
    [InlineData("a")]
    public void Parse_ValidInput_ReturnsSuccess(string input)
    {
        var s = Assert.IsType<Result<SpecName>.Success>(SpecName.Parse(input));
        Assert.Equal(input, s.Value.Value);
    }

    [Theory]
    [InlineData("ACME", "acme")]
    [InlineData("Acme_Corp", "acme_corp")]
    [InlineData("A1", "a1")]
    public void Parse_MixedCaseInput_NormalizesToLowercase(string input, string expected)
    {
        var s = Assert.IsType<Result<SpecName>.Success>(SpecName.Parse(input));
        Assert.Equal(expected, s.Value.Value);
        Assert.Equal(expected, s.Value.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Parse_NullEmptyOrWhitespace_ReturnsEmptyValidationFailure(string? input)
    {
        // null reaches Parse only through reflection callers (see IParse); it lands in the empty guard
        var f = Assert.IsType<Result<SpecName>.Failure>(SpecName.Parse(input!));
        var error = Assert.Single(f.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("spec_name.empty", error.Code);
    }

    [Theory]
    [InlineData("0abc")]
    [InlineData("a-b")]
    [InlineData("a.b")]
    [InlineData("a:b")]
    [InlineData("a b")]
    [InlineData("café")]
    [InlineData("#")]
    [InlineData("@")]
    public void Parse_InvalidCharacters_ReturnsInvalidValidationFailure(string input)
    {
        var f = Assert.IsType<Result<SpecName>.Failure>(SpecName.Parse(input));
        var error = Assert.Single(f.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("spec_name.invalid", error.Code);
    }

    [Fact]
    public void Parse_IsOneSegment_AndAgreesWithTheSegmentInsideANamespacePath()
    {
        // spec and namespace names are the same kind of thing — authored vocabulary — and share the segment
        // grammar ([[identifiers]]); a namespace path is two of those segments, so a spec name that parses is
        // exactly a spec name that can lead one
        string[] inputs = ["acme", "ACME", "_x", "a1", "0abc", "a-b", "a.b", "a b", ""];

        foreach (var input in inputs)
            Assert.Equal(
                SpecName.Parse(input) is Result<SpecName>.Success,
                NamespacePath.Parse($"{input}/file") is Result<NamespacePath>.Success);
    }

    [Theory]
    [InlineData("io/file")]
    [InlineData("io/file#viewer")]
    public void Parse_QualifiedPath_IsNotASpecName(string input)
    {
        // a spec is one segment; anything carrying a separator names something below it
        var f = Assert.IsType<Result<SpecName>.Failure>(SpecName.Parse(input));
        Assert.Equal("spec_name.invalid", Assert.Single(f.Errors).Code);
    }

    [Fact]
    public void Unchecked_BypassesValidation_AcceptsRejectedInput()
    {
        var id = SpecName.Unchecked("0-not.valid:");
        Assert.Equal("0-not.valid:", id.Value);
    }

    [Fact]
    public void Unchecked_DoesNotLowercase()
    {
        var id = SpecName.Unchecked("ACME");
        Assert.Equal("ACME", id.Value);
    }

    [Fact]
    public void Equality_EqualValues_AreEqual()
    {
        var a = SpecName.Unchecked("acme");
        var b = SpecName.Unchecked("acme");

        Assert.True(a.Equals(b));
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equality_UnequalValues_AreNotEqual()
    {
        var a = SpecName.Unchecked("acme");
        var b = SpecName.Unchecked("globex");

        Assert.False(a.Equals(b));
        Assert.False(a == b);
        Assert.True(a != b);
    }

    [Fact]
    public void CompareTo_IsOrdinal_AndConsistentWithOperators()
    {
        var a = SpecName.Unchecked("a");
        var b = SpecName.Unchecked("b");

        Assert.True(a.CompareTo(b) < 0);
        Assert.True(b.CompareTo(a) > 0);
        Assert.Equal(0, a.CompareTo(a));

        Assert.True(a < b);
        Assert.True(a <= b);
        Assert.False(a > b);
        Assert.False(a >= b);
        Assert.True(b > a);
        Assert.True(b >= a);
    }

    [Fact]
    public void ToString_ReturnsRawValue()
    {
        var id = SpecName.Unchecked("acme");
        Assert.Equal("acme", id.ToString());
    }
}
