using Kingo.Policies.Yaml;
using LanguageExt;

namespace Kingo.Policies.Tests.Yaml;

public class RewriteExpressionParserTests
{
    [Theory]
    [InlineData("this", typeof(DirectRewrite))]
    [InlineData("THIS", typeof(DirectRewrite))] 
    [InlineData("owner", typeof(ComputedSubjectSetRewrite))]
    public void Parse_SingleTerms_ReturnsCorrectRewriteType(string input, Type expectedType)
    {
        var result = RewriteExpressionParser.Parse(input).Run();
        Assert.True(result.IsSucc);
        var rewrite = result.IfFail(ex => throw ex);
        Assert.IsType(expectedType, rewrite);
    }

    [Fact]
    public void Parse_ComputedSubjectSet_ReturnsCorrectRelation()
    {
        var result = RewriteExpressionParser.Parse("owner").Run();
        Assert.True(result.IsSucc);
        var rewrite = result.IfFail(ex => throw ex);
        var computed = Assert.IsType<ComputedSubjectSetRewrite>(rewrite);
        Assert.Equal("owner", computed.Relation.ToString());
    }

    [Theory]
    [InlineData("(parent, viewer)", "parent", "viewer")]
    [InlineData("(parent,\nviewer)", "parent", "viewer")]
    public void Parse_TupleToSubjectSet_ReturnsCorrectTuple(string input, string expectedFirst, string expectedSecond)
    {
        var result = RewriteExpressionParser.Parse(input).Run();
        Assert.True(result.IsSucc);
        var rewrite = result.IfFail(ex => throw ex);
        var tuple = Assert.IsType<TupleToSubjectSetRewrite>(rewrite);
        Assert.Equal(expectedFirst, tuple.TuplesetRelation.ToString());
        Assert.Equal(expectedSecond, tuple.ComputedSubjectSetRelation.ToString());
    }

    [Theory]
    [InlineData("this | owner", typeof(UnionRewrite), typeof(DirectRewrite), typeof(ComputedSubjectSetRewrite))]
    [InlineData("this & viewer", typeof(IntersectionRewrite), typeof(DirectRewrite), typeof(ComputedSubjectSetRewrite))]
    public void Parse_BinaryOperators_ReturnsCorrectStructure(string input, Type expectedRootType, Type expectedLeftType, Type expectedRightType)
    {
        var result = RewriteExpressionParser.Parse(input).Run();
        Assert.True(result.IsSucc);
        var rewrite = result.IfFail(ex => throw ex);
        
        if (expectedRootType == typeof(UnionRewrite))
        {
            var union = Assert.IsType<UnionRewrite>(rewrite);
            Assert.Equal(2, union.Children.Count);
            Assert.IsType(expectedLeftType, union.Children[0]);
            Assert.IsType(expectedRightType, union.Children[1]);
        }
        else if (expectedRootType == typeof(IntersectionRewrite))
        {
            var intersection = Assert.IsType<IntersectionRewrite>(rewrite);
            Assert.Equal(2, intersection.Children.Count);
            Assert.IsType(expectedLeftType, intersection.Children[0]);
            Assert.IsType(expectedRightType, intersection.Children[1]);
        }
    }

    [Fact]
    public void Parse_Exclusion_ReturnsExclusionRewrite()
    {
        var result = RewriteExpressionParser.Parse("editor ! banned").Run();
        Assert.True(result.IsSucc);
        var rewrite = result.IfFail(ex => throw ex);
        var exclusion = Assert.IsType<ExclusionRewrite>(rewrite);
        var include = Assert.IsType<ComputedSubjectSetRewrite>(exclusion.Include);
        Assert.Equal("editor", include.Relation.ToString());
        var exclude = Assert.IsType<ComputedSubjectSetRewrite>(exclusion.Exclude);
        Assert.Equal("banned", exclude.Relation.ToString());
    }

