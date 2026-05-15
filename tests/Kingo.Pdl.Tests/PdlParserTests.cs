using System.Collections.Immutable;

namespace Kingo.Pdl.Tests;

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

        ImmutableArray<Namespace> expected =
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
                        ]))
                ])
        ];

        var doc = PdlParser.Parse(yaml);
        Assert.Equal(expected, doc.Namespaces);
    }

    [Fact]
    public void Parse_ComplexYamlPolicy_ReturnsCorrectAst()
    {
        var yaml = File.ReadAllText("Data/doc.policy.yml");

        var filePolicy = new Namespace(
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
                new Relation(RelationIdentifier.From("banned"), ThisRewrite.Default),
            ]);

        var folderPolicy = new Namespace(
            NamespaceIdentifier.From("folder"),
            [
                new Relation(RelationIdentifier.From("owner"), ThisRewrite.Default),
                new Relation(
                    RelationIdentifier.From("viewer"),
                    new ExclusionRewrite(
                        new UnionRewrite(
                        [
                            ThisRewrite.Default,
                            new TupleToSubjectSetRewrite(RelationIdentifier.From("parent"), RelationIdentifier.From("viewer")),
                        ]),
                        new ComputedSubjectSetRewrite(RelationIdentifier.From("banned")))),
                new Relation(RelationIdentifier.From("banned"), ThisRewrite.Default),
            ]);

        ImmutableArray<Namespace> expected = [filePolicy, folderPolicy];

        var doc = PdlParser.Parse(yaml);
        Assert.Equal(expected, doc.Namespaces);
        Assert.Equal(yaml, doc.Yaml);
    }

    [Theory]
    [InlineData("file:\n  - owner")]
    [InlineData("file:\n  - owner\n  - editor: this")]
    [InlineData("file:\n  - viewer: this | owner")]
    [InlineData("file:\n  - viewer: this & owner")]
    [InlineData("file:\n  - viewer: this ! owner")]
    public void Parse_ValidYamlFormats_Succeeds(string yaml) =>
        _ = PdlParser.Parse(yaml);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("null")]
    [InlineData("invalid: yaml: content")]
    [InlineData("file:\n  - owner: invalid expression syntax")]
    [InlineData("file:\n  - viewer: this |")]
    [InlineData("file:\n  - viewer: | this")]
    [InlineData("file:\n  - viewer: this & & owner")]
    [InlineData("file name:\n  - owner")]
    [InlineData("file-name:\n  - owner")]
    [InlineData("123file:\n  - owner")]
    [InlineData("file.ext:\n  - owner")]
    [InlineData("file:\n  - viewer: invalid-identifier")]
    [InlineData("file:\n  - viewer: (incomplete tuple")]
    [InlineData("file:\n  - owner-name")]
    [InlineData("file:\n  - 123owner")]
    [InlineData("file:\n  - owner.ext")]
    [InlineData("file:\n  - viewer: 123invalid")]
    [InlineData("file:\n  - viewer: invalid-name")]
    [InlineData("file:\n  - viewer: (parent, child, extra)")]
    [InlineData("[]")]
    [InlineData("scalar")]
    public void Parse_InvalidYamlFormats_Throws(string yaml) =>
        Assert.Throws<PdlParseException>(() => PdlParser.Parse(yaml));

    [Fact]
    public void Parse_Empty_ReturnsEmpty()
    {
        var doc = PdlParser.Parse("{}");
        Assert.Empty(doc.Namespaces);
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
        // Should either succeed or throw PdlParseException — nothing else.
        try
        {
            _ = PdlParser.Parse(yaml);
        }
        catch (PdlParseException)
        {
        }
    }

    [Fact]
    public void Parse_NullInput_Throws() =>
        Assert.Throws<ArgumentNullException>(() => PdlParser.Parse(null!));

    [Theory]
    [InlineData("file:\n  - viewer: (this)")]
    [InlineData("file:\n  - viewer: this # comment")]
    [InlineData("file:\n  - viewer: (parent, child)")]
    [InlineData("file:\n  - viewer: this | (parent, child) & owner ! banned")]
    public void Parse_ComplexExpressionFormats_Succeeds(string yaml) =>
        _ = PdlParser.Parse(yaml);

    [Fact]
    public async Task Parse_ConcurrentCalls_HandlesCorrectly()
    {
        const string yaml = "file:\n  - owner\n  - editor: this | owner";

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => PdlParser.Parse(yaml)))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, Assert.NotNull);
    }

    [Fact]
    public void Parse_LargeDocument_HandlesCorrectly()
    {
        var namespaces = Enumerable.Range(0, 50)
            .Select(i => $"namespace{i}:\n  - owner\n  - editor: this | owner");
        var yaml = string.Join("\n", namespaces);

        var doc = PdlParser.Parse(yaml);
        Assert.Equal(50, doc.Namespaces.Length);
    }
}
