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
        Assert.Equal(ThisRewrite.Default, ParseSuccess(expression));

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
    public void Parse_TupleToSubjectSet_ReturnsBothRelationships(string expression) =>
        Assert.Equal(new TupleToSubjectSetRewrite(Rel("parent"), Rel("viewer")), ParseSuccess(expression));

    [Fact]
    public void Parse_UnionChain_FlattensToOneNode() =>
        Assert.Equal(
            new UnionRewrite([Computed("a"), Computed("b"), Computed("c")]),
            ParseSuccess("a | b | c"));

    [Fact]
    public void Parse_IntersectionChain_FlattensToOneNode() =>
        Assert.Equal(
            new IntersectionRewrite([Computed("a"), Computed("b"), Computed("c")]),
            ParseSuccess("a & b & c"));

    [Fact]
    public void Parse_ParenthesizedOperand_KeepsItsNestedShape() =>
        // a parenthesized group is opaque to the operator chain: (a | b) | c is not the same tree as a | b | c
        Assert.Equal(
            new UnionRewrite([new UnionRewrite([Computed("a"), Computed("b")]), Computed("c")]),
            ParseSuccess("(a | b) | c"));

    [Fact]
    public void Parse_MixedOperators_GroupsConsecutiveRunsLeftToRight() =>
        // & and | share precedence, left-associative: a & b | c parses as (a & b) | c
        Assert.Equal(
            new UnionRewrite([new IntersectionRewrite([Computed("a"), Computed("b")]), Computed("c")]),
            ParseSuccess("a & b | c"));

    [Fact]
    public void Parse_ExclusionBindsTighterThanBinaryOperators() =>
        // ! is highest: a & b | c & d ! e parses as ((a & b) | c) & (d ! e)
        Assert.Equal(
            new IntersectionRewrite(
            [
                new UnionRewrite([new IntersectionRewrite([Computed("a"), Computed("b")]), Computed("c")]),
                new ExclusionRewrite(Computed("d"), Computed("e")),
            ]),
            ParseSuccess("a & b | c & d ! e"));

    [Fact]
    public void Parse_ChainedExclusions_AssociateLeft() =>
        // matches mathematical set difference: users ! banned ! deleted is (users ! banned) ! deleted
        Assert.Equal(
            new ExclusionRewrite(new ExclusionRewrite(Computed("users"), Computed("banned")), Computed("deleted")),
            ParseSuccess("users ! banned ! deleted"));

    [Fact]
    public void Parse_ComplexExpression_ReturnsExpectedTree() =>
        Assert.Equal(
            new ExclusionRewrite(
                new UnionRewrite(
                [
                    ThisRewrite.Default,
                    Computed("editor"),
                    new TupleToSubjectSetRewrite(Rel("parent"), Rel("viewer")),
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
    [InlineData("(this, viewer)")] // a tupleset relationship is an identifier; 'this' lexes as the keyword
    [InlineData("this ! ! banned")]
    [InlineData("...")] // the tuple grammar's sentinel cannot lex in a rewrite expression
    public void Parse_InvalidExpressions_FailsWithRewriteCode(string expression)
    {
        var failure = Assert.IsType<Result<SubjectSetRewrite>.Failure>(RewriteExpressionParser.Parse(expression));

        var error = Assert.Single(failure.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("sdl.rewrite", error.Code);
    }

    [Fact]
    public void Parse_IdentifiersOutsideTheCoreGrammar_SurfaceTheCoreErrorsAccumulated()
    {
        // Superpower's C-style identifier lexes Unicode letters, but the core identifier grammar is ASCII:
        // the exit transform's RelationshipIdentifier.Parse rejects each one and the errors accumulate
        var failure = Assert.IsType<Result<SubjectSetRewrite>.Failure>(RewriteExpressionParser.Parse("café | naïve"));

        Assert.Equal(2, failure.Errors.Length);
        Assert.All(failure.Errors, error => Assert.Equal("relationship_id.invalid", error.Code));
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
