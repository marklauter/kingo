using Kingo.Schemas;
using static Kingo.Sdl.Tests.TestHelpers;

namespace Kingo.Sdl.Tests;

public sealed class SchemaSdlExtensionsTests
{
    [Fact]
    public void ToSdl_SimpleDocument_EmitsCanonicalSdl()
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

        Assert.Equal("file:\n- owner\n- editor: this | owner\n", schema.ToSdl());
    }

    [Fact]
    public void ToSdl_AllRewriteTypes_EmitsExpectedExpressions()
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

        var sdl = schema.ToSdl();

        Assert.Contains("- direct", sdl, StringComparison.Ordinal);
        Assert.Contains("computed: owner", sdl, StringComparison.Ordinal);
        Assert.Contains("tuple: (parent, viewer)", sdl, StringComparison.Ordinal);
        Assert.Contains("union: this | owner", sdl, StringComparison.Ordinal);
        Assert.Contains("intersection: this & viewer", sdl, StringComparison.Ordinal);
        Assert.Contains("exclusion: this ! banned", sdl, StringComparison.Ordinal);
    }

    [Fact]
    public void ToSdl_MultipleNamespaces_EmitsAllInOrder()
    {
        var schema = MakeSchema(
        [
            MakeNs(Ns("file"), [Bare("owner")]),
            MakeNs(Ns("folder"), [Bare("viewer")]),
        ]);

        Assert.Equal("file:\n- owner\nfolder:\n- viewer\n", schema.ToSdl());
    }

    [Fact]
    public void ToSdl_NewlineIsPinned_NoCarriageReturnOnAnyPlatform()
    {
        var schema = MakeSchema(
        [
            MakeNs(
                Ns("file"),
                [Bare("owner"), new Relationship(Rel("editor"), ThisRewrite.Default)]),
        ]);

        Assert.DoesNotContain("\r", schema.ToSdl(), StringComparison.Ordinal);
    }

    [Fact]
    public void ToSdl_NamespaceWithoutRelationships_EmitsEmptySequence()
    {
        var schema = MakeSchema([MakeNs(Ns("file"), [])]);

        Assert.Equal("file: []\n", schema.ToSdl());
    }

    [Theory]
    [InlineData("this")]
    [InlineData("...")]
    public void ToSdl_ReservedRelationshipName_IsCallerDefect(string name)
    {
        // SDL cannot express a relationship named by a rewrite-grammar reserved word: 'this' could
        // never be referenced (a reference lexes as the keyword) and '...' cannot lex at all.
        var schema = MakeSchema([MakeNs(Ns("file"), [Bare(name)])]);

        _ = Assert.Throws<ArgumentException>(schema.ToSdl);
    }

    [Theory]
    [InlineData("this")]
    [InlineData("...")]
    public void ToSdl_ReservedReferenceInRewrite_IsCallerDefect(string name)
    {
        // a computed reference to 'this' would silently reparse as ThisRewrite — direct membership
        // instead of a relationship reference — so emitting it is corruption, not serialization
        var schema = MakeSchema(
        [
            MakeNs(Ns("file"), [new Relationship(Rel("viewer"), Computed(name))]),
        ]);

        _ = Assert.Throws<ArgumentException>(schema.ToSdl);
    }

    [Fact]
    public void ToSdl_ReservedReferenceInTupleset_IsCallerDefect()
    {
        var schema = MakeSchema(
        [
            MakeNs(
                Ns("file"),
                [new Relationship(Rel("viewer"), new TupleToSubjectSetRewrite(Rel("parent"), Rel("...")))]),
        ]);

        _ = Assert.Throws<ArgumentException>(schema.ToSdl);
    }
}
