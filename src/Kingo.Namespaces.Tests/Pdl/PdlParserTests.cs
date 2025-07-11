using Kingo.Policies.Pdl;
using static LanguageExt.Prelude;

namespace Kingo.Policies.Tests.Pdl;

public sealed class PdlParserTests
{
    [Theory]
    [InlineData("policy file rel owner")]
    [InlineData("policy file\nrel owner")]
    [InlineData("policy file\r\nrel owner")]
    [InlineData("policy file rel owner rel editor (dir | cmp owner)")]
    [InlineData("policy file rel owner rel editor (direct | cmp owner)")]
    [InlineData("policy file rel owner rel editor (direct | computed owner)")]
    [InlineData("policy file rel owner rel editor (dir | computed owner)")]
    [InlineData("policy file\nrel owner\nrel\neditor(dir | cmp owner)")]
    public void Parse_SimpleValidPdl_ReturnsDocument(string pdl) =>
        PdlParser.Parse(pdl).Match(
            Right: _ => { },
            Left: error => Assert.Fail($"Parse failed: {error}")
        );

    [Fact]
    public void Parse_SinglePolicy_ReturnsCorrectAst()
    {
        const string pdl = "policy file rel owner";
        var expected = new PolicySet(
            Seq(
                new Policy(
                    PolicyName.From("file"),
                    Seq(
                        new Relation(RelationName.From("owner"), DirectRewrite.Default)
                    )
                )
            )
        );

        _ = PdlParser.Parse(pdl).Match(
            Right: doc => Assert.Equal(expected, doc.PolicySet),
            Left: error => Assert.Fail(error.ToString())
        );
    }

    [Fact]
    public void Parse_ComplexRewriteRule_ReturnsCorrectAst()
    {
        const string pdl = "policy file rel viewer ((dir | cmp editor | tpl (parent, viewer)) ! cmp banned)";

        var unionRewrite = new UnionRewrite(
            Seq<SubjectSetRewrite>(
                DirectRewrite.Default,
                new ComputedSubjectSetRewrite(RelationName.From("editor")),
                new TupleToSubjectSetRewrite(RelationName.From("parent"), RelationName.From("viewer"))
            )
        );

        var exclusionRewrite = new ExclusionRewrite(
            unionRewrite,
            new ComputedSubjectSetRewrite(RelationName.From("banned"))
        );

        var expected = new PolicySet(
            Seq(
                new Policy(
                    PolicyName.From("file"),
                    Seq(
                        new Relation(
                            RelationName.From("viewer"),
                            exclusionRewrite
                        )
                    )
                )
            )
        );

        _ = PdlParser.Parse(pdl).Match(
            Right: doc => Assert.Equal(expected, doc.PolicySet),
            Left: error => Assert.Fail(error.ToString())
        );
    }

    [Fact]
    public void Parse_FullPdlDocument_ReturnsCorrectAst()
    {
        var pdl = File.ReadAllText("Data/doc.policy.pdl");

        var filePolicy = new Policy(
            PolicyName.From("file"),
            Seq(
                new Relation(RelationName.From("owner"), DirectRewrite.Default),
                new Relation(
                    RelationName.From("editor"),
                    new UnionRewrite(
                        Seq<SubjectSetRewrite>(
                            DirectRewrite.Default,
                            new ComputedSubjectSetRewrite(RelationName.From("owner"))
                        )
                    )
                ),
                new Relation(
                    RelationName.From("viewer"),
                    new ExclusionRewrite(
                        new UnionRewrite(
                            Seq<SubjectSetRewrite>(
                                DirectRewrite.Default,
                                new ComputedSubjectSetRewrite(RelationName.From("editor")),
                                new TupleToSubjectSetRewrite(RelationName.From("parent"), RelationName.From("viewer"))
                            )
                        ),
                        new ComputedSubjectSetRewrite(RelationName.From("banned"))
                    )
                ),
                new Relation(
                    RelationName.From("auditor"),
                    new IntersectionRewrite(
                        Seq<SubjectSetRewrite>(
                            DirectRewrite.Default,
                            new ComputedSubjectSetRewrite(RelationName.From("viewer"))
                        )
                    )
                ),
                new Relation(RelationName.From("banned"), DirectRewrite.Default)
            )
        );

        var folderPolicy = new Policy(
            PolicyName.From("folder"),
            Seq(
                new Relation(RelationName.From("owner"), DirectRewrite.Default),
                new Relation(
                    RelationName.From("viewer"),
                    new ExclusionRewrite(
                        new UnionRewrite(
                            Seq<SubjectSetRewrite>(
                                DirectRewrite.Default,
                                new ComputedSubjectSetRewrite(RelationName.From("editor")),
                                new TupleToSubjectSetRewrite(RelationName.From("parent"), RelationName.From("viewer"))
                            )
                        ),
                        new ComputedSubjectSetRewrite(RelationName.From("banned"))
                    )
                ),
                new Relation(RelationName.From("banned"), DirectRewrite.Default)
            )
        );

        var expected = new PolicySet(Seq(filePolicy, folderPolicy));

        _ = PdlParser.Parse(pdl).Match(
            Right: doc =>
            {
                Assert.Equal(expected, doc.PolicySet);
                Assert.Equal(pdl, doc.Pdl);
            },
            Left: error => Assert.Fail(error.ToString())
        );
    }
}
