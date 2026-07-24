using Results;

namespace Kingo.Tests;

public sealed class NamespaceNameTests
{
    [Theory]
    [InlineData("file")]
    [InlineData("file_share")]
    [InlineData("_private")]
    [InlineData("a1")]
    [InlineData("a")]
    public void Parse_ValidInput_ReturnsSuccess(string input)
    {
        var s = Assert.IsType<Result<NamespaceName>.Success>(NamespaceName.Parse(input));
        Assert.Equal(input, s.Value.Value);
    }

    [Theory]
    [InlineData("FILE", "file")]
    [InlineData("File_Share", "file_share")]
    [InlineData("A1", "a1")]
    public void Parse_MixedCaseInput_NormalizesToLowercase(string input, string expected)
    {
        var s = Assert.IsType<Result<NamespaceName>.Success>(NamespaceName.Parse(input));
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
        var f = Assert.IsType<Result<NamespaceName>.Failure>(NamespaceName.Parse(input!));
        var error = Assert.Single(f.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("namespace_name.empty", error.Code);
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
        var f = Assert.IsType<Result<NamespaceName>.Failure>(NamespaceName.Parse(input));
        var error = Assert.Single(f.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("namespace_name.invalid", error.Code);
    }

    [Theory]
    [InlineData("io/file")]
    [InlineData("io/file#viewer")]
    public void Parse_QualifiedPath_IsNotANamespaceName(string input)
    {
        // a namespace name is one segment; the qualified form is a NamespacePath, and only the fact side holds one
        var f = Assert.IsType<Result<NamespaceName>.Failure>(NamespaceName.Parse(input));
        Assert.Equal("namespace_name.invalid", Assert.Single(f.Errors).Code);
    }

    [Fact]
    public void Parse_IsTheSecondSegmentOfANamespacePath()
    {
        // a namespace path is a spec name and a namespace name joined by '/', so a bare name that parses here is
        // exactly a bare name that can close one ([[identifiers]])
        string[] inputs = ["file", "FILE", "_x", "a1", "0abc", "a-b", "a.b", "a b", ""];

        foreach (var input in inputs)
            Assert.Equal(
                NamespaceName.Parse(input) is Result<NamespaceName>.Success,
                NamespacePath.Parse($"io/{input}") is Result<NamespacePath>.Success);
    }

    [Fact]
    public void Unchecked_BypassesValidation_AcceptsRejectedInput()
    {
        var id = NamespaceName.Unchecked("0-not.valid:");
        Assert.Equal("0-not.valid:", id.Value);
    }

    [Fact]
    public void Unchecked_DoesNotLowercase()
    {
        var id = NamespaceName.Unchecked("FILE");
        Assert.Equal("FILE", id.Value);
    }

    [Fact]
    public void Equality_EqualValues_AreEqual()
    {
        var a = NamespaceName.Unchecked("file");
        var b = NamespaceName.Unchecked("file");

        Assert.True(a.Equals(b));
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equality_UnequalValues_AreNotEqual()
    {
        var a = NamespaceName.Unchecked("file");
        var b = NamespaceName.Unchecked("folder");

        Assert.False(a.Equals(b));
        Assert.False(a == b);
        Assert.True(a != b);
    }

    [Fact]
    public void CompareTo_IsOrdinal_AndConsistentWithOperators()
    {
        var a = NamespaceName.Unchecked("a");
        var b = NamespaceName.Unchecked("b");

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
        var id = NamespaceName.Unchecked("file");
        Assert.Equal("file", id.ToString());
    }
}
