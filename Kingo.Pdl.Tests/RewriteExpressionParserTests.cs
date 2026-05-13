namespace Kingo.Pdl.Tests;

public sealed class RewriteExpressionParserTests
{
    [Theory]
    [InlineData("this", typeof(ThisRewrite))]
    [InlineData("THIS", typeof(ThisRewrite))]
    [InlineData("owner", typeof(ComputedSubjectSetRewrite))]
    public void Parse_SingleTerms_ReturnsCorrectRewriteType(string input, Type expectedType)
    {
        var rewrite = RewriteExpressionParser.Parse(input);
        Assert.IsType(expectedType, rewrite);
    }

    [Fact]
    public void Parse_ComputedSubjectSet_ReturnsCorrectRelation()
    {
        var rewrite = RewriteExpressionParser.Parse("owner");
        var computed = Assert.IsType<ComputedSubjectSetRewrite>(rewrite);
        Assert.Equal("owner", computed.Relation.ToString());
    }

    [Theory]
    [InlineData("(parent, viewer)", "parent", "viewer")]
    [InlineData("(parent,\nviewer)", "parent", "viewer")]
    public void Parse_TupleToSubjectSet_ReturnsCorrectTuple(string input, string expectedFirst, string expectedSecond)
    {
        var rewrite = RewriteExpressionParser.Parse(input);
        var tuple = Assert.IsType<TupleToSubjectSetRewrite>(rewrite);
        Assert.Equal(expectedFirst, tuple.TuplesetRelation.ToString());
        Assert.Equal(expectedSecond, tuple.ComputedSubjectSetRelation.ToString());
    }

    [Theory]
    [InlineData("this | owner", typeof(UnionRewrite), typeof(ThisRewrite), typeof(ComputedSubjectSetRewrite))]
    [InlineData("this & viewer", typeof(IntersectionRewrite), typeof(ThisRewrite), typeof(ComputedSubjectSetRewrite))]
    public void Parse_BinaryOperators_ReturnsCorrectStructure(string input, Type expectedRootType, Type expectedLeftType, Type expectedRightType)
    {
        var rewrite = RewriteExpressionParser.Parse(input);

        if (expectedRootType == typeof(UnionRewrite))
        {
            var union = Assert.IsType<UnionRewrite>(rewrite);
            Assert.Equal(2, union.Children.Length);
            Assert.IsType(expectedLeftType, union.Children[0]);
            Assert.IsType(expectedRightType, union.Children[1]);
        }
        else if (expectedRootType == typeof(IntersectionRewrite))
        {
            var intersection = Assert.IsType<IntersectionRewrite>(rewrite);
            Assert.Equal(2, intersection.Children.Length);
            Assert.IsType(expectedLeftType, intersection.Children[0]);
            Assert.IsType(expectedRightType, intersection.Children[1]);
        }
    }

    [Fact]
    public void Parse_Exclusion_ReturnsExclusionRewrite()
    {
        var rewrite = RewriteExpressionParser.Parse("editor ! banned");
        var exclusion = Assert.IsType<ExclusionRewrite>(rewrite);
        var include = Assert.IsType<ComputedSubjectSetRewrite>(exclusion.Include);
        Assert.Equal("editor", include.Relation.ToString());
        var exclude = Assert.IsType<ComputedSubjectSetRewrite>(exclusion.Exclude);
        Assert.Equal("banned", exclude.Relation.ToString());
    }

    [Fact]
    public void Parse_ComplexExpression_WithPrecedence_ReturnsCorrectAst()
    {
        var rewrite = RewriteExpressionParser.Parse("(this | editor | (parent, viewer)) ! banned");

        var exclusion = Assert.IsType<ExclusionRewrite>(rewrite);
        var include = Assert.IsType<UnionRewrite>(exclusion.Include);
        var exclude = Assert.IsType<ComputedSubjectSetRewrite>(exclusion.Exclude);

        Assert.Equal("banned", exclude.Relation.ToString());
        Assert.Equal(3, include.Children.Length);
        _ = Assert.IsType<ThisRewrite>(include.Children[0]);
        var computed = Assert.IsType<ComputedSubjectSetRewrite>(include.Children[1]);
        Assert.Equal("editor", computed.Relation.ToString());
        var tuple = Assert.IsType<TupleToSubjectSetRewrite>(include.Children[2]);
        Assert.Equal("parent", tuple.TuplesetRelation.ToString());
        Assert.Equal("viewer", tuple.ComputedSubjectSetRelation.ToString());
    }

