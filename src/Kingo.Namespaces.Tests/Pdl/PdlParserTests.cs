using Kingo.Policies.Pdl;
using LanguageExt;

namespace Kingo.Policies.Tests.Pdl;

public sealed class PdlParserTests
{
    private const string SamplePdl = """
        # Sample PDL document
        # This demonstrates the namespace specification language

        ns:file
        re:owner
        re:editor (this | cp:owner)
        re:viewer ((this | cp:editor | tp:(parent,viewer)) ! cp:banned)
        re:auditor (this & cp:viewer)
        re:banned

        ns:folder
        re:owner
        re:viewer ((this | cp:editor | tp:(parent,viewer)) ! cp:banned)
        """;

    [Fact]
    public void Parse_ValidPdl_ReturnsDocument()
    {
        var result = PdlParser.Parse(SamplePdl);

        Assert.True(result.IsRight);
        _ = result.IfRight(document =>
        {
            Assert.Equal(2, document.Namespaces.Count);
            Assert.Equal("file", document.Namespaces[0].Name);
            Assert.Equal("folder", document.Namespaces[1].Name);
        });
    }
    
    [Fact]
    public void Parse_InvalidSyntax_ReturnsError()
    {
        var invalidPdl = "ns:file\nre:invalid syntax here";
        var result = PdlParser.Parse(invalidPdl);

        Assert.True(result.IsLeft);
    }

    [Fact]
    public void Parse_ComplexRewriteRules_ParsesCorrectly()
    {
        var complexPdl = """
            ns:document
            re:viewer ((this | cp:editor | tp:(parent,viewer)) ! cp:banned)
            """;

        var result = PdlParser.Parse(complexPdl);

        Assert.True(result.IsRight);
        _ = result.IfRight(document =>
        {
            var relationship = document.Namespaces[0].Relationships[0];
            Assert.True(relationship.RewriteRule.IsSome);

            _ = relationship.RewriteRule.IfSome(rule =>
            {
                var exclusion = Assert.IsType<ExclusionRewrite>(rule);
                _ = Assert.IsType<UnionRewrite>(exclusion.Include);
            });
        });
    }
}