    [Fact]
    public void Parse_ComplexExpression_WithPrecedence_ReturnsCorrectAst()
    {
        var result = RewriteExpressionParser.Parse("(this | editor | (parent, viewer)) ! banned").Run();
        Assert.True(result.IsSucc);
        var rewrite = result.IfFail(ex => throw ex);

        var exclusion = Assert.IsType<ExclusionRewrite>(rewrite);
        var include = Assert.IsType<UnionRewrite>(exclusion.Include);
        var exclude = Assert.IsType<ComputedSubjectSetRewrite>(exclusion.Exclude);

        Assert.Equal("banned", exclude.Relation.ToString());
        Assert.Equal(3, include.Children.Count);
        _ = Assert.IsType<DirectRewrite>(include.Children[0]);
        var computed = Assert.IsType<ComputedSubjectSetRewrite>(include.Children[1]);
        Assert.Equal("editor", computed.Relation.ToString());
        var tuple = Assert.IsType<TupleToSubjectSetRewrite>(include.Children[2]);
        Assert.Equal("parent", tuple.TuplesetRelation.ToString());
        Assert.Equal("viewer", tuple.ComputedSubjectSetRelation.ToString());
    }

    [Fact]
    public void Parse_ComplexExpression_WithMultipleOperators_ReturnsCorrectAst()
    {
        var result = RewriteExpressionParser.Parse("a & b | c & d ! e").Run();
        Assert.True(result.IsSucc);
        var rewrite = result.IfFail(ex => throw ex);

        var union = Assert.IsType<UnionRewrite>(rewrite);
        Assert.Equal(2, union.Children.Count);

        var firstIntersection = Assert.IsType<IntersectionRewrite>(union.Children[0]);
        Assert.Equal(2, firstIntersection.Children.Count);
        var a = Assert.IsType<ComputedSubjectSetRewrite>(firstIntersection.Children[0]);
        Assert.Equal("a", a.Relation.ToString());
        var b = Assert.IsType<ComputedSubjectSetRewrite>(firstIntersection.Children[1]);
        Assert.Equal("b", b.Relation.ToString());

        var exclusion = Assert.IsType<ExclusionRewrite>(union.Children[1]);
        var secondIntersection = Assert.IsType<IntersectionRewrite>(exclusion.Include);
        Assert.Equal(2, secondIntersection.Children.Count);
        var c = Assert.IsType<ComputedSubjectSetRewrite>(secondIntersection.Children[0]);
        Assert.Equal("c", c.Relation.ToString());
        var d = Assert.IsType<ComputedSubjectSetRewrite>(secondIntersection.Children[1]);
        Assert.Equal("d", d.Relation.ToString());
        var e = Assert.IsType<ComputedSubjectSetRewrite>(exclusion.Exclude);
        Assert.Equal("e", e.Relation.ToString());
    }

    [Fact]
    public void Parse_ChainedExclusions_ReturnsNestedExclusionRewrite()
    {
        var result = RewriteExpressionParser.Parse("users ! banned ! deleted").Run();
        Assert.True(result.IsSucc);
        var rewrite = result.IfFail(ex => throw ex);

        // Should be nested left-associative: (users ! banned) ! deleted
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
    [InlineData("(this)")]
    [InlineData("this # this is a comment")]
    public void Parse_ValidExpressions_WithVariations_ParsesCorrectly(string input)
    {
        var result = RewriteExpressionParser.Parse(input).Run();
        Assert.True(result.IsSucc);
        var rewrite = result.IfFail(ex => throw ex);
        _ = Assert.IsType<DirectRewrite>(rewrite);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("(parent viewer)")]          // Missing comma
    [InlineData("(parent, viewer")]          // Missing right paren
    [InlineData("this |")]                   // Trailing operator
    [InlineData("| this")]                   // Leading operator
    [InlineData("this | & owner")]           // Consecutive operators
    public void Parse_InvalidExpressions_ReturnsError(string input)
    {
        var result = RewriteExpressionParser.Parse(input).Run();
        Assert.True(result.IsFail);
    }

    [Theory]
    [InlineData("this |\nowner &\nviewer", typeof(UnionRewrite))]
    [InlineData("this |\r\nowner", typeof(UnionRewrite))]
    [InlineData(@"(this |
    editor | # user editors
    (parent, viewer)) ! # exclude
banned", typeof(ExclusionRewrite))]
    public void Parse_MultiLineExpressions_ParsesCorrectly(string input, Type expectedRootType)
    {
        var result = RewriteExpressionParser.Parse(input).Run();
        Assert.True(result.IsSucc);
        var rewrite = result.IfFail(ex => throw ex);
        Assert.IsType(expectedRootType, rewrite);
    }
}
