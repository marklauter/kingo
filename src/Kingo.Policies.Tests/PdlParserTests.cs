using LanguageExt;
using static LanguageExt.Prelude;

namespace Kingo.Policies.Tests;

public sealed class PdlParserTests
{
    [Fact]
    public void Parse_SimpleYamlPolicy_ReturnsCorrectAst()
    {
        const string yaml = """
            file:
              - owner
              - editor: this | owner
            """;

        var expected =
            Seq(
                new Namespace(
                    NamespaceIdentifier.From("file"),
                    Seq(
                        new Relation(RelationIdentifier.From("owner"), ThisRewrite.Default),
                        new Relation(
                            RelationIdentifier.From("editor"),
                            new UnionRewrite(Seq<SubjectSetRewrite>(
                                ThisRewrite.Default,
                                new ComputedSubjectSetRewrite(RelationIdentifier.From("owner"))
                            ))
                        )
                    )
                )
            );

        var result = PdlParser.Parse(yaml).Run();
        Assert.True(result.IsSucc);
        var doc = result.IfFail(ex => throw ex);
        Assert.Equal(expected, doc.Namespaces);
    }

    [Fact]
    public void Parse_ComplexYamlPolicy_ReturnsCorrectAst()
    {
        var yaml = File.ReadAllText("Data/doc.policy.yml");

        var filePolicy = new Namespace(
            NamespaceIdentifier.From("file"),
            Seq(
                new Relation(RelationIdentifier.From("owner"), ThisRewrite.Default),
                new Relation(
                    RelationIdentifier.From("editor"),
                    new UnionRewrite(Seq<SubjectSetRewrite>(
                        ThisRewrite.Default,
                        new ComputedSubjectSetRewrite(RelationIdentifier.From("owner"))
                    ))
                ),
                new Relation(
                    RelationIdentifier.From("viewer"),
                    new ExclusionRewrite(
                        new UnionRewrite(Seq<SubjectSetRewrite>(
                            ThisRewrite.Default,
                            new ComputedSubjectSetRewrite(RelationIdentifier.From("editor")),
                            new TupleToSubjectSetRewrite(RelationIdentifier.From("parent"), RelationIdentifier.From("viewer"))
                        )),
                        new ComputedSubjectSetRewrite(RelationIdentifier.From("banned"))
                    )
                ),
                new Relation(
                    RelationIdentifier.From("auditor"),
                    new IntersectionRewrite(Seq<SubjectSetRewrite>(
                        ThisRewrite.Default,
                        new ComputedSubjectSetRewrite(RelationIdentifier.From("viewer"))
                    ))
                ),
                new Relation(RelationIdentifier.From("banned"), ThisRewrite.Default)
            )
        );

        var folderPolicy = new Namespace(
            NamespaceIdentifier.From("folder"),
            Seq(
                new Relation(RelationIdentifier.From("owner"), ThisRewrite.Default),
                new Relation(
                    RelationIdentifier.From("viewer"),
                    new ExclusionRewrite(
                        new UnionRewrite(Seq<SubjectSetRewrite>(
                            ThisRewrite.Default,
                            new TupleToSubjectSetRewrite(RelationIdentifier.From("parent"), RelationIdentifier.From("viewer"))
                        )),
                        new ComputedSubjectSetRewrite(RelationIdentifier.From("banned"))
                    )
                ),
                new Relation(RelationIdentifier.From("banned"), ThisRewrite.Default)
            )
        );

        var expected = Seq(filePolicy, folderPolicy);

        var result = PdlParser.Parse(yaml).Run();
        Assert.True(result.IsSucc);
        var doc = result.IfFail(ex => throw ex);
        Assert.Equal(expected, doc.Namespaces);
        Assert.Equal(yaml, doc.Yaml);
    }

