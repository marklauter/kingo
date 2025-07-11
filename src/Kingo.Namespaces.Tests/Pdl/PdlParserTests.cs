using Kingo.Policies.Pdl;

namespace Kingo.Policies.Tests.Pdl;

public sealed class PdlParserTests
{
    [Fact]
    public void Parse_SimpleValidPdl_ReturnsDocument()
    {
        // Start with the simplest possible valid PDL
        var simplePdl = "pn:test\nre:owner\n";
        var result = PdlParser.Parse(simplePdl);

        Assert.True(result.IsRight);
        _ = result.IfRight(document =>
        {
            Assert.Single(document.Policies);
            Assert.Equal("test", document.Policies[0].Name.ToString());
            Assert.Single(document.Policies[0].Relationships);
            Assert.Equal("owner", document.Policies[0].Relationships[0].Name.ToString());
        });
    }

    [Fact] 
    public void Parse_InvalidSyntax_ReturnsError()
    {
        var invalidPdl = "invalid:syntax\n";
        var result = PdlParser.Parse(invalidPdl);

        Assert.True(result.IsLeft);
    }

    [Fact]
    public void Parse_EmptyRelationship_DefaultsToThis()
    {
        var pdl = "pn:test\nre:simple\n";

        var result = PdlParser.Parse(pdl);

        Assert.True(result.IsRight);
        _ = result.IfRight(document =>
        {
            var relationship = document.Policies[0].Relationships[0];
            Assert.Equal("simple", relationship.Name.ToString());
            Assert.IsType<This>(relationship.SubjectSetRewrite);
        });
    }

    [Fact]
    public void Parse_UnionRewrite_ParsesCorrectly()
    {
        var pdl = "pn:test\nre:access (this | cp:owner)\n";

        var result = PdlParser.Parse(pdl);

        Assert.True(result.IsRight);
        _ = result.IfRight(document =>
        {
            var relationship = document.Policies[0].Relationships[0];
            var union = Assert.IsType<UnionRewrite>(relationship.SubjectSetRewrite);
            Assert.Equal(2, union.Children.Count);
            Assert.IsType<This>(union.Children[0]);
            var computed = Assert.IsType<ComputedSubjectSetRewrite>(union.Children[1]);
            Assert.Equal("owner", computed.Relationship.ToString());
        });
    }

    [Fact]
    public void Parse_IntersectionRewrite_ParsesCorrectly()
    {
        var pdl = "pn:test\nre:restricted (this & cp:approved)\n";

        var result = PdlParser.Parse(pdl);

        Assert.True(result.IsRight);
        _ = result.IfRight(document =>
        {
            var relationship = document.Policies[0].Relationships[0];
            var intersection = Assert.IsType<IntersectionRewrite>(relationship.SubjectSetRewrite);
            Assert.Equal(2, intersection.Children.Count);
            Assert.IsType<This>(intersection.Children[0]);
            var computed = Assert.IsType<ComputedSubjectSetRewrite>(intersection.Children[1]);
            Assert.Equal("approved", computed.Relationship.ToString());
        });
    }

    [Fact]
    public void Parse_TupleToSubjectSetRewrite_ParsesCorrectly()
    {
        var pdl = "pn:test\nre:inherited tp:(parent,owner)\n";

        var result = PdlParser.Parse(pdl);

        Assert.True(result.IsRight);
        _ = result.IfRight(document =>
        {
            var relationship = document.Policies[0].Relationships[0];
            var tupleToSubject = Assert.IsType<TupleToSubjectSetRewrite>(relationship.SubjectSetRewrite);
            Assert.Equal("parent", tupleToSubject.TuplesetRelation.ToString());
            Assert.Equal("owner", tupleToSubject.ComputedSubjectSetRelation.ToString());
        });
    }

    [Fact]
    public void Parse_ComplexRewriteRules_ParsesCorrectly()
    {
        var complexPdl = "pn:document\nre:viewer ((this | cp:editor | tp:(parent,viewer)) ! cp:banned)\n";

        var result = PdlParser.Parse(complexPdl);

        Assert.True(result.IsRight);
        _ = result.IfRight(document =>
        {
            var relationship = document.Policies[0].Relationships[0];
            Assert.Equal("viewer", relationship.Name.ToString());

            var exclusion = Assert.IsType<ExclusionRewrite>(relationship.SubjectSetRewrite);
            var union = Assert.IsType<UnionRewrite>(exclusion.Include);
            Assert.Equal(3, union.Children.Count);
            
            // Verify the union children
            Assert.IsType<This>(union.Children[0]);
            var computed = Assert.IsType<ComputedSubjectSetRewrite>(union.Children[1]);
            Assert.Equal("editor", computed.Relationship.ToString());
            var tupleToSubject = Assert.IsType<TupleToSubjectSetRewrite>(union.Children[2]);
            Assert.Equal("parent", tupleToSubject.TuplesetRelation.ToString());
            Assert.Equal("viewer", tupleToSubject.ComputedSubjectSetRelation.ToString());
            
            // Verify the exclusion
            var bannedComputed = Assert.IsType<ComputedSubjectSetRewrite>(exclusion.Exclude);
            Assert.Equal("banned", bannedComputed.Relationship.ToString());
        });
    }

    [Fact]
    public void Parse_MultiplePolicesAndComments_ParsesCorrectly()
    {
        var pdl = "# Comment\npn:file\nre:owner\nre:editor (this | cp:owner)\n\npn:folder\nre:owner\n";

        var result = PdlParser.Parse(pdl);

        Assert.True(result.IsRight);
        _ = result.IfRight(document =>
        {
            Assert.Equal(2, document.Policies.Count);
            
            var filePolicy = document.Policies[0];
            Assert.Equal("file", filePolicy.Name.ToString());
            Assert.Equal(2, filePolicy.Relationships.Count);
            Assert.Equal("owner", filePolicy.Relationships[0].Name.ToString());
            Assert.Equal("editor", filePolicy.Relationships[1].Name.ToString());
            
            var folderPolicy = document.Policies[1];
            Assert.Equal("folder", folderPolicy.Name.ToString());
            Assert.Single(folderPolicy.Relationships);
            Assert.Equal("owner", folderPolicy.Relationships[0].Name.ToString());
        });
    }
}
