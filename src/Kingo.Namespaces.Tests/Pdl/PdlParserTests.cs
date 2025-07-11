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
            var policy = Assert.Single(document.Policies);
            Assert.Equal("test", policy.Name.ToString());
            var rel = Assert.Single(policy.Relationships);
            Assert.Equal("owner", rel.Name.ToString());
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
            var policy = Assert.Single(document.Policies);
            var relationship = Assert.Single(policy.Relationships);
            Assert.Equal("simple", relationship.Name.ToString());
            _ = Assert.IsType<This>(relationship.SubjectSetRewrite);
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
            var policy = Assert.Single(document.Policies);
            var relationship = Assert.Single(policy.Relationships);
            var union = Assert.IsType<UnionRewrite>(relationship.SubjectSetRewrite);
            Assert.Equal(2, union.Children.Count);
            _ = Assert.IsType<This>(union.Children[0]);
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
            var policy = Assert.Single(document.Policies);
            var relationship = Assert.Single(policy.Relationships);
            var intersection = Assert.IsType<IntersectionRewrite>(relationship.SubjectSetRewrite);
            Assert.Equal(2, intersection.Children.Count);
            _ = Assert.IsType<This>(intersection.Children[0]);
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
            var policy = Assert.Single(document.Policies);
            var relationship = Assert.Single(policy.Relationships);
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
            var policy = Assert.Single(document.Policies);
            var relationship = Assert.Single(policy.Relationships);
            Assert.Equal("viewer", relationship.Name.ToString());

            var exclusion = Assert.IsType<ExclusionRewrite>(relationship.SubjectSetRewrite);
            var union = Assert.IsType<UnionRewrite>(exclusion.Include);
            Assert.Equal(3, union.Children.Count);

            // Verify the union children
            _ = Assert.IsType<This>(union.Children[0]);
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
            var folderRelationship = Assert.Single(folderPolicy.Relationships);
            Assert.Equal("owner", folderRelationship.Name.ToString());
        });
    }

    [Fact]
    public void Parse_ZanzibarExampleDocument_ParsesCorrectly()
    {
        // This replicates the Zanzibar example in PDL format
        // The dynamic object concept is implicit - no placeholders needed
        var pdl = """
            pn:doc
            re:owner
            re:editor (this | cp:owner)
            re:viewer (this | cp:editor | tp:(parent,viewer))
            """;

        var result = PdlParser.Parse(pdl);

        Assert.True(result.IsRight);
        _ = result.IfRight(document =>
        {
            var policy = Assert.Single(document.Policies);
            Assert.Equal("doc", policy.Name.ToString());
            Assert.Equal(3, policy.Relationships.Count);

            // owner relationship - simple _this
            var owner = policy.Relationships[0];
            Assert.Equal("owner", owner.Name.ToString());
            _ = Assert.IsType<This>(owner.SubjectSetRewrite);

            // editor relationship - union of _this and computed_userset
            var editor = policy.Relationships[1];
            Assert.Equal("editor", editor.Name.ToString());
            var editorUnion = Assert.IsType<UnionRewrite>(editor.SubjectSetRewrite);
            Assert.Equal(2, editorUnion.Children.Count);
            _ = Assert.IsType<This>(editorUnion.Children[0]);
            var editorComputed = Assert.IsType<ComputedSubjectSetRewrite>(editorUnion.Children[1]);
            Assert.Equal("owner", editorComputed.Relationship.ToString());

            // viewer relationship - union of _this, computed_userset, and tuple_to_userset
            // The tuple_to_userset uses concrete relationship names only
            // Dynamic object determination happens at runtime
            var viewer = policy.Relationships[2];
            Assert.Equal("viewer", viewer.Name.ToString());
            var viewerUnion = Assert.IsType<UnionRewrite>(viewer.SubjectSetRewrite);
            Assert.Equal(3, viewerUnion.Children.Count);
            _ = Assert.IsType<This>(viewerUnion.Children[0]);
            var viewerComputed = Assert.IsType<ComputedSubjectSetRewrite>(viewerUnion.Children[1]);
            Assert.Equal("editor", viewerComputed.Relationship.ToString());
            var viewerTupleToSubject = Assert.IsType<TupleToSubjectSetRewrite>(viewerUnion.Children[2]);
            Assert.Equal("parent", viewerTupleToSubject.TuplesetRelation.ToString());
            Assert.Equal("viewer", viewerTupleToSubject.ComputedSubjectSetRelation.ToString());
        });
    }
}
