using Kingo.Policies;
using System.Collections.Immutable;
using static Kingo.Serialization.Pdl.Tests.TestHelpers;

namespace Kingo.Serialization.Pdl.Tests;

public sealed class PdlSerializeTests
{
    [Fact]
    public void Serialize_SimpleDocument_EmitsCanonicalPdl()
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

        var pdl = PdlSerializer.Serialize(namespaces);

        Assert.Equal("file:\n- owner\n- editor: this | owner\n", pdl);
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

        var pdl = PdlSerializer.Serialize(namespaces);

        Assert.Contains("- direct", pdl, StringComparison.Ordinal);
        Assert.Contains("computed: owner", pdl, StringComparison.Ordinal);
        Assert.Contains("tuple: (parent, viewer)", pdl, StringComparison.Ordinal);
        Assert.Contains("union: this | owner", pdl, StringComparison.Ordinal);
        Assert.Contains("intersection: this & viewer", pdl, StringComparison.Ordinal);
        Assert.Contains("exclusion: this ! banned", pdl, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_MultipleNamespaces_EmitsAllInOrder()
    {
        ImmutableArray<Namespace> namespaces =
        [
            MakeNs(Ns("file"), [Bare("owner")]),
            MakeNs(Ns("folder"), [Bare("viewer")]),
        ];

        var pdl = PdlSerializer.Serialize(namespaces);

        Assert.Equal("file:\n- owner\nfolder:\n- viewer\n", pdl);
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

        var pdl = PdlSerializer.Serialize(namespaces);

        Assert.DoesNotContain("\r", pdl, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_NoNamespaces_EmitsEmptyMapping()
    {
        var pdl = PdlSerializer.Serialize([]);

        Assert.Equal("{}", pdl.TrimEnd('\n'));
    }

    [Fact]
    public void Serialize_NamespaceWithoutRelationships_EmitsEmptySequence()
    {
        var pdl = PdlSerializer.Serialize([MakeNs(Ns("file"), [])]);

        Assert.Equal("file: []\n", pdl);
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

        _ = Assert.Throws<ArgumentException>(() => PdlSerializer.Serialize(namespaces));
    }

    [Theory]
    [InlineData("this")]
    [InlineData("...")]
    public void Serialize_ReservedRelationshipName_IsCallerDefect(string name)
    {
        // PDL cannot express a relationship named by a rewrite-grammar reserved word: 'this' could
        // never be referenced (a reference lexes as the keyword) and '...' cannot lex at all.
        ImmutableArray<Namespace> namespaces = [MakeNs(Ns("file"), [Bare(name)])];

        _ = Assert.Throws<ArgumentException>(() => PdlSerializer.Serialize(namespaces));
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

        _ = Assert.Throws<ArgumentException>(() => PdlSerializer.Serialize(namespaces));
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

        _ = Assert.Throws<ArgumentException>(() => PdlSerializer.Serialize(namespaces));
    }
}
