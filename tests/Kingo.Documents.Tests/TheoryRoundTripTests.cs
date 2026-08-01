using Kingo.Theories;
using static Kingo.Documents.Tests.TestHelpers;

namespace Kingo.Documents.Tests;

public sealed class TheoryRoundTripTests
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
    [InlineData("file:\n  - null\n  - viewer: null")]
    public void RoundTrip_FromText_PreservesTheoryValues(string namespaceMap)
    {
        var original = ParseSuccess(Document(namespaceMap));
        var roundTripped = ParseSuccess(original.Print());

        Assert.Equal(original, roundTripped);
    }

    private static readonly IReadOnlyDictionary<string, SubjectSetRewrite> RewriteCases = new Dictionary<string, SubjectSetRewrite>
    {
        ["this"] = SubjectSetRewrite.This.Default,
        ["computed"] = Computed("owner"),
        ["computed null"] = Computed("null"),
        ["fact-to-subjectset"] = FactTo("parent", "viewer"),
        ["flat union"] = Union([SubjectSetRewrite.This.Default, Computed("owner")]),
        ["flat intersection"] = Intersection([Computed("a"), Computed("b"), Computed("c")]),
        ["exclusion"] = Exclusion(SubjectSetRewrite.This.Default, Computed("banned")),
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
                SubjectSetRewrite.This.Default,
                Computed("editor"),
                FactTo("parent", "viewer"),
            ]),
            Computed("banned")),
    };

    public static TheoryData<string> RewriteCaseKeys => [.. RewriteCases.Keys];

    [Theory]
    [MemberData(nameof(RewriteCaseKeys))]
    public void RoundTrip_FromTheory_PreservesTreeStructure(string key)
    {
        var original = MakeTheory(
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
                    new Relation(Rel("viewer"), RewriteCases[key]),
                ]),
        ]);

        var roundTripped = ParseSuccess(original.Print());

        Assert.Equal(original, roundTripped);
    }

    [Fact]
    public void RoundTrip_ComplexDocument_PreservesTheoryValues()
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
    public void RoundTrip_TheoryName_SurvivesTheDocument()
    {
        var original = ParseSuccess(Document("file:\n  - owner", name: "acme"));

        var roundTripped = ParseSuccess(original.Print());

        Assert.Equal(TheoryId("acme"), roundTripped.Name);
        Assert.Equal(original, roundTripped);
    }
}
