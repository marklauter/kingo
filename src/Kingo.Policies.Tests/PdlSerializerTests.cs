using LanguageExt;
using static LanguageExt.Prelude;

namespace Kingo.Policies.Tests;

public sealed class PdlSerializerTests
{
    [Fact]
    public void Serialize_SimpleDocument_ReturnsValidYaml()
    {
        var document = new PdlDocument(
            "original",
            Seq(
                new Namespace(
                    NamespaceIdentifier.From("file"),
                    Seq(
                        new Relation(RelationIdentifier.From("owner"), ThisRewrite.Default)
                    )
                )
            )
        );

        var result = PdlSerializer.Serialize(document).Run();
        
        Assert.True(result.IsSucc);
        var yaml = result.IfFail(ex => throw ex);
        Assert.Contains("file:", yaml);
        Assert.Contains("- owner", yaml);
    }

    [Fact]
    public void Serialize_ComplexDocument_ReturnsValidYaml()
    {
        var document = new PdlDocument(
            "original",
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
                        )
                    )
                )
            )
        );

        var result = PdlSerializer.Serialize(document).Run();
        
        Assert.True(result.IsSucc);
        var yaml = result.IfFail(ex => throw ex);
        
        // Verify namespace structure
        Assert.Contains("file:", yaml);
        
        // Verify simple relation
        Assert.Contains("- owner", yaml);
        
        // Verify complex relations
        Assert.Contains("editor: this | owner", yaml);
        Assert.Contains("auditor: this & viewer", yaml);
        Assert.Contains("! banned", yaml); // exclusion
        Assert.Contains("(parent, viewer)", yaml); // tuple-to-subjectset
    }

    [Fact]
    public void Serialize_MultipleNamespaces_ReturnsValidYaml()
    {
        var document = new PdlDocument(
            "original",
            Seq(
                new Namespace(
                    NamespaceIdentifier.From("file"),
                    Seq(new Relation(RelationIdentifier.From("owner"), ThisRewrite.Default))
                ),
                new Namespace(
                    NamespaceIdentifier.From("folder"),
                    Seq(new Relation(RelationIdentifier.From("viewer"), ThisRewrite.Default))
                )
            )
        );

        var result = PdlSerializer.Serialize(document).Run();
        
        Assert.True(result.IsSucc);
        var yaml = result.IfFail(ex => throw ex);
        
        Assert.Contains("file:", yaml);
        Assert.Contains("folder:", yaml);
        Assert.Contains("- owner", yaml);
        Assert.Contains("- viewer", yaml);
    }

    [Fact]
    public void Serialize_EmptyDocument_ReturnsEmptyYaml()
    {
        var document = new PdlDocument("original", Seq<Namespace>());

        var result = PdlSerializer.Serialize(document).Run();
        
        Assert.True(result.IsSucc);
        var yaml = result.IfFail(ex => throw ex);
        Assert.Equal("{}\r\n", yaml);
    }

    [Fact]
    public void RoundTrip_SimpleDocument_PreservesSemantics()
    {
        const string originalYaml = """
            file:
              - owner
              - editor: this | owner
            """;

        var parseResult = PdlParser.Parse(originalYaml).Run();
        Assert.True(parseResult.IsSucc);
        var originalDoc = parseResult.IfFail(ex => throw ex);

        var serializeResult = PdlSerializer.Serialize(originalDoc).Run();
        Assert.True(serializeResult.IsSucc);
        var serializedYaml = serializeResult.IfFail(ex => throw ex);

        var reparseResult = PdlParser.Parse(serializedYaml).Run();
        Assert.True(reparseResult.IsSucc);
        var reparsedDoc = reparseResult.IfFail(ex => throw ex);

        // Compare the AST structures (not the original YAML strings)
        Assert.Equal(originalDoc.Namespaces, reparsedDoc.Namespaces);
    }

    [Fact]
    public void RoundTrip_ComplexDocument_PreservesSemantics()
    {
        var yaml = File.ReadAllText("Data/doc.policy.yml");

        var parseResult = PdlParser.Parse(yaml).Run();
        Assert.True(parseResult.IsSucc);
        var originalDoc = parseResult.IfFail(ex => throw ex);

        var serializeResult = PdlSerializer.Serialize(originalDoc).Run();
        Assert.True(serializeResult.IsSucc);
        var serializedYaml = serializeResult.IfFail(ex => throw ex);

        var reparseResult = PdlParser.Parse(serializedYaml).Run();
        Assert.True(reparseResult.IsSucc);
        var reparsedDoc = reparseResult.IfFail(ex => throw ex);

        Assert.Equal(originalDoc.Namespaces, reparsedDoc.Namespaces);
    }

    [Fact]
    public void Serialize_AllRewriteTypes_ReturnsCorrectExpressions()
    {
        var document = new PdlDocument(
            "test",
            Seq(
                new Namespace(
                    NamespaceIdentifier.From("test"),
                    Seq(
                        // ThisRewrite
                        new Relation(RelationIdentifier.From("direct"), ThisRewrite.Default),
                        
                        // ComputedSubjectSetRewrite
                        new Relation(
                            RelationIdentifier.From("computed"), 
                            new ComputedSubjectSetRewrite(RelationIdentifier.From("owner"))
                        ),
                        
                        // TupleToSubjectSetRewrite
                        new Relation(
                            RelationIdentifier.From("tuple"), 
                            new TupleToSubjectSetRewrite(RelationIdentifier.From("parent"), RelationIdentifier.From("viewer"))
                        ),
                        
                        // UnionRewrite
                        new Relation(
                            RelationIdentifier.From("union"), 
                            new UnionRewrite(Seq<SubjectSetRewrite>(
                                ThisRewrite.Default,
                                new ComputedSubjectSetRewrite(RelationIdentifier.From("owner"))
                            ))
                        ),
                        
                        // IntersectionRewrite
                        new Relation(
                            RelationIdentifier.From("intersection"), 
                            new IntersectionRewrite(Seq<SubjectSetRewrite>(
                                ThisRewrite.Default,
                                new ComputedSubjectSetRewrite(RelationIdentifier.From("viewer"))
                            ))
                        ),
                        
                        // ExclusionRewrite
                        new Relation(
                            RelationIdentifier.From("exclusion"), 
                            new ExclusionRewrite(
                                ThisRewrite.Default,
                                new ComputedSubjectSetRewrite(RelationIdentifier.From("banned"))
                            )
                        )
                    )
                )
            )
        );

        var result = PdlSerializer.Serialize(document).Run();
        Assert.True(result.IsSucc);
        var yaml = result.IfFail(ex => throw ex);

        // Verify each rewrite type is serialized correctly  
        Assert.Contains("- direct", yaml);  // ThisRewrite as scalar
        Assert.Contains("computed: owner", yaml);  // ComputedSubjectSetRewrite
        Assert.Contains("tuple: (parent, viewer)", yaml);  // TupleToSubjectSetRewrite
        Assert.Contains("union: this | owner", yaml);  // UnionRewrite
        Assert.Contains("intersection: this & viewer", yaml);  // IntersectionRewrite
        Assert.Contains("exclusion: this ! banned", yaml);  // ExclusionRewrite
    }

    [Theory]
    [InlineData("file:\n  - owner")]
    [InlineData("file:\n  - owner\n  - editor: this")]
    [InlineData("file:\n  - viewer: this | owner")]
    [InlineData("file:\n  - viewer: this & owner")]
    [InlineData("file:\n  - viewer: this ! owner")]
    [InlineData("file:\n  - viewer: (parent, child)")]
    public void RoundTrip_VariousFormats_PreservesSemantics(string originalYaml)
    {
        var parseResult = PdlParser.Parse(originalYaml).Run();
        Assert.True(parseResult.IsSucc);
        var originalDoc = parseResult.IfFail(ex => throw ex);

        var serializeResult = PdlSerializer.Serialize(originalDoc).Run();
        Assert.True(serializeResult.IsSucc);
        var serializedYaml = serializeResult.IfFail(ex => throw ex);

        var reparseResult = PdlParser.Parse(serializedYaml).Run();
        Assert.True(reparseResult.IsSucc);
        var reparsedDoc = reparseResult.IfFail(ex => throw ex);

        Assert.Equal(originalDoc.Namespaces, reparsedDoc.Namespaces);
    }

    [Fact]
    public async Task Serialize_ConcurrentCalls_HandlesCorrectly()
    {
        var document = new PdlDocument(
            "test",
            Seq(
                new Namespace(
                    NamespaceIdentifier.From("file"),
                    Seq(new Relation(RelationIdentifier.From("owner"), ThisRewrite.Default))
                )
            )
        );

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => PdlSerializer.Serialize(document).Run()))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, result => Assert.True(result.IsSucc));
        
        // All results should be identical
        var firstYaml = results[0].IfFail(ex => throw ex);
        Assert.All(results, result => 
        {
            var yaml = result.IfFail(ex => throw ex);
            Assert.Equal(firstYaml, yaml);
        });
    }

    [Fact]
    public void Serialize_LargeDocument_HandlesCorrectly()
    {
        var namespaces = Enumerable.Range(0, 50)
            .Select(i => new Namespace(
                NamespaceIdentifier.From($"namespace{i}"),
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
            ));

        var document = new PdlDocument("large", toSeq(namespaces));

        var result = PdlSerializer.Serialize(document).Run();
        Assert.True(result.IsSucc);

        var yaml = result.IfFail(ex => throw ex);
        
        // Verify it contains all namespaces
        for (var i = 0; i < 50; i++)
            Assert.Contains($"namespace{i}:", yaml);
    }
}
