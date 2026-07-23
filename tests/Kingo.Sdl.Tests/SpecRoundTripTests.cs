using Kingo.Schemas;
using static Kingo.Sdl.Tests.TestHelpers;

namespace Kingo.Sdl.Tests;

public sealed class SpecRoundTripTests
{
    [Theory]
    [InlineData("file:\n  - owner")]
    [InlineData("file:\n  - owner\n  - editor: this")]
    [InlineData("file:\n  - owner\n  - viewer: this | owner")]
    [InlineData("file:\n  - owner\n  - viewer: this & owner")]
    [InlineData("file:\n  - owner\n  - viewer: this ! owner")]
    [InlineData("file:\n  - parent\n  - viewer: (parent, child)")]
    [InlineData("file:\n  - editor\n  - parent\n  - banned\n  - viewer: (this | editor | (parent, viewer)) ! banned")]
    [InlineData("file:\n  - owner\n  - parent\n  - banned\n  - viewer: this | (parent, child) & owner ! banned")]
    [InlineData("file:\n  - owner\nfolder:\n  - parent\n  - banned\n  - viewer: (this | (parent, viewer)) ! banned")]
    [InlineData("file:")]
    // 'null' is a legal relationship name; the renderer emits it as unquoted plain text and the parser reads
    // raw scalar text, so the pair stays inverse even where YAML's own typing would read a null
    [InlineData("file:\n  - null\n  - viewer: null")]
    public void RoundTrip_FromText_PreservesDomainValues(string namespaceMap)
    {
        var original = ParseSuccess(Document(namespaceMap));
        var roundTripped = ParseSuccess(original.Print());

        Assert.Equal(original, roundTripped);
    }

    // keyed by name so the theory rows stay xunit-serializable and enumerate individually
    private static readonly IReadOnlyDictionary<string, SubjectSetRewrite> RewriteCases = new Dictionary<string, SubjectSetRewrite>
    {
        ["this"] = ThisRewrite.Default,
        ["computed"] = Computed("owner"),
        ["computed null"] = Computed("null"), // rendered unquoted; survives because the parser treats scalar text as expression source
        ["fact-to-subjectset"] = FactTo("parent", "viewer"),
        ["flat union"] = Union([ThisRewrite.Default, Computed("owner")]),
        ["flat intersection"] = Intersection([Computed("a"), Computed("b"), Computed("c")]),
        ["exclusion"] = Exclusion(ThisRewrite.Default, Computed("banned")),
        // nested compounds exercise the renderer's parenthesization: each shape below is
        // structurally distinct from its flattened or re-associated reading
        ["intersection in union"] = Union([Intersection([Computed("a"), Computed("b")]), Computed("c")]),
        ["union in intersection"] = Intersection([Union([Computed("a"), Computed("b")]), Computed("c")]),
        ["right-nested union"] = Union([Computed("a"), Union([Computed("b"), Computed("c")])]),
        ["left-nested union"] = Union([Union([Computed("a"), Computed("b")]), Computed("c")]),
        ["left-nested intersection"] = Intersection([Intersection([Computed("a"), Computed("b")]), Computed("c")]),
        ["exclusion in union"] = Union([Exclusion(Computed("a"), Computed("b")), Computed("c")]),
        ["union include side"] = Exclusion(Union([Computed("a"), Computed("b")]), Computed("c")),
        ["union exclude side"] = Exclusion(Computed("a"), Union([Computed("b"), Computed("c")])),
        ["left-chained exclusion"] = Exclusion(Exclusion(Computed("a"), Computed("b")), Computed("c")),
        ["right-nested exclusion"] = Exclusion(Computed("a"), Exclusion(Computed("b"), Computed("c"))),
        ["kitchen sink"] = Exclusion(
            Union(
            [
                ThisRewrite.Default,
                Computed("editor"),
                FactTo("parent", "viewer"),
            ]),
            Computed("banned")),
    };

    public static TheoryData<string> RewriteCaseKeys => [.. RewriteCases.Keys];

    [Theory]
    [MemberData(nameof(RewriteCaseKeys))]
    public void RoundTrip_FromDomain_PreservesTreeStructure(string key)
    {
        // every relationship the rewrite cases reference, defined bare so the namespace gate passes
        var original = MakeSpec(
        [
            MakeNs(
                Ns("file"),
                [
                    Bare("owner"),
                    Bare("parent"),
                    Bare("editor"),
                    Bare("banned"),
                    Bare("null"),
                    Bare("a"),
                    Bare("b"),
                    Bare("c"),
                    new Relationship(Rel("viewer"), RewriteCases[key]),
                ]),
        ]);

        var roundTripped = ParseSuccess(original.Print());

        Assert.Equal(original, roundTripped);
    }

    [Fact]
    public void RoundTrip_ComplexDocument_PreservesDomainValues()
    {
        const string namespaceMap = """
            file:
              - owner
              - parent
              - editor: this | owner
              - viewer: >
                  (this | editor | (parent, viewer)) ! banned
              - auditor: this & viewer
              - banned

            folder:
              - owner
              - parent
              - viewer: (this | (parent, viewer)) ! banned
              - banned
            """;

        var original = ParseSuccess(Document(namespaceMap));
        var roundTripped = ParseSuccess(original.Print());

        Assert.Equal(original, roundTripped);
    }

    [Fact]
    public void RoundTrip_SpecName_SurvivesTheDocument()
    {
        // the name is in the document, so parse ∘ print = id covers the spec's domain key too
        var original = ParseSuccess(Document("file:\n  - owner", name: "acme"));

        var roundTripped = ParseSuccess(original.Print());

        Assert.Equal(SpecId("acme"), roundTripped.Path);
        Assert.Equal(original, roundTripped);
    }
}
