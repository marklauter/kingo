using System.Collections.Immutable;

namespace Kingo.Policies.Tests;

public sealed class PdlSerializerTests
{
    [Fact]
    public void Serialize_SimpleDocument_ReturnsValidYaml()
    {
        var document = new PdlDocument(
            "original",
            [
                new Namespace(
                    NamespaceIdentifier.From("file"),
                    [new Relation(RelationIdentifier.From("owner"), ThisRewrite.Default)]),
            ]);

        var yaml = PdlSerializer.Serialize(document);

        Assert.Contains("file:", yaml, StringComparison.Ordinal);
        Assert.Contains("- owner", yaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_ComplexDocument_ReturnsValidYaml()
    {
        var document = new PdlDocument(
            "original",
            [
                new Namespace(
                    NamespaceIdentifier.From("file"),
                    [
                        new Relation(RelationIdentifier.From("owner"), ThisRewrite.Default),
                        new Relation(
                            RelationIdentifier.From("editor"),
                            new UnionRewrite(
                            [
                                ThisRewrite.Default,
                                new ComputedSubjectSetRewrite(RelationIdentifier.From("owner")),
                            ])),
                        new Relation(
                            RelationIdentifier.From("viewer"),
                            new ExclusionRewrite(
                                new UnionRewrite(
                                [
                                    ThisRewrite.Default,
                                    new ComputedSubjectSetRewrite(RelationIdentifier.From("editor")),
                                    new TupleToSubjectSetRewrite(RelationIdentifier.From("parent"), RelationIdentifier.From("viewer")),
                                ]),
                                new ComputedSubjectSetRewrite(RelationIdentifier.From("banned")))),
                        new Relation(
                            RelationIdentifier.From("auditor"),
                            new IntersectionRewrite(
                            [
                                ThisRewrite.Default,
                                new ComputedSubjectSetRewrite(RelationIdentifier.From("viewer")),
                            ])),
                    ]),
            ]);

        var yaml = PdlSerializer.Serialize(document);

        Assert.Contains("file:", yaml, StringComparison.Ordinal);
        Assert.Contains("- owner", yaml, StringComparison.Ordinal);
        Assert.Contains("editor: this | owner", yaml, StringComparison.Ordinal);
        Assert.Contains("auditor: this & viewer", yaml, StringComparison.Ordinal);
        Assert.Contains("! banned", yaml, StringComparison.Ordinal);
        Assert.Contains("(parent, viewer)", yaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_MultipleNamespaces_ReturnsValidYaml()
    {
        var document = new PdlDocument(
            "original",
            [
                new Namespace(
                    NamespaceIdentifier.From("file"),
                    [new Relation(RelationIdentifier.From("owner"), ThisRewrite.Default)]),
                new Namespace(
                    NamespaceIdentifier.From("folder"),
                    [new Relation(RelationIdentifier.From("viewer"), ThisRewrite.Default)]),
            ]);

        var yaml = PdlSerializer.Serialize(document);

        Assert.Contains("file:", yaml, StringComparison.Ordinal);
        Assert.Contains("folder:", yaml, StringComparison.Ordinal);
        Assert.Contains("- owner", yaml, StringComparison.Ordinal);
        Assert.Contains("- viewer", yaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_EmptyDocument_ReturnsEmptyYaml()
    {
        var document = new PdlDocument("original", []);

        var yaml = PdlSerializer.Serialize(document);

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

        var originalDoc = PdlParser.Parse(originalYaml);
        var serializedYaml = PdlSerializer.Serialize(originalDoc);
        var reparsedDoc = PdlParser.Parse(serializedYaml);

        Assert.Equal(originalDoc.Namespaces, reparsedDoc.Namespaces);
    }

    [Fact]
    public void RoundTrip_ComplexDocument_PreservesSemantics()
    {
        var yaml = File.ReadAllText("Data/doc.policy.yml");

        var originalDoc = PdlParser.Parse(yaml);
        var serializedYaml = PdlSerializer.Serialize(originalDoc);
        var reparsedDoc = PdlParser.Parse(serializedYaml);

        Assert.Equal(originalDoc.Namespaces, reparsedDoc.Namespaces);
    }

    [Fact]
    public void Serialize_AllRewriteTypes_ReturnsCorrectExpressions()
    {
        var document = new PdlDocument(
            "test",
            [
                new Namespace(
                    NamespaceIdentifier.From("test"),
                    [
                        new Relation(RelationIdentifier.From("direct"), ThisRewrite.Default),
                        new Relation(
                            RelationIdentifier.From("computed"),
                            new ComputedSubjectSetRewrite(RelationIdentifier.From("owner"))),
                        new Relation(
                            RelationIdentifier.From("tuple"),
                            new TupleToSubjectSetRewrite(RelationIdentifier.From("parent"), RelationIdentifier.From("viewer"))),
                        new Relation(
                            RelationIdentifier.From("union"),
                            new UnionRewrite(
                            [
                                ThisRewrite.Default,
                                new ComputedSubjectSetRewrite(RelationIdentifier.From("owner")),
                            ])),
                        new Relation(
                            RelationIdentifier.From("intersection"),
                            new IntersectionRewrite(
                            [
                                ThisRewrite.Default,
                                new ComputedSubjectSetRewrite(RelationIdentifier.From("viewer")),
                            ])),
                        new Relation(
                            RelationIdentifier.From("exclusion"),
                            new ExclusionRewrite(
                                ThisRewrite.Default,
                                new ComputedSubjectSetRewrite(RelationIdentifier.From("banned")))),
                    ]),
            ]);

        var yaml = PdlSerializer.Serialize(document);

        Assert.Contains("- direct", yaml, StringComparison.Ordinal);
        Assert.Contains("computed: owner", yaml, StringComparison.Ordinal);
        Assert.Contains("tuple: (parent, viewer)", yaml, StringComparison.Ordinal);
        Assert.Contains("union: this | owner", yaml, StringComparison.Ordinal);
        Assert.Contains("intersection: this & viewer", yaml, StringComparison.Ordinal);
        Assert.Contains("exclusion: this ! banned", yaml, StringComparison.Ordinal);
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
        var originalDoc = PdlParser.Parse(originalYaml);
        var serializedYaml = PdlSerializer.Serialize(originalDoc);
        var reparsedDoc = PdlParser.Parse(serializedYaml);

        Assert.Equal(originalDoc.Namespaces, reparsedDoc.Namespaces);
    }

    [Fact]
    public async Task Serialize_ConcurrentCalls_HandlesCorrectly()
    {
        var document = new PdlDocument(
            "test",
            [
                new Namespace(
                    NamespaceIdentifier.From("file"),
                    [new Relation(RelationIdentifier.From("owner"), ThisRewrite.Default)]),
            ]);

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => PdlSerializer.Serialize(document)))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, Assert.NotNull);

        var first = results[0];
        Assert.All(results, yaml => Assert.Equal(first, yaml));
    }

    [Fact]
    public void Serialize_LargeDocument_HandlesCorrectly()
    {
        var namespaces = Enumerable.Range(0, 50)
            .Select(i => new Namespace(
                NamespaceIdentifier.From($"namespace{i}"),
                [
                    new Relation(RelationIdentifier.From("owner"), ThisRewrite.Default),
                    new Relation(
                        RelationIdentifier.From("editor"),
                        new UnionRewrite(
                        [
                            ThisRewrite.Default,
                            new ComputedSubjectSetRewrite(RelationIdentifier.From("owner")),
                        ])),
                ]))
            .ToImmutableArray();

        var document = new PdlDocument("large", namespaces);

        var yaml = PdlSerializer.Serialize(document);

        for (var i = 0; i < 50; i++)
            Assert.Contains($"namespace{i}:", yaml, StringComparison.Ordinal);
    }
}
