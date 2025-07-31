using Kingo.Policies.Yaml;
using LanguageExt;

namespace Kingo.Policies.Tests.Yaml;

public class RewriteExpressionParserTests
{
    [Fact]
    public void Parse_DirectTerm_ReturnsDirectRewrite()
    {
        var result = RewriteExpressionParser.Parse("this").Run();
        Assert.True(result.IsSucc);
        var rewrite = result.IfFail(ex => throw ex);
        _ = Assert.IsType<DirectRewrite>(rewrite);
    }

    [Fact]
    public void Parse_ComputedSubjectSet_ReturnsComputedSubjectSetRewrite()
    {
        var result = RewriteExpressionParser.Parse("owner").Run();
        Assert.True(result.IsSucc);
        var rewrite = result.IfFail(ex => throw ex);
        var computed = Assert.IsType<ComputedSubjectSetRewrite>(rewrite);
        Assert.Equal("owner", computed.Relation.ToString());
    }

    [Fact]
    public void Parse_TupleToSubjectSet_ReturnsTupleToSubjectSetRewrite()
    {
        var result = RewriteExpressionParser.Parse("(parent, viewer)").Run();
        Assert.True(result.IsSucc);
        var rewrite = result.IfFail(ex => throw ex);
        var tuple = Assert.IsType<TupleToSubjectSetRewrite>(rewrite);
        Assert.Equal("parent", tuple.TuplesetRelation.ToString());
        Assert.Equal("viewer", tuple.ComputedSubjectSetRelation.ToString());
    }

    [Fact]
    public void Parse_Union_ReturnsUnionRewrite()
    {
        var result = RewriteExpressionParser.Parse("this | owner").Run();
        Assert.True(result.IsSucc);
        var rewrite = result.IfFail(ex => throw ex);
        var union = Assert.IsType<UnionRewrite>(rewrite);
        Assert.Equal(2, union.Children.Count);
        _ = Assert.IsType<DirectRewrite>(union.Children[0]);
        var computed = Assert.IsType<ComputedSubjectSetRewrite>(union.Children[1]);
        Assert.Equal("owner", computed.Relation.ToString());
    }

    [Fact]
    public void Parse_Intersection_ReturnsIntersectionRewrite()
    {
        var result = RewriteExpressionParser.Parse("this & viewer").Run();
        Assert.True(result.IsSucc);
        var rewrite = result.IfFail(ex => throw ex);
        var intersection = Assert.IsType<IntersectionRewrite>(rewrite);
        Assert.Equal(2, intersection.Children.Count);
        _ = Assert.IsType<DirectRewrite>(intersection.Children[0]);
        var computed = Assert.IsType<ComputedSubjectSetRewrite>(intersection.Children[1]);
        Assert.Equal("viewer", computed.Relation.ToString());
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

    [Fact]
    public void Parse_SimpleParentheses_ReturnsCorrectAst()
    {
        var result = RewriteExpressionParser.Parse("(this)").Run();
        Assert.True(result.IsSucc, "Parsing should succeed");
        var rewrite = result.IfFail(ex => throw ex);
        _ = Assert.IsType<DirectRewrite>(rewrite);
    }
}