    [Fact]
    public void Parse_ComplexExpression_WithMultipleOperators_ReturnsCorrectAst()
    {
        var rewrite = RewriteExpressionParser.Parse("a & b | c & d ! e");

        // Per BNF: ! is highest (right-binds to "d ! e"); & and | share precedence, left-associative.
        // "a & b | c & d ! e" parses as ((a & b) | c) & (d ! e).
        var rootIntersection = Assert.IsType<IntersectionRewrite>(rewrite);
        Assert.Equal(2, rootIntersection.Children.Length);

        var leftUnion = Assert.IsType<UnionRewrite>(rootIntersection.Children[0]);
        Assert.Equal(2, leftUnion.Children.Length);

        var firstIntersection = Assert.IsType<IntersectionRewrite>(leftUnion.Children[0]);
        Assert.Equal(2, firstIntersection.Children.Length);
        var a = Assert.IsType<ComputedSubjectSetRewrite>(firstIntersection.Children[0]);
        Assert.Equal("a", a.Relation.ToString());
        var b = Assert.IsType<ComputedSubjectSetRewrite>(firstIntersection.Children[1]);
        Assert.Equal("b", b.Relation.ToString());

        var c = Assert.IsType<ComputedSubjectSetRewrite>(leftUnion.Children[1]);
        Assert.Equal("c", c.Relation.ToString());

        var rightExclusion = Assert.IsType<ExclusionRewrite>(rootIntersection.Children[1]);
        var d = Assert.IsType<ComputedSubjectSetRewrite>(rightExclusion.Include);
        Assert.Equal("d", d.Relation.ToString());
        var e = Assert.IsType<ComputedSubjectSetRewrite>(rightExclusion.Exclude);
        Assert.Equal("e", e.Relation.ToString());
    }

    [Fact]
    public void Parse_ChainedExclusions_ReturnsNestedExclusionRewrite()
    {
        var rewrite = RewriteExpressionParser.Parse("users ! banned ! deleted");

        // Left-associative exclusion: (users ! banned) ! deleted — matches mathematical set difference.
        var outerExclusion = Assert.IsType<ExclusionRewrite>(rewrite);
        var innerExclusion = Assert.IsType<ExclusionRewrite>(outerExclusion.Include);
        var users = Assert.IsType<ComputedSubjectSetRewrite>(innerExclusion.Include);
        var banned = Assert.IsType<ComputedSubjectSetRewrite>(innerExclusion.Exclude);
        var deleted = Assert.IsType<ComputedSubjectSetRewrite>(outerExclusion.Exclude);

        Assert.Equal("users", users.Relation.ToString());
        Assert.Equal("banned", banned.Relation.ToString());
        Assert.Equal("deleted", deleted.Relation.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("(parent viewer)")]
    [InlineData("(parent, viewer")]
    [InlineData("this |")]
    [InlineData("| this")]
    [InlineData("this | & owner")]
    public void Parse_InvalidExpressions_Throws(string input) =>
        Assert.Throws<PdlParseException>(() => RewriteExpressionParser.Parse(input));

    [Theory]
    [InlineData("(this)", typeof(ThisRewrite))]
    [InlineData("this # this is a comment", typeof(ThisRewrite))]
    [InlineData("this |\nowner", typeof(UnionRewrite))]
    [InlineData("this |\r\nowner", typeof(UnionRewrite))]
    [InlineData("this &\nviewer", typeof(IntersectionRewrite))]
    [InlineData(@"(this |
    editor | # user editors
    (parent, viewer)) ! # exclude
banned", typeof(ExclusionRewrite))]
    public void Parse_VariousExpressionFormats_ParsesCorrectly(string input, Type expectedRootType)
    {
        var rewrite = RewriteExpressionParser.Parse(input);
        Assert.IsType(expectedRootType, rewrite);
    }
}