    [Theory]
    [InlineData("file:\n  - owner")]
    [InlineData("file:\n  - owner\n  - editor: this")]
    [InlineData("file:\n  - viewer: this | owner")]
    [InlineData("file:\n  - viewer: this & owner")]
    [InlineData("file:\n  - viewer: this ! owner")]
    public void Parse_ValidYamlFormats_ReturnsSuccess(string yaml) =>
        Assert.True(PdlParser.Parse(yaml).Run().IsSucc);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("null")]
    [InlineData("invalid: yaml: content")]
    [InlineData("file:\n  - owner: invalid expression syntax")]
    [InlineData("file:\n  - viewer: this |")]
    [InlineData("file:\n  - viewer: | this")]
    [InlineData("file:\n  - viewer: this & & owner")]
    [InlineData("file name:\n  - owner")]           // Space in namespace
    [InlineData("file-name:\n  - owner")]           // Hyphen in namespace  
    [InlineData("123file:\n  - owner")]             // Number prefix
    [InlineData("file.ext:\n  - owner")]            // Dot in namespace
    [InlineData("file:\n  - viewer: invalid-identifier")]      // Invalid identifier in expression
    [InlineData("file:\n  - viewer: (incomplete tuple")]       // Malformed tuple
    [InlineData("file:\n  - owner-name")]           // Hyphen in relation
    [InlineData("file:\n  - 123owner")]             // Number prefix  
    [InlineData("file:\n  - owner.ext")]            // Dot in relation
    [InlineData("file:\n  - viewer: 123invalid")]          // Number prefix in rewrite expression
    [InlineData("file:\n  - viewer: invalid-name")]        // Hyphen in rewrite expression  
    [InlineData("file:\n  - viewer: (parent, child, extra)")] // Three element tuple
    [InlineData("[]")]                              // Array at root
    [InlineData("scalar")]                          // Scalar at root
    public void Parse_InvalidYamlFormats_ReturnsFailure(string yaml) =>
        Assert.True(PdlParser.Parse(yaml).Run().IsFail);

    [Fact]
    public void Parse_Empty_ReturnsEmpty()
    {
        var result = PdlParser.Parse("{}").Run();
        Assert.True(result.IsSucc);
        var pdl = result.IfFail(ex => throw ex);
        Assert.Empty(pdl.Namespaces);
    }

    [Theory]
    [InlineData("file:\n  []")]
    [InlineData("file:\n  - ")]
    [InlineData("file: null")]
    [InlineData("123: \n  - owner")]
    [InlineData("file-name:\n  - owner-name")]
    [InlineData("file:\n  - owner\n  - owner")]
    public void Parse_EdgeCaseYamlFormats_HandlesGracefully(string yaml) =>
        // Should either return success or failure, but not throw
        _ = PdlParser.Parse(yaml).Run();

    [Fact]
    public void Parse_NullInput_ReturnsFailure() =>
        Assert.True(PdlParser.Parse(null!).Run().IsFail);

    [Theory]
    [InlineData("file:\n  - viewer: (this)")]
    [InlineData("file:\n  - viewer: this # comment")]
    [InlineData("file:\n  - viewer: (parent, child)")]
    [InlineData("file:\n  - viewer: this | (parent, child) & owner ! banned")]
    public void Parse_ComplexExpressionFormats_ReturnsSuccess(string yaml) =>
        Assert.True(PdlParser.Parse(yaml).Run().IsSucc);

    [Fact]
    public async Task Parse_ConcurrentCalls_HandlesCorrectly()
    {
        const string yaml = "file:\n  - owner\n  - editor: this | owner";

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => PdlParser.Parse(yaml).Run()))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, result => Assert.True(result.IsSucc));
    }

    [Fact]
    public void Parse_LargeDocument_HandlesCorrectly()
    {
        var namespaces = Enumerable.Range(0, 50)
            .Select(i => $"namespace{i}:\n  - owner\n  - editor: this | owner");
        var yaml = string.Join("\n", namespaces);

        var result = PdlParser.Parse(yaml).Run();
        Assert.True(result.IsSucc);

        var doc = result.IfFail(ex => throw ex);
        Assert.Equal(50, doc.Namespaces.Count);
    }
}
