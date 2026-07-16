using Kingo.Schemas;
using static Kingo.Sdl.Tests.TestHelpers;

namespace Kingo.Sdl.Tests;

public sealed class SchemaPrinterTests
{
    [Fact]
    public void Print_SimpleDocument_EmitsCanonicalSdl()
    {
        var schema = MakeSchema(
        [
            MakeNs(
                Ns("file"),
                [
                    Bare("owner"),
                    new Relationship(
                        Rel("editor"),
                        new UnionRewrite([ThisRewrite.Default, Computed("owner")])),
                ]),
        ]);

        Assert.Equal("schema: test\nnamespaces:\n  file:\n  - owner\n  - editor: this | owner\n", schema.Print());
    }

    [Fact]
    public void Print_AllRewriteTypes_EmitsExpectedExpressions()
    {
        var schema = MakeSchema(
        [
            MakeNs(
                Ns("test"),
                [
                    Bare("direct"),
                    new Relationship(Rel("computed"), Computed("owner")),
                    new Relationship(Rel("tuple"), new TupleToSubjectSetRewrite(Rel("parent"), Rel("viewer"))),
                    new Relationship(Rel("union"), new UnionRewrite([ThisRewrite.Default, Computed("owner")])),
                    new Relationship(Rel("intersection"), new IntersectionRewrite([ThisRewrite.Default, Computed("viewer")])),
                    new Relationship(Rel("exclusion"), new ExclusionRewrite(ThisRewrite.Default, Computed("banned"))),
                ]),
        ]);

        var sdl = schema.Print();

        Assert.Contains("- direct", sdl, StringComparison.Ordinal);
        Assert.Contains("computed: owner", sdl, StringComparison.Ordinal);
        Assert.Contains("tuple: (parent, viewer)", sdl, StringComparison.Ordinal);
        Assert.Contains("union: this | owner", sdl, StringComparison.Ordinal);
        Assert.Contains("intersection: this & viewer", sdl, StringComparison.Ordinal);
        Assert.Contains("exclusion: this ! banned", sdl, StringComparison.Ordinal);
    }

    [Fact]
    public void Print_SchemaName_LeadsTheDocument()
    {
        var schema = MakeSchema(SchemaId("acme"), [MakeNs(Ns("file"), [Bare("owner")])]);

        Assert.StartsWith("schema: acme\nnamespaces:\n", schema.Print(), StringComparison.Ordinal);
    }

    [Fact]
    public void Print_MultipleNamespaces_EmitsAllInOrder()
    {
        var schema = MakeSchema(
        [
            MakeNs(Ns("file"), [Bare("owner")]),
            MakeNs(Ns("folder"), [Bare("viewer")]),
        ]);

        Assert.Equal("schema: test\nnamespaces:\n  file:\n  - owner\n  folder:\n  - viewer\n", schema.Print());
    }

    [Fact]
    public void Print_NewlineIsPinned_NoCarriageReturnOnAnyPlatform()
    {
        var schema = MakeSchema(
        [
            MakeNs(
                Ns("file"),
                [Bare("owner"), new Relationship(Rel("editor"), ThisRewrite.Default)]),
        ]);

        Assert.DoesNotContain("\r", schema.Print(), StringComparison.Ordinal);
    }

    [Fact]
    public void Print_NamespaceWithoutRelationships_EmitsEmptySequence()
    {
        var schema = MakeSchema([MakeNs(Ns("file"), [])]);

        Assert.Equal("schema: test\nnamespaces:\n  file: []\n", schema.Print());
    }

    [Theory]
    [InlineData("this")]
    [InlineData("THIS")] // Create performs no normalization, but the reserved-word check is case-insensitive like the tokenizer
    [InlineData("...")]
    public void Print_ReservedRelationshipName_IsCallerDefect(string name)
    {
        // SDL cannot express a relationship named by a rewrite-grammar reserved word: 'this' could
        // never be referenced (a reference lexes as the keyword) and '...' cannot lex at all.
        var schema = MakeSchema([MakeNs(Ns("file"), [Bare(name)])]);

        _ = Assert.Throws<ArgumentException>(schema.Print);
    }

    [Theory]
    [InlineData("this")]
    [InlineData("...")]
    public void Print_ReservedReferenceInRewrite_IsCallerDefect(string name)
    {
        // a computed reference to 'this' would silently reparse as ThisRewrite — direct membership
        // instead of a relationship reference — so emitting it is corruption, not serialization
        var schema = MakeSchema(
        [
            MakeNs(Ns("file"), [new Relationship(Rel("viewer"), Computed(name))]),
        ]);

        _ = Assert.Throws<ArgumentException>(schema.Print);
    }

    [Fact]
    public void Print_ReservedReferenceInTupleset_IsCallerDefect()
    {
        var schema = MakeSchema(
        [
            MakeNs(
                Ns("file"),
                [new Relationship(Rel("viewer"), new TupleToSubjectSetRewrite(Rel("parent"), Rel("...")))]),
        ]);

        _ = Assert.Throws<ArgumentException>(schema.Print);
    }
}
