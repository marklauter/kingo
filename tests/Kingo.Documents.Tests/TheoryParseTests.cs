using Kingo.Theories;
using Results;
using System.Collections.Immutable;
using static Kingo.Documents.Tests.TestHelpers;

namespace Kingo.Documents.Tests;

public sealed class TheoryParseTests
{
    [Fact]
    public void Parse_SimpleDocument_ReturnsDefinedNamespaces()
    {
        const string document = """
            theory: acme
            namespaces:
              file:
                - owner
                - editor: this | owner
            """;

        ImmutableArray<Namespace> expected =
        [
            MakeNs(
                Ns("file"),
                [
                    Bare("owner"),
                    new Relation(
                        Rel("editor"),
                        Union([SubjectSetRewrite.This.Default, Computed("owner")])),
                ]),
        ];

        Assert.Equal(MakeTheory(TheoryId("acme"), expected), ParseSuccess(document));
    }

    [Fact]
    public void Parse_ComplexDocument_ReturnsDefinedNamespaces()
    {
        const string document = """
            # rewrite set operators:
            #   ! = exclusion operator
            #   & = intersection operator
            #   | = union operator

            theory: acme

            namespaces:
              file:                           # namespace
                - owner                       # empty relation - implicit this
                - parent                      # the factset relation the viewer rewrite walks
                - editor: this | owner        # relation with union rewrite
                - viewer: >                   # relation with union, factset, and exclusion rewrites
                    (this | editor | (parent, viewer)) ! banned
                - auditor: this & viewer      # relation with intersection rewrite
                - banned                      # empty relation - implicit this

              # second namespace defined within same document
              folder:
                - owner
                - parent
                - viewer: (this | (parent, viewer)) ! banned
                - banned
            """;

        var file = MakeNs(
            Ns("file"),
            [
                Bare("owner"),
                Bare("parent"),
                new Relation(
                    Rel("editor"),
                    Union([SubjectSetRewrite.This.Default, Computed("owner")])),
                new Relation(
                    Rel("viewer"),
                    Exclusion(
                        Union(
                        [
                            SubjectSetRewrite.This.Default,
                            Computed("editor"),
                            FactTo("parent", "viewer"),
                        ]),
                        Computed("banned"))),
                new Relation(
                    Rel("auditor"),
                    Intersection([SubjectSetRewrite.This.Default, Computed("viewer")])),
                Bare("banned"),
            ]);

        var folder = MakeNs(
            Ns("folder"),
            [
                Bare("owner"),
                Bare("parent"),
                new Relation(
                    Rel("viewer"),
                    Exclusion(
                        Union(
                        [
                            SubjectSetRewrite.This.Default,
                            FactTo("parent", "viewer"),
                        ]),
                        Computed("banned"))),
                Bare("banned"),
            ]);

        Assert.Equal([file, folder], ParseSuccess(document).Namespaces);
    }

    [Theory]
    [InlineData("file:\n  - owner")]
    [InlineData("file:\n  - owner\n  - editor: this")]
    [InlineData("file:\n  - owner\n  - viewer: this | owner")]
    [InlineData("file:\n  - owner\n  - viewer: this & owner")]
    [InlineData("file:\n  - owner\n  - viewer: this ! owner")]
    [InlineData("file:\n  - viewer: (this)")]
    [InlineData("file:\n  - viewer: this # comment")]
    [InlineData("file:\n  - parent\n  - viewer: (parent, child)")]
    [InlineData("file:\n  - owner\n  - parent\n  - banned\n  - viewer: this | (parent, child) & owner ! banned")]
    [InlineData("base: &shared\n  - owner\nfile: *shared")]
    public void Parse_ValidNamespaceMaps_Succeeds(string namespaceMap) =>
        _ = ParseSuccess(Document(namespaceMap));

