using Kingo.Schemas;
using static Kingo.Sdl.Tests.TestHelpers;

namespace Kingo.Sdl.Tests;

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
        // a union's operand sits at the <intersection> level: & binds tighter, so the nested
        // intersection reparses as the same child either side of the union
        Assert.Equal(
            "a & b | c",
            RewriteExpressionPrinter.Print(Union([Intersection([Computed("a"), Computed("b")]), Computed("c")])));
        Assert.Equal(
            "a | b & c",
            RewriteExpressionPrinter.Print(Union([Computed("a"), Intersection([Computed("b"), Computed("c")])])));
    }

    [Fact]
    public void Print_NestedUnionOperandOfUnion_IsParenthesized() =>
        // the chain would otherwise absorb it into one n-ary union
        Assert.Equal(
            "a | (b | c)",
            RewriteExpressionPrinter.Print(Union([Computed("a"), Union([Computed("b"), Computed("c")])])));

    [Fact]
    public void Print_CompoundOperandOfIntersection_IsParenthesized()
    {
        // an intersection's operand sits at the <exclusion> level: a union would regroup and a
        // nested intersection would be absorbed
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
        // ! binds tighter than both, so no parentheses are needed either side
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
        // left-associative chain form: (a ! b) ! c renders and reparses as a ! b ! c
        Assert.Equal(
            "a ! b ! c",
            RewriteExpressionPrinter.Print(Exclusion(Exclusion(Computed("a"), Computed("b")), Computed("c"))));

    [Theory]
    [InlineData("this")]
    [InlineData("This")] // Unchecked performs no normalization, but the tokenizer matches the keyword case-insensitively
    public void Print_ReservedReference_IsCallerDefect(string name) =>
        // 'this' would silently reparse as the keyword (direct membership)
        _ = Assert.Throws<ArgumentException>(() => RewriteExpressionPrinter.Print(Computed(name)));

    [Fact]
    public void Print_DegenerateChains_RenderAsTheirChildren()
    {
        // the one tree the grammar cannot express (constructing it is the caller's defect): a single-child
        // union/intersection renders as its child and reparses to the simpler shape; the zero-child shape
        // is unrepresentable — Create refuses it
        Assert.Equal("a", RewriteExpressionPrinter.Print(Union([Computed("a")])));
        Assert.Equal("a", RewriteExpressionPrinter.Print(Intersection([Computed("a")])));
    }

    [Fact]
    public void Print_CompoundExcludeSideOfExclusion_IsParenthesized()
    {
        // the exclude side is a <term>: any compound there needs parentheses, including a nested exclusion
        Assert.Equal(
            "a ! (b | c)",
            RewriteExpressionPrinter.Print(Exclusion(Computed("a"), Union([Computed("b"), Computed("c")]))));
        Assert.Equal(
            "a ! (b ! c)",
            RewriteExpressionPrinter.Print(Exclusion(Computed("a"), Exclusion(Computed("b"), Computed("c")))));
    }
}
