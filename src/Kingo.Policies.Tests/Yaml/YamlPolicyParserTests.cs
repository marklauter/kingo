//using Kingo.Policies.Yaml;
//using static LanguageExt.Prelude;

//namespace Kingo.Policies.Tests.Yaml;

//public sealed class YamlPolicyParserTests
//{
//    [Fact]
//    public void Parse_SimpleYamlPolicy_ReturnsCorrectAst()
//    {
//        const string yaml = """
//            file:
//              - owner
//              - editor: this | owner
//            """;

//        var expected = new NamespaceSet(
//            Seq(
//                new Namespace(
//                    NamespaceIdentifier.From("file"),
//                    Seq(
//                        new Relation(RelationIdentifier.From("owner"), DirectRewrite.Default),
//                        new Relation(
//                            RelationIdentifier.From("editor"),
//                            new UnionRewrite(Seq<SubjectSetRewrite>(
//                                DirectRewrite.Default,
//                                new ComputedSubjectSetRewrite(RelationIdentifier.From("owner"))
//                            ))
//                        )
//                    )
//                )
//            )
//        );

//        var doc = YamlPolicyParser.Parse(yaml);
//        Assert.NotNull(doc);
//        Assert.Equal(expected, doc.PolicySet);
//    }

//    [Fact]
//    public void Parse_ComplexYamlPolicy_ReturnsCorrectAst()
//    {
//        var yaml = File.ReadAllText("Data/doc.policy.yml");

//        var filePolicy = new Namespace(
//            NamespaceIdentifier.From("file"),
//            Seq(
//                new Relation(RelationIdentifier.From("owner"), DirectRewrite.Default),
//                new Relation(
//                    RelationIdentifier.From("editor"),
//                    new UnionRewrite(Seq<SubjectSetRewrite>(
//                        DirectRewrite.Default,
//                        new ComputedSubjectSetRewrite(RelationIdentifier.From("owner"))
//                    ))
//                ),
//                new Relation(
//                    RelationIdentifier.From("viewer"),
//                    new ExclusionRewrite(
//                        new UnionRewrite(Seq<SubjectSetRewrite>(
//                            DirectRewrite.Default,
//                            new ComputedSubjectSetRewrite(RelationIdentifier.From("editor")),
//                            new TupleToSubjectSetRewrite(RelationIdentifier.From("parent"), RelationIdentifier.From("viewer"))
//                        )),
//                        new ComputedSubjectSetRewrite(RelationIdentifier.From("banned"))
//                    )
//                ),
//                new Relation(
//                    RelationIdentifier.From("auditor"),
//                    new IntersectionRewrite(Seq<SubjectSetRewrite>(
//                        DirectRewrite.Default,
//                        new ComputedSubjectSetRewrite(RelationIdentifier.From("viewer"))
//                    ))
//                ),
//                new Relation(RelationIdentifier.From("banned"), DirectRewrite.Default)
//            )
//        );

//        var folderPolicy = new Namespace(
//            NamespaceIdentifier.From("folder"),
//            Seq(
//                new Relation(RelationIdentifier.From("owner"), DirectRewrite.Default),
//                new Relation(
//                    RelationIdentifier.From("viewer"),
//                    new ExclusionRewrite(
//                        new UnionRewrite(Seq<SubjectSetRewrite>(
//                            DirectRewrite.Default,
//                            new TupleToSubjectSetRewrite(RelationIdentifier.From("parent"), RelationIdentifier.From("viewer"))
//                        )),
//                        new ComputedSubjectSetRewrite(RelationIdentifier.From("banned"))
//                    )
//                ),
//                new Relation(RelationIdentifier.From("banned"), DirectRewrite.Default)
//            )
//        );

//        var expected = new NamespaceSet(Seq(filePolicy, folderPolicy));

//        var doc = YamlPolicyParser.Parse(yaml);
//        Assert.NotNull(doc);
//        Assert.Equal(expected, doc.PolicySet);
//        Assert.Equal(yaml, doc.Pdl);
//    }

//    [Theory]
//    [InlineData("file:\n  - owner")]
//    [InlineData("file:\n  - owner\n  - editor: this")]
//    [InlineData("file:\n  - viewer: this | owner")]
//    [InlineData("file:\n  - viewer: this & owner")]
//    [InlineData("file:\n  - viewer: this ! owner")]
//    public void Parse_ValidYamlFormats_ReturnsSuccess(string yaml)
//    {
//        var result = YamlPolicyParser.Parse(yaml);
//        Assert.NotNull(result);
//    }
//}