    [Theory]
    [InlineData("invalid: yaml: content", "theory.syntax")]
    [InlineData("file:\n  - viewer: | this", "theory.syntax")]
    [InlineData("file:\n  - a\nfile:\n  - b", "theory.syntax")]
    [InlineData("file:\n  - a: this\n    a: owner", "theory.syntax")]
    [InlineData("file: *missing", "theory.syntax")]
    [InlineData("file: 5", "theory.namespace")]
    [InlineData("file:\n  a: b", "theory.namespace")]
    [InlineData("file: ''", "theory.namespace")]
    [InlineData("file: 'null'", "theory.namespace")]
    [InlineData("file: NuLL", "theory.namespace")]
    [InlineData("'':\n  - owner", "namespace_name.empty")]
    [InlineData("file name:\n  - owner", "namespace_name.invalid")]
    [InlineData("file-name:\n  - owner", "namespace_name.invalid")]
    [InlineData("123file:\n  - owner", "namespace_name.invalid")]
    [InlineData("file.ext:\n  - owner", "namespace_name.invalid")]
    [InlineData("file:\n  - owner-name", "relation_name.invalid")]
    [InlineData("file:\n  - 123owner", "relation_name.invalid")]
    [InlineData("file:\n  - owner.ext", "relation_name.invalid")]
    [InlineData("file:\n  - ", "relation_name.empty")]
    [InlineData("file:\n  - : this", "theory.syntax")]
    [InlineData("file:\n  - [nested]", "theory.relation")]
    [InlineData("file: &a [*a]", "theory.relation")]
    [InlineData("file:\n  - a: this\n    b: this", "theory.relation")]
    [InlineData("file:\n  - viewer:", "theory.relation")]
    [InlineData("file:\n  - viewer: ''", "theory.rewrite")]
    [InlineData("file:\n  - viewer: ~", "theory.rewrite")]
    [InlineData("? [complex, key]\n: - owner", "theory.namespace")]
    [InlineData("file:\n  - ? [complex, key]\n    : this", "theory.relation")]
    [InlineData("file:\n  - viewer:\n      - nested", "theory.relation")]
    [InlineData("file:\n  - owner: invalid expression syntax", "theory.rewrite")]
    [InlineData("file:\n  - viewer: this |", "theory.rewrite")]
    [InlineData("file:\n  - viewer: this & & owner", "theory.rewrite")]
    [InlineData("file:\n  - viewer: invalid-identifier", "theory.rewrite")]
    [InlineData("file:\n  - viewer: (incomplete factset", "theory.rewrite")]
    [InlineData("file:\n  - viewer: (parent, child, extra)", "theory.rewrite")]
    [InlineData("file:\n  - viewer: 123invalid", "theory.rewrite")]
    [InlineData("file:\n  - this", "theory.relation.reserved")]
    [InlineData("file:\n  - THIS", "theory.relation.reserved")]
    [InlineData("file:\n  - this: owner", "theory.relation.reserved")]
    [InlineData("file:\n  - '...'", "relation_name.invalid")]
    [InlineData("file:\n  - '...': owner", "relation_name.invalid")]
    [InlineData("file:\n  - viewer: editor", "namespace.dangling_reference")]
    [InlineData("file:\n  - viewer: (parent, member)", "namespace.dangling_reference")]
    [InlineData("file:\n  - viewer: viewer", "namespace.rewrite_cycle")]
    [InlineData("file:\n  - editor: viewer\n  - viewer: editor", "namespace.rewrite_cycle")]
    public void Parse_InvalidNamespaceMaps_FailsWithExpectedCode(string namespaceMap, string expectedCode)
    {
        var errors = ParseFailure(Document(namespaceMap));

        Assert.All(errors, error => Assert.Equal(ErrorType.Validation, error.Type));
        Assert.Contains(errors, error => error.Code.Value == expectedCode);
    }

    [Theory]
    [InlineData("", "theory.document")]
    [InlineData("   ", "theory.document")]
    [InlineData("null", "theory.document")]
    [InlineData("scalar", "theory.document")]
    [InlineData("[]", "theory.document")]
    [InlineData("{}", "theory.document")]
    [InlineData("theory: acme\n---\ntheory: other", "theory.document")]
    [InlineData("namespaces:\n  file:\n    - owner", "theory.document")]
    [InlineData("theory: acme", "theory.document")]
    [InlineData("theory: acme\nnamespaces: 5", "theory.document")]
    [InlineData("theory: acme\nnamespaces: []", "theory.document")]
    [InlineData("theory: [acme]\nnamespaces:\n  file:\n    - owner", "theory.document")]
    [InlineData("theory:\nnamespaces:\n  file:\n    - owner", "theory_name.empty")]
    [InlineData("file:\n  - owner", "theory.document")]
    [InlineData("theory: ''\nnamespaces:\n  file:\n    - owner", "theory_name.empty")]
    [InlineData("theory: acme corp\nnamespaces:\n  file:\n    - owner", "theory_name.invalid")]
    [InlineData("theory: 123acme\nnamespaces:\n  file:\n    - owner", "theory_name.invalid")]
    [InlineData("theory: acme-corp\nnamespaces:\n  file:\n    - owner", "theory_name.invalid")]
    public void Parse_InvalidEnvelope_FailsWithExpectedCode(string document, string expectedCode)
    {
        var errors = ParseFailure(document);

        Assert.All(errors, error => Assert.Equal(ErrorType.Validation, error.Type));
        Assert.Contains(errors, error => error.Code.Value == expectedCode);
    }

