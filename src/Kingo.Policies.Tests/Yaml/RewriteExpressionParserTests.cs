using Kingo.Policies.Yaml;

namespace Kingo.Policies.Tests.Yaml;

public class RewriteExpressionParserTests
{
    [Fact]
    public void Parse_DirectTerm_ReturnsDirectRewrite()
    {
        var result = RewriteExpressionParser.Parse("this");
        Assert.True(result.IsSucc);
        var rewrite = result.ThrowIfFail();
        Assert.IsType<DirectRewrite>(rewrite);
    }

    [Fact]
    public void Parse_ComputedSubjectSet_ReturnsComputedSubjectSetRewrite()
    {
        var result = RewriteExpressionParser.Parse("owner");
        Assert.True(result.IsSucc);
        var rewrite = result.ThrowIfFail();
        var computed = Assert.IsType<ComputedSubjectSetRewrite>(rewrite);
        Assert.Equal("owner", computed.Relation.ToString());
    }

    [Fact]
    public void Parse_TupleToSubjectSet_ReturnsTupleToSubjectSetRewrite()
    {
        var result = RewriteExpressionParser.Parse("(parent, viewer)");
        Assert.True(result.IsSucc);
        var rewrite = result.ThrowIfFail();
        var tuple = Assert.IsType<TupleToSubjectSetRewrite>(rewrite);
        Assert.Equal("parent", tuple.TuplesetRelation.ToString());
        Assert.Equal("viewer", tuple.ComputedSubjectSetRelation.ToString());
    }

    [Fact]
    public void Parse_Union_ReturnsUnionRewrite()
    {
        var result = RewriteExpressionParser.Parse("this | owner");
        Assert.True(result.IsSucc);
        var rewrite = result.ThrowIfFail();
        var union = Assert.IsType<UnionRewrite>(rewrite);
        Assert.Equal(2, union.Children.Count);
        Assert.IsType<DirectRewrite>(union.Children[0]);
        var computed = Assert.IsType<ComputedSubjectSetRewrite>(union.Children[1]);
        Assert.Equal("owner", computed.Relation.ToString());
    }

    [Fact]
    public void Parse_Intersection_ReturnsIntersectionRewrite()
    {
        var result = RewriteExpressionParser.Parse("this & viewer");
        Assert.True(result.IsSucc);
        var rewrite = result.ThrowIfFail();
        var intersection = Assert.IsType<IntersectionRewrite>(rewrite);
        Assert.Equal(2, intersection.Children.Count);
        Assert.IsType<DirectRewrite>(intersection.Children[0]);
        var computed = Assert.IsType<ComputedSubjectSetRewrite>(intersection.Children[1]);
        Assert.Equal("viewer", computed.Relation.ToString());
    }

    [Fact]
    public void Parse_Exclusion_ReturnsExclusionRewrite()
    {
        var result = RewriteExpressionParser.Parse("editor ! banned");
        Assert.True(result.IsSucc);
        var rewrite = result.ThrowIfFail();
        var exclusion = Assert.IsType<ExclusionRewrite>(rewrite);
        var include = Assert.IsType<ComputedSubjectSetRewrite>(exclusion.Include);
        Assert.Equal("editor", include.Relation.ToString());
        var exclude = Assert.IsType<ComputedSubjectSetRewrite>(exclusion.Exclude);
        Assert.Equal("banned", exclude.Relation.ToString());
    }

    [Fact]
    public void Parse_ComplexExpression_WithPrecedence_ReturnsCorrectAst()
    {
        var result = RewriteExpressionParser.Parse("(this | editor | (parent, viewer)) ! banned");
        Assert.True(result.IsSucc);
        var rewrite = result.ThrowIfFail();
        
        var exclusion = Assert.IsType<ExclusionRewrite>(rewrite);
        var include = Assert.IsType<UnionRewrite>(exclusion.Include);
        var exclude = Assert.IsType<ComputedSubjectSetRewrite>(exclusion.Exclude);

        Assert.Equal("banned", exclude.Relation.ToString());
        Assert.Equal(3, include.Children.Count);
        Assert.IsType<DirectRewrite>(include.Children[0]);
        var computed = Assert.IsType<ComputedSubjectSetRewrite>(include.Children[1]);
        Assert.Equal("editor", computed.Relation.ToString());
        var tuple = Assert.IsType<TupleToSubjectSetRewrite>(include.Children[2]);
        Assert.Equal("parent", tuple.TuplesetRelation.ToString());
        Assert.Equal("viewer", tuple.ComputedSubjectSetRelation.ToString());
    }
}
