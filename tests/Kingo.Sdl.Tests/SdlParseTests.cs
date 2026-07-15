using Kingo.Schemas;
using Results;
using System.Collections.Immutable;
using static Kingo.Sdl.Tests.TestHelpers;

namespace Kingo.Sdl.Tests;

public sealed class SdlParseTests
{
    [Fact]
    public void Parse_SimpleDocument_ReturnsDefinedNamespaces()
    {
        const string sdl = """
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

        Assert.Equal(expected, ParseSuccess(sdl).Namespaces);
    }

    [Fact]
    public void Parse_ComplexDocument_ReturnsDefinedNamespaces()
    {
        // the example document from docs/notes/sdl-yaml.md: comments, a folded block scalar, two namespaces
        const string sdl = """
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

            # second namespace defined within same document
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

        Assert.Equal([file, folder], ParseSuccess(sdl).Namespaces);
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
    public void Parse_ValidDocuments_Succeeds(string sdl) =>
        _ = ParseSuccess(sdl);

    [Theory]
    [InlineData("", "sdl.document")]
    [InlineData("   ", "sdl.document")]
    [InlineData("null", "sdl.document")]
    [InlineData("scalar", "sdl.document")]
    [InlineData("[]", "sdl.document")]
    [InlineData("a:\n  - x\n---\nb:\n  - y", "sdl.document")]
    [InlineData("invalid: yaml: content", "sdl.syntax")]
    [InlineData("file:\n  - viewer: | this", "sdl.syntax")]
    [InlineData("file: 5", "sdl.namespace")]
    [InlineData("file:\n  a: b", "sdl.namespace")]
    [InlineData("file name:\n  - owner", "namespace_id.invalid")]
    [InlineData("file-name:\n  - owner", "namespace_id.invalid")]
    [InlineData("123file:\n  - owner", "namespace_id.invalid")]
    [InlineData("file.ext:\n  - owner", "namespace_id.invalid")]
    [InlineData("file:\n  - owner-name", "relationship_id.invalid")]
    [InlineData("file:\n  - 123owner", "relationship_id.invalid")]
    [InlineData("file:\n  - owner.ext", "relationship_id.invalid")]
    [InlineData("file:\n  - ", "relationship_id.empty")]
    [InlineData("file:\n  - [nested]", "sdl.relationship")]
    [InlineData("file:\n  - a: this\n    b: this", "sdl.relationship")]
    [InlineData("? [complex, key]\n: - owner", "sdl.namespace")]
    [InlineData("file:\n  - ? [complex, key]\n    : this", "sdl.relationship")]
    [InlineData("file:\n  - viewer:\n      - nested", "sdl.relationship")]
    [InlineData("file:\n  - owner: invalid expression syntax", "sdl.rewrite")]
    [InlineData("file:\n  - viewer: this |", "sdl.rewrite")]
    [InlineData("file:\n  - viewer: this & & owner", "sdl.rewrite")]
    [InlineData("file:\n  - viewer: invalid-identifier", "sdl.rewrite")]
    [InlineData("file:\n  - viewer: (incomplete tuple", "sdl.rewrite")]
    [InlineData("file:\n  - viewer: (parent, child, extra)", "sdl.rewrite")]
    [InlineData("file:\n  - viewer: 123invalid", "sdl.rewrite")]
    [InlineData("file:\n  - this", "sdl.relationship.reserved")]
    [InlineData("file:\n  - THIS", "sdl.relationship.reserved")]
    [InlineData("file:\n  - this: owner", "sdl.relationship.reserved")]
    [InlineData("file:\n  - '...'", "sdl.relationship.reserved")]
    [InlineData("file:\n  - '...': owner", "sdl.relationship.reserved")]
    public void Parse_InvalidDocuments_FailsWithExpectedCode(string sdl, string expectedCode)
    {
        var errors = ParseFailure(sdl);

        Assert.All(errors, error => Assert.Equal(ErrorType.Validation, error.Type));
        Assert.Contains(errors, error => error.Code == expectedCode);
    }

    [Fact]
    public void Parse_MultipleDefects_AccumulatesEveryErrorInDocumentOrder()
    {
        const string sdl = """
            123file:
              - owner
            folder:
              - 123bad
              - viewer: this |
            """;

        var errors = ParseFailure(sdl);

        Assert.Equal(3, errors.Length);
        Assert.Equal("namespace_id.invalid", errors[0].Code);
        Assert.Equal("relationship_id.invalid", errors[1].Code);
        Assert.Equal("sdl.rewrite", errors[2].Code);
    }

    [Fact]
    public void Parse_CaseVariantNamespaceKeys_FailsAsDuplicate()
    {
        // distinct YAML keys, one namespace identity after Parse's lowercase normalization
        var errors = ParseFailure("file:\n  - owner\nFILE:\n  - viewer");

        var error = Assert.Single(errors);
        Assert.Equal("schema.duplicate_namespace", error.Code);
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
        const string sdl = """
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

        Assert.Equal(expected, ParseSuccess(sdl).Namespaces);
    }

    [Fact]
    public void Parse_EmptyMapping_FailsAsEmptySchema()
    {
        // a schema is never empty: the absence of namespaces is the absence of a schema
        var errors = ParseFailure("{}");

        Assert.Equal("schema.empty", Assert.Single(errors).Code);
    }

    [Theory]
    [InlineData("file:")]
    [InlineData("file: null")]
    [InlineData("file: Null")]
    [InlineData("file: NULL")]
    [InlineData("file: ~")]
    [InlineData("file: []")]
    public void Parse_NamespaceWithoutRelationships_ReturnsEmptyRelationships(string sdl)
    {
        var ns = Assert.Single(ParseSuccess(sdl).Namespaces);

        Assert.Equal(Ns("file"), ns.Name);
        Assert.Empty(ns.Relationships);
    }
}
