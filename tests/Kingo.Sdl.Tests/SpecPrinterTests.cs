using Kingo.Schemas;
using static Kingo.Sdl.Tests.TestHelpers;

namespace Kingo.Sdl.Tests;

public sealed class SpecPrinterTests
{
    [Fact]
    public void Print_SimpleDocument_EmitsCanonicalSdl()
    {
        var spec = MakeSpec(
        [
            MakeNs(
                Ns("file"),
                [
                    Bare("owner"),
                    new Relationship(
                        Rel("editor"),
                        Union([ThisRewrite.Default, Computed("owner")])),
                ]),
        ]);

        Assert.Equal("schema: test\nnamespaces:\n  file:\n  - owner\n  - editor: this | owner\n", spec.Print());
    }

    [Fact]
    public void Print_AllRewriteTypes_EmitsExpectedExpressions()
    {
        var spec = MakeSpec(
        [
            MakeNs(
                Ns("test"),
                [
                    Bare("owner"),
                    Bare("parent"),
                    Bare("viewer"),
                    Bare("banned"),
                    Bare("direct"),
                    new Relationship(Rel("computed"), Computed("owner")),
                    new Relationship(Rel("factset"), FactTo("parent", "viewer")),
                    new Relationship(Rel("union"), Union([ThisRewrite.Default, Computed("owner")])),
                    new Relationship(Rel("intersection"), Intersection([ThisRewrite.Default, Computed("viewer")])),
                    new Relationship(Rel("exclusion"), Exclusion(ThisRewrite.Default, Computed("banned"))),
                ]),
        ]);

        var sdl = spec.Print();

        Assert.Contains("- direct", sdl, StringComparison.Ordinal);
        Assert.Contains("computed: owner", sdl, StringComparison.Ordinal);
        Assert.Contains("factset: (parent, viewer)", sdl, StringComparison.Ordinal);
        Assert.Contains("union: this | owner", sdl, StringComparison.Ordinal);
        Assert.Contains("intersection: this & viewer", sdl, StringComparison.Ordinal);
        Assert.Contains("exclusion: this ! banned", sdl, StringComparison.Ordinal);
    }

    [Fact]
    public void Print_SpecName_LeadsTheDocument()
    {
        var spec = MakeSpec(SpecId("acme"), [MakeNs(Ns("file"), [Bare("owner")])]);

        Assert.StartsWith("schema: acme\nnamespaces:\n", spec.Print(), StringComparison.Ordinal);
    }

    [Fact]
    public void Print_MultipleNamespaces_EmitsAllInOrder()
    {
        var spec = MakeSpec(
        [
            MakeNs(Ns("file"), [Bare("owner")]),
            MakeNs(Ns("folder"), [Bare("viewer")]),
        ]);

        Assert.Equal("schema: test\nnamespaces:\n  file:\n  - owner\n  folder:\n  - viewer\n", spec.Print());
    }

    [Fact]
    public void Print_NewlineIsPinned_NoCarriageReturnOnAnyPlatform()
    {
        var spec = MakeSpec(
        [
            MakeNs(
                Ns("file"),
                [Bare("owner"), new Relationship(Rel("editor"), ThisRewrite.Default)]),
        ]);

        Assert.DoesNotContain("\r", spec.Print(), StringComparison.Ordinal);
    }

    [Fact]
    public void Print_NamespaceWithoutRelationships_EmitsEmptySequence()
    {
        var spec = MakeSpec([MakeNs(Ns("file"), [])]);

        Assert.Equal("schema: test\nnamespaces:\n  file: []\n", spec.Print());
    }

    [Theory]
    [InlineData("this")]
    [InlineData("THIS")] // Unchecked performs no normalization, but the reserved-word check is case-insensitive like the tokenizer
    public void Print_ReservedRelationshipName_IsCallerDefect(string name)
    {
        // SDL cannot express a relationship named by the rewrite-grammar reserved word: 'this' could
        // never be referenced (a reference lexes as the keyword).
        var spec = MakeSpec([MakeNs(Ns("file"), [Bare(name)])]);

        _ = Assert.Throws<ArgumentException>(spec.Print);
    }

    [Fact]
    public void Print_ReservedReferenceInRewrite_IsCallerDefect()
    {
        // a computed reference to 'this' would silently reparse as ThisRewrite — direct membership
        // instead of a relationship reference — so emitting it is corruption, not serialization
        var spec = MakeSpec(
        [
            MakeNs(Ns("file"), [Bare("this"), new Relationship(Rel("viewer"), Computed("this"))]),
        ]);

        _ = Assert.Throws<ArgumentException>(spec.Print);
    }

    [Fact]
    public void Print_ReservedReferenceInFactset_IsCallerDefect()
    {
        // pins that the factset arm routes both components through the reserved-word gate
        var spec = MakeSpec(
        [
            MakeNs(
                Ns("file"),
                [Bare("this"), new Relationship(Rel("viewer"), FactTo("this", "member"))]),
        ]);

        _ = Assert.Throws<ArgumentException>(spec.Print);
    }
}
