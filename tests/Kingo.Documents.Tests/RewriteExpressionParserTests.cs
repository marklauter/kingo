using Kingo.Theories;
using Results;
using static Kingo.Documents.Tests.TestHelpers;

namespace Kingo.Documents.Tests;

public sealed class RewriteExpressionParserTests
{
    private static SubjectSetRewrite ParseSuccess(string expression) =>
        Assert.IsType<Result<SubjectSetRewrite>.Success>(RewriteExpressionParser.Parse(expression)).Value;

    [Theory]
    [InlineData("this")]
    [InlineData("THIS")]
    [InlineData("(this)")]
    public void Parse_ThisKeyword_IsCaseInsensitive(string expression) =>
        Assert.Equal(SubjectSetRewrite.This.Default, ParseSuccess(expression));

    [Theory]
    [InlineData("owner", "owner")]
    [InlineData("OWNER", "owner")]
    [InlineData("thisone", "thisone")]
    [InlineData("_underscore", "_underscore")]
    public void Parse_Identifier_ReturnsComputedNormalizedToLowercase(string expression, string expected) =>
        Assert.Equal(Computed(expected), ParseSuccess(expression));

    [Theory]
    [InlineData("(parent, viewer)")]
    [InlineData("(parent,\nviewer)")]
    [InlineData("(PARENT, Viewer)")]
    public void Parse_FactToSubjectSet_ReturnsBothRelations(string expression) =>
        Assert.Equal(FactTo("parent", "viewer"), ParseSuccess(expression));

    [Fact]
    public void Parse_UnionChain_FlattensToOneNode() =>
        Assert.Equal(
            Union([Computed("a"), Computed("b"), Computed("c")]),
            ParseSuccess("a | b | c"));

    [Fact]
    public void Parse_IntersectionChain_FlattensToOneNode() =>
        Assert.Equal(
            Intersection([Computed("a"), Computed("b"), Computed("c")]),
            ParseSuccess("a & b & c"));

    [Fact]
    public void Parse_ParenthesizedOperand_KeepsItsNestedShape() =>
        Assert.Equal(
            Union([Union([Computed("a"), Computed("b")]), Computed("c")]),
            ParseSuccess("(a | b) | c"));

    [Fact]
    public void Parse_IntersectionBindsTighterThanUnion_OnTheLeft() =>
        Assert.Equal(
            Union([Intersection([Computed("a"), Computed("b")]), Computed("c")]),
            ParseSuccess("a & b | c"));

    [Fact]
    public void Parse_IntersectionBindsTighterThanUnion_OnTheRight() =>
        Assert.Equal(
            Union([Computed("a"), Intersection([Computed("b"), Computed("c")])]),
            ParseSuccess("a | b & c"));

    [Fact]
    public void Parse_ExclusionBindsTighterThanBinaryOperators() =>
        Assert.Equal(
            Union(
            [
                Intersection([Computed("a"), Computed("b")]),
                Intersection([Computed("c"), Exclusion(Computed("d"), Computed("e"))]),
            ]),
            ParseSuccess("a & b | c & d ! e"));

    [Fact]
    public void Parse_ChainedExclusions_AssociateLeft() =>
        Assert.Equal(
            Exclusion(Exclusion(Computed("users"), Computed("banned")), Computed("deleted")),
            ParseSuccess("users ! banned ! deleted"));

    [Fact]
    public void Parse_ComplexExpression_ReturnsExpectedTree() =>
        Assert.Equal(
            Exclusion(
                Union(
                [
                    SubjectSetRewrite.This.Default,
                    Computed("editor"),
                    FactTo("parent", "viewer"),
                ]),
                Computed("banned")),
            ParseSuccess("(this | editor | (parent, viewer)) ! banned"));

