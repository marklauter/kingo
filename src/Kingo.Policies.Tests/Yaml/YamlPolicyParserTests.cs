using Kingo.Policies.Yaml;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Kingo.Policies.Tests.Yaml;

public sealed class YamlPolicyParserTests
{
    [Fact]
    public void Parse_SimpleYamlPolicy_ReturnsCorrectAst()
    {
        const string yaml = """
            file:
              - owner
              - editor: this | owner
            """;

        var expected = new NamespaceSet(
            Seq(
                new Namespace(
                    NamespaceIdentifier.From("file"),
                    Seq(
                        new Relation(RelationIdentifier.From("owner"), DirectRewrite.Default),
                        new Relation(
                            RelationIdentifier.From("editor"),
                            new UnionRewrite(Seq<SubjectSetRewrite>(
                                DirectRewrite.Default,
                                new ComputedSubjectSetRewrite(RelationIdentifier.From("owner"))
                            ))
                        )
                    )
                )
            )
        );

        var result = YamlPolicyParser.Parse(yaml).Run();
        Assert.True(result.IsSucc);
        var doc = result.IfFail(ex => throw ex);
        Assert.Equal(expected, doc.PolicySet);
    }

    [Fact]
    public void Parse_ComplexYamlPolicy_ReturnsCorrectAst()
    {
        var yaml = File.ReadAllText("Data/doc.policy.yml");

        var filePolicy = new Namespace(
            NamespaceIdentifier.From("file"),
            Seq(
                new Relation(RelationIdentifier.From("owner"), DirectRewrite.Default),
                new Relation(
                    RelationIdentifier.From("editor"),
                    new UnionRewrite(Seq<SubjectSetRewrite>(
                        DirectRewrite.Default,
                        new ComputedSubjectSetRewrite(RelationIdentifier.From("owner"))
                    ))
                ),
                new Relation(
                    RelationIdentifier.From("viewer"),
                    new ExclusionRewrite(
                        new UnionRewrite(Seq<SubjectSetRewrite>(
                            DirectRewrite.Default,
                            new ComputedSubjectSetRewrite(RelationIdentifier.From("editor")),
                            new TupleToSubjectSetRewrite(RelationIdentifier.From("parent"), RelationIdentifier.From("viewer"))
                        )),
                        new ComputedSubjectSetRewrite(RelationIdentifier.From("banned"))
                    )
                ),
                new Relation(
                    RelationIdentifier.From("auditor"),
                    new IntersectionRewrite(Seq<SubjectSetRewrite>(
                        DirectRewrite.Default,
                        new ComputedSubjectSetRewrite(RelationIdentifier.From("viewer"))
                    ))
                ),
                new Relation(RelationIdentifier.From("banned"), DirectRewrite.Default)
            )
        );

        var folderPolicy = new Namespace(
            NamespaceIdentifier.From("folder"),
            Seq(
                new Relation(RelationIdentifier.From("owner"), DirectRewrite.Default),
                new Relation(
                    RelationIdentifier.From("viewer"),
                    new ExclusionRewrite(
                        new UnionRewrite(Seq<SubjectSetRewrite>(
                            DirectRewrite.Default,
                            new TupleToSubjectSetRewrite(RelationIdentifier.From("parent"), RelationIdentifier.From("viewer"))
                        )),
                        new ComputedSubjectSetRewrite(RelationIdentifier.From("banned"))
                    )
                ),
                new Relation(RelationIdentifier.From("banned"), DirectRewrite.Default)
            )
        );

        var expected = new NamespaceSet(Seq(filePolicy, folderPolicy));

        var result = YamlPolicyParser.Parse(yaml).Run();
        Assert.True(result.IsSucc);
        var doc = result.IfFail(ex => throw ex);
        Assert.Equal(expected, doc.PolicySet);
        Assert.Equal(yaml, doc.Pdl);
    }

    [Theory]
    [InlineData("file:\n  - owner")]
    [InlineData("file:\n  - owner\n  - editor: this")]
    [InlineData("file:\n  - viewer: this | owner")]
    [InlineData("file:\n  - viewer: this & owner")]
    [InlineData("file:\n  - viewer: this ! owner")]
    public void Parse_ValidYamlFormats_ReturnsSuccess(string yaml)
    {
        var result = YamlPolicyParser.Parse(yaml).Run();
        Assert.True(result.IsSucc);
    }

    [Fact]
    public void Parse_DocPolicyYmlFile_ReturnsValidDocument()
    {
        var yaml = File.ReadAllText("Data/doc.policy.yml");

        var result = YamlPolicyParser.Parse(yaml).Run();

        Assert.True(result.IsSucc);
        var doc = result.IfFail(ex => throw ex);
        Assert.Equal(yaml, doc.Pdl);

        // Basic validation - document contains expected namespaces
        Assert.Contains("file:", yaml);
        Assert.Contains("folder:", yaml);
        Assert.Contains("viewer: (this | editor | (parent, viewer)) ! banned", yaml);
        Assert.Contains("auditor: this & viewer", yaml);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("null")]
    [InlineData("invalid: yaml: content")]
    [InlineData("file:\n  - owner: invalid expression syntax")]
    [InlineData("file:\n  - viewer: this |")]
    [InlineData("file:\n  - viewer: | this")]
    [InlineData("file:\n  - viewer: this & & owner")]
    public void Parse_InvalidYamlFormats_ReturnsFailure(string yaml)
    {
        var result = YamlPolicyParser.Parse(yaml).Run();
        Assert.True(result.IsFail);
    }

    [Theory]
    [InlineData("file:\n  []")]
    [InlineData("file:\n  - ")]
    [InlineData("file: null")]
    [InlineData("123: \n  - owner")]
    [InlineData("file-name:\n  - owner-name")]
    [InlineData("file:\n  - owner\n  - owner")]
    public void Parse_EdgeCaseYamlFormats_HandlesGracefully(string yaml)
    {
        // Should either return success or failure, but not throw
        _ = YamlPolicyParser.Parse(yaml).Run();
    }

    [Fact]
    public void Parse_NullInput_ReturnsFailure()
    {
        var result = YamlPolicyParser.Parse(null!).Run();
        Assert.True(result.IsFail);
    }

    [Fact]
    public void Parse_EmptyNamespace_HandlesGracefully()
    {
        const string yaml = "file:\n  []";
        _ = YamlPolicyParser.Parse(yaml).Run();
    }

    [Theory]
    [InlineData("file:\n  - viewer: (this)")]
    [InlineData("file:\n  - viewer: this # comment")]
    [InlineData("file:\n  - viewer: (parent, child)")]
    [InlineData("file:\n  - viewer: this | (parent, child) & owner ! banned")]
    public void Parse_ComplexExpressionFormats_ReturnsSuccess(string yaml)
    {
        var result = YamlPolicyParser.Parse(yaml).Run();
        Assert.True(result.IsSucc);
    }
}
