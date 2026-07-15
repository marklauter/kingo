using Kingo.Policies;
using Results;
using System.Collections.Immutable;
using static Kingo.Serialization.Pdl.Tests.TestHelpers;

namespace Kingo.Serialization.Pdl.Tests;

public sealed class PdlParseTests
{
    [Fact]
    public void Parse_SimpleDocument_ReturnsDefinedNamespaces()
    {
        const string pdl = """
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
                    new Relationship(
                        Rel("editor"),
                        new UnionRewrite([ThisRewrite.Default, Computed("owner")])),
                ]),
        ];

        Assert.Equal(expected, ParseSuccess(pdl).Namespaces);
    }

    [Fact]
    public void Parse_ComplexDocument_ReturnsDefinedNamespaces()
    {
        // the example document from docs/notes/pdl-yaml.md: comments, a folded block scalar, two namespaces
        const string pdl = """
            # rewrite set operators:
            #   ! = exclusion operator
            #   & = intersection operator
            #   | = union operator

            file:                           # namespace
              - owner                       # empty relationship - implicit this
              - editor: this | owner        # relationship with union rewrite
              - viewer: >                   # relationship with union, tupleset, and exclusion rewrites
                  (this | editor | (parent, viewer)) ! banned
              - auditor: this & viewer      # relationship with intersection rewrite
              - banned                      # empty relationship - implicit this

            # second policy defined within same document
            folder:
              - owner
              - viewer: (this | (parent, viewer)) ! banned
              - banned
            """;

        var file = MakeNs(
            Ns("file"),
            [
                Bare("owner"),
                new Relationship(
                    Rel("editor"),
                    new UnionRewrite([ThisRewrite.Default, Computed("owner")])),
                new Relationship(
                    Rel("viewer"),
                    new ExclusionRewrite(
                        new UnionRewrite(
                        [
                            ThisRewrite.Default,
                            Computed("editor"),
                            new TupleToSubjectSetRewrite(Rel("parent"), Rel("viewer")),
                        ]),
                        Computed("banned"))),
                new Relationship(
                    Rel("auditor"),
                    new IntersectionRewrite([ThisRewrite.Default, Computed("viewer")])),
                Bare("banned"),
            ]);

        var folder = MakeNs(
            Ns("folder"),
            [
                Bare("owner"),
                new Relationship(
                    Rel("viewer"),
                    new ExclusionRewrite(
                        new UnionRewrite(
                        [
                            ThisRewrite.Default,
                            new TupleToSubjectSetRewrite(Rel("parent"), Rel("viewer")),
                        ]),
                        Computed("banned"))),
                Bare("banned"),
            ]);

        Assert.Equal([file, folder], ParseSuccess(pdl).Namespaces);
    }

    [Theory]
    [InlineData("file:\n  - owner")]
    [InlineData("file:\n  - owner\n  - editor: this")]
    [InlineData("file:\n  - viewer: this | owner")]
    [InlineData("file:\n  - viewer: this & owner")]
    [InlineData("file:\n  - viewer: this ! owner")]
    [InlineData("file:\n  - viewer: (this)")]
    [InlineData("file:\n  - viewer: this # comment")]
    [InlineData("file:\n  - viewer: (parent, child)")]
    [InlineData("file:\n  - viewer: this | (parent, child) & owner ! banned")]
    public void Parse_ValidDocuments_Succeeds(string pdl) =>
        _ = ParseSuccess(pdl);

    [Theory]
    [InlineData("", "pdl.document")]
    [InlineData("   ", "pdl.document")]
    [InlineData("null", "pdl.document")]
    [InlineData("scalar", "pdl.document")]
    [InlineData("[]", "pdl.document")]
    [InlineData("a:\n  - x\n---\nb:\n  - y", "pdl.document")]
    [InlineData("invalid: yaml: content", "pdl.syntax")]
    [InlineData("file:\n  - viewer: | this", "pdl.syntax")]
    [InlineData("file: 5", "pdl.namespace")]
    [InlineData("file:\n  a: b", "pdl.namespace")]
    [InlineData("file name:\n  - owner", "namespace_id.invalid")]
    [InlineData("file-name:\n  - owner", "namespace_id.invalid")]
    [InlineData("123file:\n  - owner", "namespace_id.invalid")]
    [InlineData("file.ext:\n  - owner", "namespace_id.invalid")]
    [InlineData("file:\n  - owner-name", "relationship_id.invalid")]
    [InlineData("file:\n  - 123owner", "relationship_id.invalid")]
    [InlineData("file:\n  - owner.ext", "relationship_id.invalid")]
    [InlineData("file:\n  - ", "relationship_id.empty")]
    [InlineData("file:\n  - [nested]", "pdl.relationship")]
    [InlineData("file:\n  - a: this\n    b: this", "pdl.relationship")]
    [InlineData("? [complex, key]\n: - owner", "pdl.namespace")]
    [InlineData("file:\n  - ? [complex, key]\n    : this", "pdl.relationship")]
    [InlineData("file:\n  - viewer:\n      - nested", "pdl.relationship")]
    [InlineData("file:\n  - owner: invalid expression syntax", "pdl.rewrite")]
    [InlineData("file:\n  - viewer: this |", "pdl.rewrite")]
    [InlineData("file:\n  - viewer: this & & owner", "pdl.rewrite")]
    [InlineData("file:\n  - viewer: invalid-identifier", "pdl.rewrite")]
    [InlineData("file:\n  - viewer: (incomplete tuple", "pdl.rewrite")]
    [InlineData("file:\n  - viewer: (parent, child, extra)", "pdl.rewrite")]
    [InlineData("file:\n  - viewer: 123invalid", "pdl.rewrite")]
    [InlineData("file:\n  - this", "pdl.relationship.reserved")]
    [InlineData("file:\n  - THIS", "pdl.relationship.reserved")]
    [InlineData("file:\n  - this: owner", "pdl.relationship.reserved")]
    [InlineData("file:\n  - '...'", "pdl.relationship.reserved")]
    [InlineData("file:\n  - '...': owner", "pdl.relationship.reserved")]
    public void Parse_InvalidDocuments_FailsWithExpectedCode(string pdl, string expectedCode)
    {
        var errors = ParseFailure(pdl);

        Assert.All(errors, error => Assert.Equal(ErrorType.Validation, error.Type));
        Assert.Contains(errors, error => error.Code == expectedCode);
    }

    [Fact]
    public void Parse_MultipleDefects_AccumulatesEveryErrorInDocumentOrder()
    {
        const string pdl = """
            123file:
              - owner
            folder:
              - 123bad
              - viewer: this |
            """;

        var errors = ParseFailure(pdl);

        Assert.Equal(3, errors.Length);
        Assert.Equal("namespace_id.invalid", errors[0].Code);
        Assert.Equal("relationship_id.invalid", errors[1].Code);
        Assert.Equal("pdl.rewrite", errors[2].Code);
    }

    [Fact]
    public void Parse_CaseVariantNamespaceKeys_FailsAsDuplicate()
    {
        // distinct YAML keys, one namespace identity after Parse's lowercase normalization
        var errors = ParseFailure("file:\n  - owner\nFILE:\n  - viewer");

        var error = Assert.Single(errors);
        Assert.Equal("policy.duplicate_namespace", error.Code);
        Assert.Contains("'file'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_DuplicateRelationshipNames_FailsThroughDefine()
    {
        var errors = ParseFailure("file:\n  - owner\n  - owner");

        var error = Assert.Single(errors);
        Assert.Equal("namespace.duplicate_relationship", error.Code);
    }

    [Fact]
    public void Parse_MixedCaseIdentifiers_NormalizesToLowercase()
    {
        const string pdl = """
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
                    new Relationship(
                        Rel("editor"),
                        new UnionRewrite([ThisRewrite.Default, Computed("owner")])),
                ]),
        ];

        Assert.Equal(expected, ParseSuccess(pdl).Namespaces);
    }

    [Fact]
    public void Parse_EmptyMapping_FailsAsEmptyPolicy()
    {
        // a policy is never empty: the absence of namespaces is the absence of a policy
        var errors = ParseFailure("{}");

        Assert.Equal("policy.empty", Assert.Single(errors).Code);
    }

    [Theory]
    [InlineData("file:")]
    [InlineData("file: null")]
    [InlineData("file: Null")]
    [InlineData("file: NULL")]
    [InlineData("file: ~")]
    [InlineData("file: []")]
    public void Parse_NamespaceWithoutRelationships_ReturnsEmptyRelationships(string pdl)
    {
        var ns = Assert.Single(ParseSuccess(pdl).Namespaces);

        Assert.Equal(Ns("file"), ns.Name);
        Assert.Empty(ns.Relationships);
    }
}
