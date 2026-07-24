using Results;

namespace Kingo.Tests;

public sealed class DomainNameTests
{
    [Theory]
    [InlineData("acme")]
    [InlineData("acme_corp")]
    [InlineData("_private")]
    [InlineData("a1")]
    [InlineData("a")]
    public void Parse_ValidInput_ReturnsSuccess(string input)
    {
        var s = Assert.IsType<Result<DomainName>.Success>(DomainName.Parse(input));
        Assert.Equal(input, s.Value.Value);
    }

    [Theory]
    [InlineData("ACME", "acme")]
    [InlineData("Acme_Corp", "acme_corp")]
    [InlineData("A1", "a1")]
    public void Parse_MixedCaseInput_NormalizesToLowercase(string input, string expected)
    {
        var s = Assert.IsType<Result<DomainName>.Success>(DomainName.Parse(input));
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
        var f = Assert.IsType<Result<DomainName>.Failure>(DomainName.Parse(input!));
        var error = Assert.Single(f.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("domain_name.empty", error.Code);
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
        var f = Assert.IsType<Result<DomainName>.Failure>(DomainName.Parse(input));
        var error = Assert.Single(f.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("domain_name.invalid", error.Code);
    }

    [Fact]
    public void Parse_IsOneSegment_AndAgreesWithTheSegmentInsideANamespacePath()
    {
        // domain and namespace names are the same kind of thing — authored vocabulary — and share the segment
        // grammar ([[identifiers]]); a namespace path is two of those segments, so a domain name that parses is
        // exactly a domain name that can lead one
        string[] inputs = ["acme", "ACME", "_x", "a1", "0abc", "a-b", "a.b", "a b", ""];

        foreach (var input in inputs)
            Assert.Equal(
                DomainName.Parse(input) is Result<DomainName>.Success,
                NamespacePath.Parse($"{input}/file") is Result<NamespacePath>.Success);
    }

    [Theory]
    [InlineData("io/file")]
    [InlineData("io/file#viewer")]
    public void Parse_QualifiedPath_IsNotADomainName(string input)
    {
        // a domain is one segment; anything carrying a separator names something below it
        var f = Assert.IsType<Result<DomainName>.Failure>(DomainName.Parse(input));
        Assert.Equal("domain_name.invalid", Assert.Single(f.Errors).Code);
    }

    [Fact]
    public void Unchecked_BypassesValidation_AcceptsRejectedInput()
    {
        var id = DomainName.Unchecked("0-not.valid:");
        Assert.Equal("0-not.valid:", id.Value);
    }

    [Fact]
    public void Unchecked_DoesNotLowercase()
    {
        var id = DomainName.Unchecked("ACME");
        Assert.Equal("ACME", id.Value);
    }

    [Fact]
    public void Equality_EqualValues_AreEqual()
    {
        var a = DomainName.Unchecked("acme");
        var b = DomainName.Unchecked("acme");

        Assert.True(a.Equals(b));
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equality_UnequalValues_AreNotEqual()
    {
        var a = DomainName.Unchecked("acme");
        var b = DomainName.Unchecked("globex");

        Assert.False(a.Equals(b));
        Assert.False(a == b);
        Assert.True(a != b);
    }

    [Fact]
    public void CompareTo_IsOrdinal_AndConsistentWithOperators()
    {
        var a = DomainName.Unchecked("a");
        var b = DomainName.Unchecked("b");

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
        var id = DomainName.Unchecked("acme");
        Assert.Equal("acme", id.ToString());
    }
}