    [Fact]
    public void Parse_TheoryName_IsTheTheoriesTheoryKey()
    {
        var domain = ParseSuccess(Document("file:\n  - owner", name: "acme"));

        Assert.Equal(TheoryId("acme"), domain.Name);
    }

    [Fact]
    public void Parse_MixedCaseTheoryName_NormalizesToLowercase()
    {
        var domain = ParseSuccess(Document("file:\n  - owner", name: "ACME"));

        Assert.Equal(TheoryId("acme"), domain.Name);
    }

    [Fact]
    public void Parse_DefectsInNameAndNamespaces_AccumulateAcrossBoth()
    {
        var errors = ParseFailure("theory: 123acme\nnamespaces:\n  123file:\n    - owner");

        Assert.Equal(2, errors.Length);
        Assert.Equal("theory_name.invalid", errors[0].Code.Value);
        Assert.Equal("namespace_name.invalid", errors[1].Code.Value);
    }

    [Fact]
    public void Parse_MissingRewriteExpression_NamesTheRelation()
    {
        var errors = ParseFailure(Document("file:\n  - viewer:"));

        var error = Assert.Single(errors);
        Assert.Equal("theory.relation", error.Code.Value);
        Assert.Contains("'viewer'", error.Message.Value, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_PlainNullExpressionText_IsTheNullIdentifier()
    {
        var ns = Assert.Single(ParseSuccess(Document("file:\n  - null\n  - viewer: null")).Namespaces);

        ImmutableArray<Relation> expected = [Bare("null"), new Relation(Rel("viewer"), Computed("null"))];
        Assert.Equal(expected, ns.Relations);
    }

    [Fact]
    public void Parse_DefectsInOneRelationPair_AccumulateAcrossNameAndExpression()
    {
        var errors = ParseFailure(Document("123file:\n  - 456bad: this |"));

        Assert.Equal(3, errors.Length);
        Assert.Equal("namespace_name.invalid", errors[0].Code.Value);
        Assert.Equal("relation_name.invalid", errors[1].Code.Value);
        Assert.Equal("theory.rewrite", errors[2].Code.Value);
    }

    [Fact]
    public void Parse_MultipleDefects_AccumulatesEveryErrorInDocumentOrder()
    {
        const string namespaceMap = """
            123file:
              - owner
            folder:
              - 123bad
              - viewer: this |
            """;

        var errors = ParseFailure(Document(namespaceMap));

        Assert.Equal(3, errors.Length);
        Assert.Equal("namespace_name.invalid", errors[0].Code.Value);
        Assert.Equal("relation_name.invalid", errors[1].Code.Value);
        Assert.Equal("theory.rewrite", errors[2].Code.Value);
    }

    [Fact]
    public void Parse_CaseVariantNamespaceKeys_FailsAsDuplicate()
    {
        var errors = ParseFailure(Document("file:\n  - owner\nFILE:\n  - viewer"));

        var error = Assert.Single(errors);
        Assert.Equal("theory.duplicate_namespace", error.Code.Value);
        Assert.Contains("'file'", error.Message.Value, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_DuplicateRelationNames_FailsThroughDefine()
    {
        var errors = ParseFailure(Document("file:\n  - owner\n  - owner"));

        var error = Assert.Single(errors);
        Assert.Equal("namespace.duplicate_relation", error.Code.Value);
    }

    [Fact]
    public void Parse_MixedCaseIdentifiers_NormalizesToLowercase()
    {
        const string namespaceMap = """
            FILE:
              - OWNER
              - EDITOR: THIS | Owner
            """;

        ImmutableArray<Namespace> expected =
        [
            MakeNs(
                Ns("file"),
                [
                    Bare("owner"),
                    new Relation(
                        Rel("editor"),
                        Union([SubjectSetRewrite.This.Default, Computed("owner")])),
                ]),
        ];

        Assert.Equal(expected, ParseSuccess(Document(namespaceMap)).Namespaces);
    }

    [Fact]
    public void Parse_EmptyNamespaceMap_FailsAsEmptyTheory()
    {
        var errors = ParseFailure(Document("{}"));

        Assert.Equal("theory.empty", Assert.Single(errors).Code.Value);
    }

    [Theory]
    [InlineData("file:")]
    [InlineData("file: null")]
    [InlineData("file: Null")]
    [InlineData("file: NULL")]
    [InlineData("file: ~")]
    [InlineData("file: []")]
    public void Parse_NamespaceWithoutRelations_ReturnsEmptyRelations(string namespaceMap)
    {
        var ns = Assert.Single(ParseSuccess(Document(namespaceMap)).Namespaces);

        Assert.Equal(Ns("file"), ns.Name);
        Assert.Empty(ns.Relations);
    }
}
