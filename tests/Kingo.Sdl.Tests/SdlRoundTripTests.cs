using Kingo.Schemas;
using static Kingo.Sdl.Tests.TestHelpers;

namespace Kingo.Sdl.Tests;

public sealed class SdlRoundTripTests
{
    [Theory]
    [InlineData("file:\n  - owner")]
    [InlineData("file:\n  - owner\n  - editor: this")]
    [InlineData("file:\n  - viewer: this | owner")]
    [InlineData("file:\n  - viewer: this & owner")]
    [InlineData("file:\n  - viewer: this ! owner")]
    [InlineData("file:\n  - viewer: (parent, child)")]
    [InlineData("file:\n  - viewer: (this | editor | (parent, viewer)) ! banned")]
    [InlineData("file:\n  - viewer: this | (parent, child) & owner ! banned")]
    [InlineData("file:\n  - owner\nfolder:\n  - viewer: (this | (parent, viewer)) ! banned")]
    [InlineData("file:")]
    // 'null' is a legal relationship name; the renderer emits it as unquoted plain text and the parser reads
    // raw scalar text, so the pair stays inverse even where YAML's own typing would read a null
    [InlineData("file:\n  - null\n  - viewer: null")]
    public void RoundTrip_FromText_PreservesDomainValues(string sdl)
    {
        var original = ParseSuccess(sdl);
        var roundTripped = ParseSuccess(original.Print());

        Assert.Equal(original, roundTripped);
    }

    // keyed by name so the theory rows stay xunit-serializable and enumerate individually
    private static readonly IReadOnlyDictionary<string, SubjectSetRewrite> RewriteCases = new Dictionary<string, SubjectSetRewrite>
    {
        ["this"] = ThisRewrite.Default,
        ["computed"] = Computed("owner"),
        ["computed null"] = Computed("null"), // rendered unquoted; survives because the parser treats scalar text as expression source
        ["tuple-to-subjectset"] = new TupleToSubjectSetRewrite(Rel("parent"), Rel("viewer")),
        ["flat union"] = new UnionRewrite([ThisRewrite.Default, Computed("owner")]),
        ["flat intersection"] = new IntersectionRewrite([Computed("a"), Computed("b"), Computed("c")]),
        ["exclusion"] = new ExclusionRewrite(ThisRewrite.Default, Computed("banned")),
        // nested compounds exercise the renderer's parenthesization: each shape below is
        // structurally distinct from its flattened or re-associated reading
        ["intersection in union"] = new UnionRewrite([new IntersectionRewrite([Computed("a"), Computed("b")]), Computed("c")]),
        ["union in intersection"] = new IntersectionRewrite([new UnionRewrite([Computed("a"), Computed("b")]), Computed("c")]),
        ["right-nested union"] = new UnionRewrite([Computed("a"), new UnionRewrite([Computed("b"), Computed("c")])]),
        ["left-nested union"] = new UnionRewrite([new UnionRewrite([Computed("a"), Computed("b")]), Computed("c")]),
        ["left-nested intersection"] = new IntersectionRewrite([new IntersectionRewrite([Computed("a"), Computed("b")]), Computed("c")]),
        ["exclusion in union"] = new UnionRewrite([new ExclusionRewrite(Computed("a"), Computed("b")), Computed("c")]),
        ["union include side"] = new ExclusionRewrite(new UnionRewrite([Computed("a"), Computed("b")]), Computed("c")),
        ["union exclude side"] = new ExclusionRewrite(Computed("a"), new UnionRewrite([Computed("b"), Computed("c")])),
        ["left-chained exclusion"] = new ExclusionRewrite(new ExclusionRewrite(Computed("a"), Computed("b")), Computed("c")),
        ["right-nested exclusion"] = new ExclusionRewrite(Computed("a"), new ExclusionRewrite(Computed("b"), Computed("c"))),
        ["kitchen sink"] = new ExclusionRewrite(
            new UnionRewrite(
            [
                ThisRewrite.Default,
                Computed("editor"),
                new TupleToSubjectSetRewrite(Rel("parent"), Rel("viewer")),
            ]),
            Computed("banned")),
    };

    public static TheoryData<string> RewriteCaseKeys => [.. RewriteCases.Keys];

    [Theory]
    [MemberData(nameof(RewriteCaseKeys))]
    public void RoundTrip_FromDomain_PreservesTreeStructure(string key)
    {
        var original = MakeSchema(
        [
            MakeNs(Ns("file"), [new Relationship(Rel("viewer"), RewriteCases[key])]),
        ]);

        var roundTripped = ParseSuccess(original.Print());

        Assert.Equal(original, roundTripped);
    }

    [Fact]
    public void RoundTrip_ComplexDocument_PreservesDomainValues()
    {
        const string sdl = """
            file:
              - owner
              - editor: this | owner
              - viewer: >
                  (this | editor | (parent, viewer)) ! banned
              - auditor: this & viewer
              - banned

            folder:
              - owner
              - viewer: (this | (parent, viewer)) ! banned
              - banned
            """;

        var original = ParseSuccess(sdl);
        var roundTripped = ParseSuccess(original.Print());

        Assert.Equal(original, roundTripped);
    }
}