    [Theory]
    [InlineData("this # trailing comment")]
    [InlineData("this |\nowner")]
    [InlineData("this |\r\nowner")]
    [InlineData(@"(this |
    editor | # user editors
    (parent, viewer)) ! # exclude
banned")]
    public void Parse_CommentsAndLineBreaks_AreIgnored(string expression) =>
        _ = ParseSuccess(expression);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("(parent viewer)")]
    [InlineData("(parent, viewer")]
    [InlineData("this |")]
    [InlineData("| this")]
    [InlineData("this | & owner")]
    [InlineData("invalid-name")]
    [InlineData("123invalid")]
    [InlineData("a ! (b !")]
    [InlineData("a b")]
    [InlineData("()")]
    [InlineData("(this, viewer)")]
    [InlineData("this ! ! banned")]
    [InlineData("...")]
    public void Parse_InvalidExpressions_FailsWithRewriteCode(string expression)
    {
        var failure = Assert.IsType<Result<SubjectSetRewrite>.Failure>(RewriteExpressionParser.Parse(expression));

        var error = Assert.Single(failure.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("theory.rewrite", error.Code.Value);
    }

    [Fact]
    public void Parse_IdentifiersOutsideTheCoreGrammar_SurfaceTheCoreErrorsAccumulated()
    {
        var failure = Assert.IsType<Result<SubjectSetRewrite>.Failure>(RewriteExpressionParser.Parse("café | naïve"));

        Assert.Equal(2, failure.Errors.Length);
        Assert.All(failure.Errors, error => Assert.Equal("relation_name.invalid", error.Code.Value));
    }

    [Fact]
    public void Parse_DeeplyNestedParentheses_FailsWithRewriteCode_NotStackOverflow()
    {
        var expression = new string('(', 20_000) + "this" + new string(')', 20_000);

        var failure = Assert.IsType<Result<SubjectSetRewrite>.Failure>(RewriteExpressionParser.Parse(expression));

        Assert.Equal("theory.rewrite", Assert.Single(failure.Errors).Code.Value);
    }

    [Fact]
    public void Parse_NestingJustPastTheDepthBound_FailsWithRewriteCode_NotStackOverflow()
    {
        var depth = SubjectSetRewrite.MaxDepth + 1;
        var expression = new string('(', depth) + "this" + new string(')', depth);

        var failure = Assert.IsType<Result<SubjectSetRewrite>.Failure>(RewriteExpressionParser.Parse(expression));

        Assert.Equal("theory.rewrite", Assert.Single(failure.Errors).Code.Value);
    }

    [Fact]
    public void Parse_DeepestNestingTheDepthBoundAdmits_ParsesWithoutOverflow()
    {
        var expression = new string('(', SubjectSetRewrite.MaxDepth) + "this" + new string(')', SubjectSetRewrite.MaxDepth);

        var success = Assert.IsType<Result<SubjectSetRewrite>.Success>(RewriteExpressionParser.Parse(expression));

        Assert.Same(SubjectSetRewrite.This.Default, success.Value);
    }

    [Fact]
    public void Parse_FactsetUnderTheDeepestGroupingNesting_IsNotCountedAsALevel()
    {
        var expression = new string('(', SubjectSetRewrite.MaxDepth) + "(a, b)" + new string(')', SubjectSetRewrite.MaxDepth);

        var success = Assert.IsType<Result<SubjectSetRewrite>.Success>(RewriteExpressionParser.Parse(expression));

        _ = Assert.IsType<SubjectSetRewrite.FactToSubjectSet>(success.Value);
    }

    [Fact]
    public void Parse_NearFactsetShapes_CountAsGroupingAndFailAsSyntax()
    {
        foreach (var expression in new[] { "(a, b | c)", "(a, (b, c))", "(a" })
        {
            var failure = Assert.IsType<Result<SubjectSetRewrite>.Failure>(RewriteExpressionParser.Parse(expression));

            Assert.Equal("theory.rewrite", Assert.Single(failure.Errors).Code.Value);
        }
    }

    [Fact]
    public void Parse_UnbalancedCloseParenthesis_FallsThroughTheScanToTheGrammarError()
    {
        var failure = Assert.IsType<Result<SubjectSetRewrite>.Failure>(RewriteExpressionParser.Parse(") this"));

        Assert.Equal("theory.rewrite", Assert.Single(failure.Errors).Code.Value);
    }

    [Fact]
    public void Parse_WideFlatExpression_IsNotMistakenForDepth()
    {
        var expression = string.Join(" | ", Enumerable.Repeat("a", 500));

        var success = Assert.IsType<Result<SubjectSetRewrite>.Success>(RewriteExpressionParser.Parse(expression));

        Assert.Equal(500, Assert.IsType<SubjectSetRewrite.Union>(success.Value).Children.Length);
    }

    [Fact]
    public void Parse_TreeDeeperThanTheDepthBound_FailsWithDepthCode()
    {
        var expression = Enumerable.Range(0, SubjectSetRewrite.MaxDepth - 1)
            .Aggregate("a | b", (accumulated, _) => $"({accumulated}) | b");

        var failure = Assert.IsType<Result<SubjectSetRewrite>.Failure>(RewriteExpressionParser.Parse(expression));

        Assert.Equal("rewrite.depth", Assert.Single(failure.Errors).Code.Value);
    }

    [Fact]
    public void Parse_LongExclusionChain_FailsWithDepthCode_NotStackOverflow()
    {
        var expression = string.Join(" ! ", Enumerable.Repeat("a", 20_000));

        var failure = Assert.IsType<Result<SubjectSetRewrite>.Failure>(RewriteExpressionParser.Parse(expression));

        Assert.Equal("rewrite.depth", Assert.Single(failure.Errors).Code.Value);
    }

    [Fact]
    public void Parse_WideFlatUnionOfExclusions_IsNotMistakenForDepth()
    {
        var expression = string.Join(" | ", Enumerable.Repeat("a ! b", SubjectSetRewrite.MaxDepth + 1));

        var success = Assert.IsType<Result<SubjectSetRewrite>.Success>(RewriteExpressionParser.Parse(expression));

        Assert.Equal(SubjectSetRewrite.MaxDepth + 1, Assert.IsType<SubjectSetRewrite.Union>(success.Value).Children.Length);
    }

    [Fact]
    public void Parse_RightNestedExclusions_WithinTheBound_Parse()
    {
        var expression = Enumerable.Range(0, 60)
            .Aggregate("this", (accumulated, _) => $"this ! ({accumulated})");

        _ = Assert.IsType<Result<SubjectSetRewrite>.Success>(RewriteExpressionParser.Parse(expression));
    }

    [Fact]
    public void Parse_ExclusionLinksSpreadAcrossParenLevels_FailWithDepthCode_NotStackOverflow()
    {
        var expression = Enumerable.Range(0, SubjectSetRewrite.MaxDepth - 1)
            .Aggregate("a", (accumulated, _) => $"({accumulated})" + string.Concat(Enumerable.Repeat(" ! a", 25)));

        var failure = Assert.IsType<Result<SubjectSetRewrite>.Failure>(RewriteExpressionParser.Parse(expression));

        Assert.Equal("rewrite.depth", Assert.Single(failure.Errors).Code.Value);
    }

    [Fact]
    public void Parse_InvalidExpression_MessageEmbedsTheOffendingText()
    {
        var failure = Assert.IsType<Result<SubjectSetRewrite>.Failure>(RewriteExpressionParser.Parse("this |"));

        var error = Assert.Single(failure.Errors);
        Assert.Contains("'this |'", error.Message.Value, StringComparison.Ordinal);
    }
}
