using Kingo.Schemas;
using System.Collections.Immutable;
using static Kingo.Serialization.Sdl.Tests.TestHelpers;

namespace Kingo.Serialization.Sdl.Tests;

public sealed class SdlSerializeTests
{
    [Fact]
    public void Serialize_SimpleDocument_EmitsCanonicalSdl()
    {
        ImmutableArray<Namespace> namespaces =
        [
            MakeNs(
                Ns("file"),
                [
                    Bare("owner"),
                    new Relationship(
                        Rel("editor"),
                        new UnionRewrite([ThisRewrite.Default, Computed("owner")])),
                ]),
        ];

        var sdl = SdlSerializer.Serialize(namespaces);

        Assert.Equal("file:\n- owner\n- editor: this | owner\n", sdl);
    }

    [Fact]
    public void Serialize_AllRewriteTypes_EmitsExpectedExpressions()
    {
        ImmutableArray<Namespace> namespaces =
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
        ];

        var sdl = SdlSerializer.Serialize(namespaces);

        Assert.Contains("- direct", sdl, StringComparison.Ordinal);
        Assert.Contains("computed: owner", sdl, StringComparison.Ordinal);
        Assert.Contains("tuple: (parent, viewer)", sdl, StringComparison.Ordinal);
        Assert.Contains("union: this | owner", sdl, StringComparison.Ordinal);
        Assert.Contains("intersection: this & viewer", sdl, StringComparison.Ordinal);
        Assert.Contains("exclusion: this ! banned", sdl, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_MultipleNamespaces_EmitsAllInOrder()
    {
        ImmutableArray<Namespace> namespaces =
        [
            MakeNs(Ns("file"), [Bare("owner")]),
            MakeNs(Ns("folder"), [Bare("viewer")]),
        ];

        var sdl = SdlSerializer.Serialize(namespaces);

        Assert.Equal("file:\n- owner\nfolder:\n- viewer\n", sdl);
    }

    [Fact]
    public void Serialize_NewlineIsPinned_NoCarriageReturnOnAnyPlatform()
    {
        ImmutableArray<Namespace> namespaces =
        [
            MakeNs(
                Ns("file"),
                [Bare("owner"), new Relationship(Rel("editor"), ThisRewrite.Default)]),
        ];

        var sdl = SdlSerializer.Serialize(namespaces);

        Assert.DoesNotContain("\r", sdl, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_NoNamespaces_EmitsEmptyMapping()
    {
        var sdl = SdlSerializer.Serialize([]);

        Assert.Equal("{}", sdl.TrimEnd('\n'));
    }

    [Fact]
    public void Serialize_NamespaceWithoutRelationships_EmitsEmptySequence()
    {
        var sdl = SdlSerializer.Serialize([MakeNs(Ns("file"), [])]);

        Assert.Equal("file: []\n", sdl);
    }

    [Fact]
    public void Serialize_DuplicateNamespaceNames_IsCallerDefect()
    {
        // A YAML mapping cannot express duplicate keys; the document invariant lives outside the
        // domain model, so violating it is misuse, not a modeled outcome.
        ImmutableArray<Namespace> namespaces =
        [
            MakeNs(Ns("file"), [Bare("owner")]),
            MakeNs(Ns("file"), [Bare("viewer")]),
        ];

        _ = Assert.Throws<ArgumentException>(() => SdlSerializer.Serialize(namespaces));
    }

    [Theory]
    [InlineData("this")]
    [InlineData("...")]
    public void Serialize_ReservedRelationshipName_IsCallerDefect(string name)
    {
        // SDL cannot express a relationship named by a rewrite-grammar reserved word: 'this' could
        // never be referenced (a reference lexes as the keyword) and '...' cannot lex at all.
        ImmutableArray<Namespace> namespaces = [MakeNs(Ns("file"), [Bare(name)])];

        _ = Assert.Throws<ArgumentException>(() => SdlSerializer.Serialize(namespaces));
    }

    [Theory]
    [InlineData("this")]
    [InlineData("...")]
    public void Serialize_ReservedReferenceInRewrite_IsCallerDefect(string name)
    {
        // a computed reference to 'this' would silently reparse as ThisRewrite — direct membership
        // instead of a relationship reference — so emitting it is corruption, not serialization
        ImmutableArray<Namespace> namespaces =
        [
            MakeNs(Ns("file"), [new Relationship(Rel("viewer"), Computed(name))]),
        ];

        _ = Assert.Throws<ArgumentException>(() => SdlSerializer.Serialize(namespaces));
    }

    [Fact]
    public void Serialize_ReservedReferenceInTupleset_IsCallerDefect()
    {
        ImmutableArray<Namespace> namespaces =
        [
            MakeNs(
                Ns("file"),
                [new Relationship(Rel("viewer"), new TupleToSubjectSetRewrite(Rel("parent"), Rel("...")))]),
        ];

        _ = Assert.Throws<ArgumentException>(() => SdlSerializer.Serialize(namespaces));
    }
}
