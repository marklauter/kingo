using Kingo.Schemas;
using static Kingo.Serialization.Sdl.Tests.TestHelpers;

namespace Kingo.Serialization.Sdl.Tests;

public sealed class RewriteExpressionRendererTests
{
    [Fact]
    public void Render_This_EmitsKeyword() =>
        Assert.Equal("this", RewriteExpressionRenderer.Render(ThisRewrite.Default));

    [Fact]
    public void Render_Computed_EmitsIdentifier() =>
        Assert.Equal("owner", RewriteExpressionRenderer.Render(Computed("owner")));

    [Fact]
    public void Render_TupleToSubjectSet_EmitsPair() =>
        Assert.Equal(
            "(parent, viewer)",
            RewriteExpressionRenderer.Render(new TupleToSubjectSetRewrite(Rel("parent"), Rel("viewer"))));

    [Fact]
    public void Render_FlatChains_EmitBareOperands()
    {
        Assert.Equal(
            "a | b | c",
            RewriteExpressionRenderer.Render(new UnionRewrite([Computed("a"), Computed("b"), Computed("c")])));
        Assert.Equal(
            "a & b & c",
            RewriteExpressionRenderer.Render(new IntersectionRewrite([Computed("a"), Computed("b"), Computed("c")])));
    }

    [Fact]
    public void Render_CompoundOperandOfBinaryOperator_IsParenthesized()
    {
        // the operator chain would otherwise absorb or regroup the nested node on reparse
        Assert.Equal(
            "(a & b) | c",
            RewriteExpressionRenderer.Render(new UnionRewrite([new IntersectionRewrite([Computed("a"), Computed("b")]), Computed("c")])));
        Assert.Equal(
            "a | (b | c)",
            RewriteExpressionRenderer.Render(new UnionRewrite([Computed("a"), new UnionRewrite([Computed("b"), Computed("c")])])));
    }

    [Fact]
    public void Render_ExclusionOperandOfBinaryOperator_IsBare() =>
        // ! binds tighter than | so no parentheses are needed: a ! b | c reparses as (a ! b) | c
        Assert.Equal(
            "a ! b | c",
            RewriteExpressionRenderer.Render(new UnionRewrite([new ExclusionRewrite(Computed("a"), Computed("b")), Computed("c")])));

    [Fact]
    public void Render_CompoundIncludeSideOfExclusion_IsParenthesized() =>
        Assert.Equal(
            "(a | b) ! c",
            RewriteExpressionRenderer.Render(new ExclusionRewrite(new UnionRewrite([Computed("a"), Computed("b")]), Computed("c"))));

    [Fact]
    public void Render_ChainedExclusionIncludeSide_IsBare() =>
        // left-associative chain form: (a ! b) ! c renders and reparses as a ! b ! c
        Assert.Equal(
            "a ! b ! c",
            RewriteExpressionRenderer.Render(new ExclusionRewrite(new ExclusionRewrite(Computed("a"), Computed("b")), Computed("c"))));

    [Theory]
    [InlineData("this")]
    [InlineData("This")] // Create performs no normalization, but the tokenizer matches the keyword case-insensitively
    [InlineData("...")]
    public void Render_ReservedReference_IsCallerDefect(string name) =>
        // 'this' would silently reparse as the keyword (direct membership); '...' cannot lex at all
        _ = Assert.Throws<ArgumentException>(() => RewriteExpressionRenderer.Render(Computed(name)));

    [Fact]
    public void Render_CompoundExcludeSideOfExclusion_IsParenthesized()
    {
        // the exclude side is a <term>: any compound there needs parentheses, including a nested exclusion
        Assert.Equal(
            "a ! (b | c)",
            RewriteExpressionRenderer.Render(new ExclusionRewrite(Computed("a"), new UnionRewrite([Computed("b"), Computed("c")]))));
        Assert.Equal(
            "a ! (b ! c)",
            RewriteExpressionRenderer.Render(new ExclusionRewrite(Computed("a"), new ExclusionRewrite(Computed("b"), Computed("c")))));
    }
}
