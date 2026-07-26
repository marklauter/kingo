using Kingo.Theories;
using static Kingo.Documents.Tests.TestHelpers;

namespace Kingo.Documents.Tests;

public sealed class RewriteExpressionPrinterTests
{
    [Fact]
    public void Print_This_EmitsKeyword() =>
        Assert.Equal("this", RewriteExpressionPrinter.Print(SubjectSetRewrite.This.Default));

    [Fact]
    public void Print_Computed_EmitsIdentifier() =>
        Assert.Equal("owner", RewriteExpressionPrinter.Print(Computed("owner")));

    [Fact]
    public void Print_FactToSubjectSet_EmitsPair() =>
        Assert.Equal(
            "(parent, viewer)",
            RewriteExpressionPrinter.Print(FactTo("parent", "viewer")));

    [Fact]
    public void Print_FlatChains_EmitBareOperands()
    {
        Assert.Equal(
            "a | b | c",
            RewriteExpressionPrinter.Print(Union([Computed("a"), Computed("b"), Computed("c")])));
        Assert.Equal(
            "a & b & c",
            RewriteExpressionPrinter.Print(Intersection([Computed("a"), Computed("b"), Computed("c")])));
    }

    [Fact]
    public void Print_IntersectionOperandOfUnion_IsBare()
    {
        Assert.Equal(
            "a & b | c",
            RewriteExpressionPrinter.Print(Union([Intersection([Computed("a"), Computed("b")]), Computed("c")])));
        Assert.Equal(
            "a | b & c",
            RewriteExpressionPrinter.Print(Union([Computed("a"), Intersection([Computed("b"), Computed("c")])])));
    }

    [Fact]
    public void Print_NestedUnionOperandOfUnion_IsParenthesized() =>
        Assert.Equal(
            "a | (b | c)",
            RewriteExpressionPrinter.Print(Union([Computed("a"), Union([Computed("b"), Computed("c")])])));

    [Fact]
    public void Print_CompoundOperandOfIntersection_IsParenthesized()
    {
        Assert.Equal(
            "(a | b) & c",
            RewriteExpressionPrinter.Print(Intersection([Union([Computed("a"), Computed("b")]), Computed("c")])));
        Assert.Equal(
            "a & (b & c)",
            RewriteExpressionPrinter.Print(Intersection([Computed("a"), Intersection([Computed("b"), Computed("c")])])));
    }

    [Fact]
    public void Print_ExclusionOperandOfBinaryOperator_IsBare()
    {
        Assert.Equal(
            "a ! b | c",
            RewriteExpressionPrinter.Print(Union([Exclusion(Computed("a"), Computed("b")), Computed("c")])));
        Assert.Equal(
            "a ! b & c",
            RewriteExpressionPrinter.Print(Intersection([Exclusion(Computed("a"), Computed("b")), Computed("c")])));
    }

    [Fact]
    public void Print_CompoundIncludeSideOfExclusion_IsParenthesized() =>
        Assert.Equal(
            "(a | b) ! c",
            RewriteExpressionPrinter.Print(Exclusion(Union([Computed("a"), Computed("b")]), Computed("c"))));

    [Fact]
    public void Print_ChainedExclusionIncludeSide_IsBare() =>
        Assert.Equal(
            "a ! b ! c",
            RewriteExpressionPrinter.Print(Exclusion(Exclusion(Computed("a"), Computed("b")), Computed("c"))));

    [Theory]
    [InlineData("this")]
    [InlineData("This")]
    public void Print_ReservedReference_IsCallerDefect(string name) =>
        _ = Assert.Throws<ArgumentException>(() => RewriteExpressionPrinter.Print(Computed(name)));

    [Fact]
    public void Print_DegenerateChains_RenderAsTheirChildren()
    {
        Assert.Equal("a", RewriteExpressionPrinter.Print(Union([Computed("a")])));
        Assert.Equal("a", RewriteExpressionPrinter.Print(Intersection([Computed("a")])));
    }

    [Fact]
    public void Print_CompoundExcludeSideOfExclusion_IsParenthesized()
    {
        Assert.Equal(
            "a ! (b | c)",
            RewriteExpressionPrinter.Print(Exclusion(Computed("a"), Union([Computed("b"), Computed("c")]))));
        Assert.Equal(
            "a ! (b ! c)",
            RewriteExpressionPrinter.Print(Exclusion(Computed("a"), Exclusion(Computed("b"), Computed("c")))));
    }
}
