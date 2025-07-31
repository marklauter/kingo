using Kingo.Policies.Pdl;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Kingo.Policies.Tests;

public sealed class PdlParserTests
{
    [Theory]
    [InlineData("namespace file relation owner")]
    [InlineData("namespace file\n relation owner")]
    [InlineData("namespace file\r\nrelation owner")]
    [InlineData("namespace file relation owner relation editor (direct | computed owner)")]
    [InlineData("namespace file relation\nowner relation\neditor\n(direct\n| computed owner)\n")]
    [InlineData("namespace file relation owner relation editor \n(\ndirect | \ncomputed owner\n)")]
    [InlineData("namespace file relation owner relation editor (direct & computed owner)")]
    [InlineData("namespace file\r\nrelation owner\r\nrelation\r\neditor(direct | computed owner)")]
    [InlineData("/n file /r owner")]
    [InlineData("/n file\n/r owner")]
    [InlineData("/n file\r\n/r owner")]
    [InlineData("/n file /r owner /r editor (/d | /c owner)")]
    [InlineData("/n file /r\nowner /r\neditor\n(/d\n| /c owner)\n")]
    [InlineData("/n file /r owner /r editor \n(\n/d | \n/c owner\n)")]
    [InlineData("/n file /r owner /r editor (/d & /c owner)")]
    [InlineData("/n file\r\n /r owner\r\n/r\neditor(/d | /c owner)")]
    public void Parse_SimpleValidPdl_ReturnsDocument(string pdl) =>
        PdlParser.Parse(pdl).Run().Match(
            Succ: _ => { },
            Fail: error => Assert.Fail($"Parse failed: {error}")
        );

    [Fact]
    public void Parse_SinglePolicy_ReturnsCorrectAst()
    {
        const string pdl = "namespace file relation owner";
        var expected = new NamespaceSet(
            Seq(
                new Namespace(
                    NamespaceIdentifier.From("file"),
                    Seq(
                        new Relation(RelationIdentifier.From("owner"), DirectRewrite.Default)
                    )
                )
            )
        );

        _ = PdlParser.Parse(pdl).Run().Match(
            Succ: doc => Assert.Equal(expected, doc.PolicySet),
            Fail: error => Assert.Fail(error.ToString())
        );
    }

    [Fact]
    public void Parse_ComplexRewriteRule_ReturnsCorrectAst()
    {
        const string pdl = "/n file /r viewer ((/d | /c editor | /t (parent, viewer)) ! /c banned)";

        var unionRewrite = new UnionRewrite(
            Seq<SubjectSetRewrite>(
                DirectRewrite.Default,
                new ComputedSubjectSetRewrite(RelationIdentifier.From("editor")),
                new TupleToSubjectSetRewrite(RelationIdentifier.From("parent"), RelationIdentifier.From("viewer"))
            )
        );

        var exclusionRewrite = new ExclusionRewrite(
            unionRewrite,
            new ComputedSubjectSetRewrite(RelationIdentifier.From("banned"))
        );

        var expected = new NamespaceSet(
            Seq(
                new Namespace(
                    NamespaceIdentifier.From("file"),
                    Seq(
                        new Relation(
                            RelationIdentifier.From("viewer"),
                            exclusionRewrite
                        )
                    )
                )
            )
        );

        _ = PdlParser.Parse(pdl).Run().Match(
            Succ: doc => Assert.Equal(expected, doc.PolicySet),
            Fail: error => Assert.Fail(error.ToString())
        );
    }

    [Fact]
    public void Parse_FullPdlDocument_ReturnsCorrectAst()
    {
        var pdl = File.ReadAllText("Data/doc.policy.pdl");

        var filePolicy = new Namespace(
            NamespaceIdentifier.From("file"),
            Seq(
                new Relation(RelationIdentifier.From("owner"), DirectRewrite.Default),
                new Relation(
                    RelationIdentifier.From("editor"),
                    new UnionRewrite(
                        Seq<SubjectSetRewrite>(
                            DirectRewrite.Default,
                            new ComputedSubjectSetRewrite(RelationIdentifier.From("owner"))
                        )
                    )
                ),
                new Relation(
                    RelationIdentifier.From("viewer"),
                    new ExclusionRewrite(
                        new UnionRewrite(
                            Seq<SubjectSetRewrite>(
                                DirectRewrite.Default,
                                new ComputedSubjectSetRewrite(RelationIdentifier.From("editor")),
                                new TupleToSubjectSetRewrite(RelationIdentifier.From("parent"), RelationIdentifier.From("viewer"))
                            )
                        ),
                        new ComputedSubjectSetRewrite(RelationIdentifier.From("banned"))
                    )
                ),
                new Relation(
                    RelationIdentifier.From("auditor"),
                    new IntersectionRewrite(
                        Seq<SubjectSetRewrite>(
                            DirectRewrite.Default,
                            new ComputedSubjectSetRewrite(RelationIdentifier.From("viewer"))
                        )
                    )
                ),
                new Relation(RelationIdentifier.From("banned"), DirectRewrite.Default)
            )
        );

        var folderPolicy = new Namespace(
            NamespaceIdentifier.From("folder"),
            Seq(
                new Relation(RelationIdentifier.From("owner"), DirectRewrite.Default),
                new Relation(
                    RelationIdentifier.From("viewer"),
                    new ExclusionRewrite(
                        new UnionRewrite(
                            Seq<SubjectSetRewrite>(
                                DirectRewrite.Default,
                                new ComputedSubjectSetRewrite(RelationIdentifier.From("editor")),
                                new TupleToSubjectSetRewrite(RelationIdentifier.From("parent"), RelationIdentifier.From("viewer"))
                            )
                        ),
                        new ComputedSubjectSetRewrite(RelationIdentifier.From("banned"))
                    )
                ),
                new Relation(RelationIdentifier.From("banned"), DirectRewrite.Default)
            )
        );

        var expected = new NamespaceSet(Seq(filePolicy, folderPolicy));

        _ = PdlParser.Parse(pdl).Run().Match(
            Succ: doc =>
            {
                Assert.Equal(expected, doc.PolicySet);
                Assert.Equal(pdl, doc.Pdl);
            },
            Fail: error => Assert.Fail(error.ToString())
        );
    }
}
