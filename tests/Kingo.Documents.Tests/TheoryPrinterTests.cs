using Kingo.Theories;
using static Kingo.Documents.Tests.TestHelpers;

namespace Kingo.Documents.Tests;

public sealed class TheoryPrinterTests
{
    [Fact]
    public void Print_SimpleDocument_EmitsCanonical_Theory_Document()
    {
        var theory = MakeTheory(
        [
            MakeNs(
                Ns("file"),
                [
                    Bare("owner"),
                    new Relation(
                        Rel("editor"),
                        Union([SubjectSetRewrite.This.Default, Computed("owner")])),
                ]),
        ]);

        Assert.Equal("theory: test\nnamespaces:\n  file:\n  - owner\n  - editor: this | owner\n", theory.Print());
    }

    [Fact]
    public void Print_AllRewriteTypes_EmitsExpectedExpressions()
    {
        var theory = MakeTheory(
        [
            MakeNs(
                Ns("test"),
                [
                    Bare("owner"),
                    Bare("parent"),
                    Bare("viewer"),
                    Bare("banned"),
                    Bare("direct"),
                    new Relation(Rel("computed"), Computed("owner")),
                    new Relation(Rel("factset"), FactTo("parent", "viewer")),
                    new Relation(Rel("union"), Union([SubjectSetRewrite.This.Default, Computed("owner")])),
                    new Relation(Rel("intersection"), Intersection([SubjectSetRewrite.This.Default, Computed("viewer")])),
                    new Relation(Rel("exclusion"), Exclusion(SubjectSetRewrite.This.Default, Computed("banned"))),
                ]),
        ]);

        var document = theory.Print();

        Assert.Contains("- direct", document, StringComparison.Ordinal);
        Assert.Contains("computed: owner", document, StringComparison.Ordinal);
        Assert.Contains("factset: (parent, viewer)", document, StringComparison.Ordinal);
        Assert.Contains("union: this | owner", document, StringComparison.Ordinal);
        Assert.Contains("intersection: this & viewer", document, StringComparison.Ordinal);
        Assert.Contains("exclusion: this ! banned", document, StringComparison.Ordinal);
    }

    [Fact]
    public void Print_TheoryName_LeadsTheDocument()
    {
        var theory = MakeTheory(TheoryId("acme"), [MakeNs(Ns("file"), [Bare("owner")])]);

        Assert.StartsWith("theory: acme\nnamespaces:\n", theory.Print(), StringComparison.Ordinal);
    }

    [Fact]
    public void Print_MultipleNamespaces_EmitsAllInOrder()
    {
        var theory = MakeTheory(
        [
            MakeNs(Ns("file"), [Bare("owner")]),
            MakeNs(Ns("folder"), [Bare("viewer")]),
        ]);

        Assert.Equal("theory: test\nnamespaces:\n  file:\n  - owner\n  folder:\n  - viewer\n", theory.Print());
    }

    [Fact]
    public void Print_NewlineIsPinned_NoCarriageReturnOnAnyPlatform()
    {
        var theory = MakeTheory(
        [
            MakeNs(
                Ns("file"),
                [Bare("owner"), new Relation(Rel("editor"), SubjectSetRewrite.This.Default)]),
        ]);

        Assert.DoesNotContain("\r", theory.Print(), StringComparison.Ordinal);
    }

    [Fact]
    public void Print_NamespaceWithoutRelations_EmitsEmptySequence()
    {
        var theory = MakeTheory([MakeNs(Ns("file"), [])]);

        Assert.Equal("theory: test\nnamespaces:\n  file: []\n", theory.Print());
    }

    [Theory]
    [InlineData("this")]
    [InlineData("THIS")]
    public void Print_ReservedRelationName_IsCallerDefect(string name)
    {
        var theory = MakeTheory([MakeNs(Ns("file"), [Bare(name)])]);

        _ = Assert.Throws<ArgumentException>(theory.Print);
    }

    [Fact]
    public void Print_ReservedReferenceInRewrite_IsCallerDefect()
    {
        var theory = MakeTheory(
        [
            MakeNs(Ns("file"), [Bare("this"), new Relation(Rel("viewer"), Computed("this"))]),
        ]);

        _ = Assert.Throws<ArgumentException>(theory.Print);
    }

    [Fact]
    public void Print_ReservedReferenceInFactset_IsCallerDefect()
    {
        var theory = MakeTheory(
        [
            MakeNs(
                Ns("file"),
                [Bare("this"), new Relation(Rel("viewer"), FactTo("this", "member"))]),
        ]);

        _ = Assert.Throws<ArgumentException>(theory.Print);
    }
}
