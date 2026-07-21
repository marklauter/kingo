using Kingo.Schemas;
using static Kingo.Sdl.Tests.TestHelpers;

namespace Kingo.Sdl.Tests;

public sealed class RewriteExpressionPrinterTests
{
    [Fact]
    public void Print_This_EmitsKeyword() =>
        Assert.Equal("this", RewriteExpressionPrinter.Print(ThisRewrite.Default));

    [Fact]
    public void Print_Computed_EmitsIdentifier() =>
        Assert.Equal("owner", RewriteExpressionPrinter.Print(Computed("owner")));

    [Fact]
    public void Print_FactToSubjectSet_EmitsPair() =>
        Assert.Equal(
            "(parent, viewer)",
            RewriteExpressionPrinter.Print(new FactToSubjectSetRewrite(Rel("parent"), Rel("viewer"))));

    [Fact]
    public void Print_FlatChains_EmitBareOperands()
    {
        Assert.Equal(
            "a | b | c",
            RewriteExpressionPrinter.Print(new UnionRewrite([Computed("a"), Computed("b"), Computed("c")])));
        Assert.Equal(
            "a & b & c",
            RewriteExpressionPrinter.Print(new IntersectionRewrite([Computed("a"), Computed("b"), Computed("c")])));
    }

    [Fact]
    public void Print_CompoundOperandOfBinaryOperator_IsParenthesized()
    {
        // the operator chain would otherwise absorb or regroup the nested node on reparse
        Assert.Equal(
            "(a & b) | c",
            RewriteExpressionPrinter.Print(new UnionRewrite([new IntersectionRewrite([Computed("a"), Computed("b")]), Computed("c")])));
        Assert.Equal(
            "a | (b | c)",
            RewriteExpressionPrinter.Print(new UnionRewrite([Computed("a"), new UnionRewrite([Computed("b"), Computed("c")])])));
    }

    [Fact]
    public void Print_ExclusionOperandOfBinaryOperator_IsBare() =>
        // ! binds tighter than | so no parentheses are needed: a ! b | c reparses as (a ! b) | c
        Assert.Equal(
            "a ! b | c",
            RewriteExpressionPrinter.Print(new UnionRewrite([new ExclusionRewrite(Computed("a"), Computed("b")), Computed("c")])));

    [Fact]
    public void Print_CompoundIncludeSideOfExclusion_IsParenthesized() =>
        Assert.Equal(
            "(a | b) ! c",
            RewriteExpressionPrinter.Print(new ExclusionRewrite(new UnionRewrite([Computed("a"), Computed("b")]), Computed("c"))));

    [Fact]
    public void Print_ChainedExclusionIncludeSide_IsBare() =>
        // left-associative chain form: (a ! b) ! c renders and reparses as a ! b ! c
        Assert.Equal(
            "a ! b ! c",
            RewriteExpressionPrinter.Print(new ExclusionRewrite(new ExclusionRewrite(Computed("a"), Computed("b")), Computed("c"))));

    [Theory]
    [InlineData("this")]
    [InlineData("This")] // Create performs no normalization, but the tokenizer matches the keyword case-insensitively
    public void Print_ReservedReference_IsCallerDefect(string name) =>
        // 'this' would silently reparse as the keyword (direct membership)
        _ = Assert.Throws<ArgumentException>(() => RewriteExpressionPrinter.Print(Computed(name)));

    [Fact]
    public void Print_DegenerateChains_RenderAsTheirChildren()
    {
        // trees the grammar cannot express (constructing one is the caller's defect): a single-child
        // union/intersection renders as its child and reparses to the simpler shape; zero children render empty
        Assert.Equal("a", RewriteExpressionPrinter.Print(new UnionRewrite([Computed("a")])));
        Assert.Equal("a", RewriteExpressionPrinter.Print(new IntersectionRewrite([Computed("a")])));
        Assert.Equal(string.Empty, RewriteExpressionPrinter.Print(new UnionRewrite([])));
    }

    [Fact]
    public void Print_CompoundExcludeSideOfExclusion_IsParenthesized()
    {
        // the exclude side is a <term>: any compound there needs parentheses, including a nested exclusion
        Assert.Equal(
            "a ! (b | c)",
            RewriteExpressionPrinter.Print(new ExclusionRewrite(Computed("a"), new UnionRewrite([Computed("b"), Computed("c")]))));
        Assert.Equal(
            "a ! (b ! c)",
            RewriteExpressionPrinter.Print(new ExclusionRewrite(Computed("a"), new ExclusionRewrite(Computed("b"), Computed("c")))));
    }
}
