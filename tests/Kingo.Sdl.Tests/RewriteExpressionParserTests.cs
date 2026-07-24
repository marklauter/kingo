using Kingo.Schemas;
using Results;
using static Kingo.Sdl.Tests.TestHelpers;

namespace Kingo.Sdl.Tests;

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
    [InlineData("thisone", "thisone")] // the keyword requires delimiters: an identifier merely starting with 'this' is an identifier
    [InlineData("_underscore", "_underscore")]
    public void Parse_Identifier_ReturnsComputedNormalizedToLowercase(string expression, string expected) =>
        Assert.Equal(Computed(expected), ParseSuccess(expression));

    [Theory]
    [InlineData("(parent, viewer)")]
    [InlineData("(parent,\nviewer)")]
    [InlineData("(PARENT, Viewer)")]
    public void Parse_FactToSubjectSet_ReturnsBothRelationships(string expression) =>
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
        // a parenthesized group is opaque to the operator chain: (a | b) | c is not the same tree as a | b | c
        Assert.Equal(
            Union([Union([Computed("a"), Computed("b")]), Computed("c")]),
            ParseSuccess("(a | b) | c"));

    [Fact]
    public void Parse_IntersectionBindsTighterThanUnion_OnTheLeft() =>
        // & binds tighter than |: a & b | c parses as (a & b) | c
        Assert.Equal(
            Union([Intersection([Computed("a"), Computed("b")]), Computed("c")]),
            ParseSuccess("a & b | c"));

    [Fact]
    public void Parse_IntersectionBindsTighterThanUnion_OnTheRight() =>
        // the mirror case, and the one precedence decides: a | b & c parses as a | (b & c)
        Assert.Equal(
            Union([Computed("a"), Intersection([Computed("b"), Computed("c")])]),
            ParseSuccess("a | b & c"));

    [Fact]
    public void Parse_ExclusionBindsTighterThanBinaryOperators() =>
        // the full cascade — ! tightest, then &, then |: a & b | c & d ! e parses as (a & b) | (c & (d ! e))
        Assert.Equal(
            Union(
            [
                Intersection([Computed("a"), Computed("b")]),
                Intersection([Computed("c"), Exclusion(Computed("d"), Computed("e"))]),
            ]),
            ParseSuccess("a & b | c & d ! e"));

    [Fact]
    public void Parse_ChainedExclusions_AssociateLeft() =>
        // matches mathematical set difference: users ! banned ! deleted is (users ! banned) ! deleted
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
    [InlineData("a b")] // two terms with no operator between them
    [InlineData("()")]
    [InlineData("(this, viewer)")] // a factset relationship is an identifier; 'this' lexes as the keyword
    [InlineData("this ! ! banned")]
    [InlineData("...")] // the fact grammar's '#...' marker punctuation cannot lex in a rewrite expression
    public void Parse_InvalidExpressions_FailsWithRewriteCode(string expression)
    {
        var failure = Assert.IsType<Result<SubjectSetRewrite>.Failure>(RewriteExpressionParser.Parse(expression));

        var error = Assert.Single(failure.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("spec.rewrite", error.Code);
    }

    [Fact]
    public void Parse_IdentifiersOutsideTheCoreGrammar_SurfaceTheCoreErrorsAccumulated()
    {
        // Superpower's C-style identifier lexes Unicode letters, but the core identifier grammar is ASCII:
        // the exit transform's RelationshipName.Parse rejects each one and the errors accumulate
        var failure = Assert.IsType<Result<SubjectSetRewrite>.Failure>(RewriteExpressionParser.Parse("café | naïve"));

        Assert.Equal(2, failure.Errors.Length);
        Assert.All(failure.Errors, error => Assert.Equal("relationship_name.invalid", error.Code));
    }

    [Fact]
    public void Parse_DeeplyNestedParentheses_FailsWithRewriteCode_NotStackOverflow()
    {
        // untrusted text must not pick the parser's stack depth: the paren scan refuses the
        // expression before the grammar's per-parenthesis recursion can run
        var expression = new string('(', 20_000) + "this" + new string(')', 20_000);

        var failure = Assert.IsType<Result<SubjectSetRewrite>.Failure>(RewriteExpressionParser.Parse(expression));

        Assert.Equal("spec.rewrite", Assert.Single(failure.Errors).Code);
    }

    [Fact]
    public void Parse_NestingJustPastTheDepthBound_FailsWithRewriteCode_NotStackOverflow()
    {
        // the scan must refuse every depth the grammar's recursion cannot survive: one level
        // past the bound is the first shape it refuses (499 levels demonstrably overflowed)
        var depth = SubjectSetRewrite.MaxDepth + 1;
        var expression = new string('(', depth) + "this" + new string(')', depth);

        var failure = Assert.IsType<Result<SubjectSetRewrite>.Failure>(RewriteExpressionParser.Parse(expression));

        Assert.Equal("spec.rewrite", Assert.Single(failure.Errors).Code);
    }

    [Fact]
    public void Parse_DeepestNestingTheDepthBoundAdmits_ParsesWithoutOverflow()
    {
        // the scan's contract from the other side: what it admits must be safe to parse
        var expression = new string('(', SubjectSetRewrite.MaxDepth) + "this" + new string(')', SubjectSetRewrite.MaxDepth);

        var success = Assert.IsType<Result<SubjectSetRewrite>.Success>(RewriteExpressionParser.Parse(expression));

        Assert.Same(SubjectSetRewrite.This.Default, success.Value);
    }

    [Fact]
    public void Parse_FactsetUnderTheDeepestGroupingNesting_IsNotCountedAsALevel()
    {
        // a factset's parens never open a recursion frame — the grammar parses the exact
        // '(a, b)' window without Parse.Ref — so the scan must not spend a level on them:
        // MaxDepth grouping levels around a factset is still exactly MaxDepth frames
        var expression = new string('(', SubjectSetRewrite.MaxDepth) + "(a, b)" + new string(')', SubjectSetRewrite.MaxDepth);

        var success = Assert.IsType<Result<SubjectSetRewrite>.Success>(RewriteExpressionParser.Parse(expression));

        _ = Assert.IsType<SubjectSetRewrite.FactToSubjectSet>(success.Value);
    }

    [Fact]
    public void Parse_NearFactsetShapes_CountAsGroupingAndFailAsSyntax()
    {
        // anything looser than the exact five-token factset window is grouping to the grammar
        // too — the scan counts it, the grammar rejects it, and nothing crashes on the way
        foreach (var expression in new[] { "(a, b | c)", "(a, (b, c))", "(a" })
        {
            var failure = Assert.IsType<Result<SubjectSetRewrite>.Failure>(RewriteExpressionParser.Parse(expression));

            Assert.Equal("spec.rewrite", Assert.Single(failure.Errors).Code);
        }
    }

    [Fact]
    public void Parse_UnbalancedCloseParenthesis_FallsThroughTheScanToTheGrammarError()
    {
        // a ')' with no matching '(' is not the scan's problem: below level zero it is
        // ignored by the paren scan and fails as plain bad syntax
        var failure = Assert.IsType<Result<SubjectSetRewrite>.Failure>(RewriteExpressionParser.Parse(") this"));

        Assert.Equal("spec.rewrite", Assert.Single(failure.Errors).Code);
    }

    [Fact]
    public void Parse_WideFlatExpression_IsNotMistakenForDepth()
    {
        // breadth is not depth: a 500-term union flattens n-ary and parses iteratively,
        // so neither guard may refuse it (a token budget would have)
        var expression = string.Join(" | ", Enumerable.Repeat("a", 500));

        var success = Assert.IsType<Result<SubjectSetRewrite>.Success>(RewriteExpressionParser.Parse(expression));

        Assert.Equal(500, Assert.IsType<SubjectSetRewrite.Union>(success.Value).Children.Length);
    }

    [Fact]
    public void Parse_TreeDeeperThanTheDepthBound_FailsWithDepthCode()
    {
        // the paren scan guards only grammar recursion; the parsed tree's height is the exact
        // authority on shape. Parenthesized unions add a tree level per paren level, so a shape
        // the paren scan admits can still exceed the bound — and must fail as a value
        var expression = Enumerable.Range(0, SubjectSetRewrite.MaxDepth - 1)
            .Aggregate("a | b", (accumulated, _) => $"({accumulated}) | b");

        var failure = Assert.IsType<Result<SubjectSetRewrite>.Failure>(RewriteExpressionParser.Parse(expression));

        Assert.Equal("rewrite.depth", Assert.Single(failure.Errors).Code);
    }

    [Fact]
    public void Parse_LongExclusionChain_FailsWithDepthCode_NotStackOverflow()
    {
        // a flat '!' chain needs no parentheses to nest: it left-associates into one node per
        // link, so the transform would recurse per link — the tree-height gate refuses it first
        var expression = string.Join(" ! ", Enumerable.Repeat("a", 20_000));

        var failure = Assert.IsType<Result<SubjectSetRewrite>.Failure>(RewriteExpressionParser.Parse(expression));

        Assert.Equal("rewrite.depth", Assert.Single(failure.Errors).Code);
    }

    [Fact]
    public void Parse_WideFlatUnionOfExclusions_IsNotMistakenForDepth()
    {
        // exclusion links at one level are siblings, not nesting: 101 'a ! b' terms under one
        // union is a depth-3 tree the printer itself emits bare, so it must parse — a scan
        // that counted '!' cumulatively across the level refused it
        var expression = string.Join(" | ", Enumerable.Repeat("a ! b", SubjectSetRewrite.MaxDepth + 1));

        var success = Assert.IsType<Result<SubjectSetRewrite>.Success>(RewriteExpressionParser.Parse(expression));

        Assert.Equal(SubjectSetRewrite.MaxDepth + 1, Assert.IsType<SubjectSetRewrite.Union>(success.Value).Children.Length);
    }

    [Fact]
    public void Parse_RightNestedExclusions_WithinTheBound_Parse()
    {
        // the printer parenthesizes a compound exclude side, so right-nested exclusions arrive
        // as 'this ! (this ! (…))' — one '!' and one '(' per level. A shape the factories
        // construct at Depth 61 must survive print → parse; a scan that counted both doubled it
        var expression = Enumerable.Range(0, 60)
            .Aggregate("this", (accumulated, _) => $"this ! ({accumulated})");

        _ = Assert.IsType<Result<SubjectSetRewrite>.Success>(RewriteExpressionParser.Parse(expression));
    }

    [Fact]
    public void Parse_ExclusionLinksSpreadAcrossParenLevels_FailWithDepthCode_NotStackOverflow()
    {
        // the bypass shape: '(…) ! a ! a' per paren level keeps momentary paren nesting small
        // while the left-associated chain stacks every link into one tree ~2,500 levels high —
        // the tree-height gate must refuse it before the transform recurses that far
        var expression = Enumerable.Range(0, SubjectSetRewrite.MaxDepth - 1)
            .Aggregate("a", (accumulated, _) => $"({accumulated})" + string.Concat(Enumerable.Repeat(" ! a", 25)));

        var failure = Assert.IsType<Result<SubjectSetRewrite>.Failure>(RewriteExpressionParser.Parse(expression));

        Assert.Equal("rewrite.depth", Assert.Single(failure.Errors).Code);
    }

    [Fact]
    public void Parse_InvalidExpression_MessageEmbedsTheOffendingText()
    {
        // the SDL author sees the error, not the document position: the message must carry the expression itself
        var failure = Assert.IsType<Result<SubjectSetRewrite>.Failure>(RewriteExpressionParser.Parse("this |"));

        var error = Assert.Single(failure.Errors);
        Assert.Contains("'this |'", error.Message, StringComparison.Ordinal);
    }
}
