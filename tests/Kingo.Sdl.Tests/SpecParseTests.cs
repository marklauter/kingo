using Kingo.Domains;
using Results;
using System.Collections.Immutable;
using static Kingo.Sdl.Tests.TestHelpers;

namespace Kingo.Sdl.Tests;

public sealed class SpecParseTests
{
    [Fact]
    public void Parse_SimpleDocument_ReturnsDefinedNamespaces()
    {
        const string sdl = """
            domain: acme
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
                    new Relationship(
                        Rel("editor"),
                        Union([SubjectSetRewrite.This.Default, Computed("owner")])),
                ]),
        ];

        Assert.Equal(MakeSpec(SpecId("acme"), expected), ParseSuccess(sdl));
    }

    [Fact]
    public void Parse_ComplexDocument_ReturnsDefinedNamespaces()
    {
        // the example document from [[specs]]: comments, a folded block scalar, two namespaces
        const string sdl = """
            # rewrite set operators:
            #   ! = exclusion operator
            #   & = intersection operator
            #   | = union operator

            domain: acme

            namespaces:
              file:                           # namespace
                - owner                       # empty relationship - implicit this
                - parent                      # the factset relationship the viewer rewrite walks
                - editor: this | owner        # relationship with union rewrite
                - viewer: >                   # relationship with union, factset, and exclusion rewrites
                    (this | editor | (parent, viewer)) ! banned
                - auditor: this & viewer      # relationship with intersection rewrite
                - banned                      # empty relationship - implicit this

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
                new Relationship(
                    Rel("editor"),
                    Union([SubjectSetRewrite.This.Default, Computed("owner")])),
                new Relationship(
                    Rel("viewer"),
                    Exclusion(
                        Union(
                        [
                            SubjectSetRewrite.This.Default,
                            Computed("editor"),
                            FactTo("parent", "viewer"),
                        ]),
                        Computed("banned"))),
                new Relationship(
                    Rel("auditor"),
                    Intersection([SubjectSetRewrite.This.Default, Computed("viewer")])),
                Bare("banned"),
            ]);

        var folder = MakeNs(
            Ns("folder"),
            [
                Bare("owner"),
                Bare("parent"),
                new Relationship(
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

        Assert.Equal([file, folder], ParseSuccess(sdl).Namespaces);
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
    [InlineData("base: &shared\n  - owner\nfile: *shared")] // anchor reuse is plain YAML; both namespaces get the shared relationship list
    public void Parse_ValidNamespaceMaps_Succeeds(string namespaceMap) =>
        _ = ParseSuccess(Document(namespaceMap));

    [Theory]
    [InlineData("invalid: yaml: content", "domain.syntax")]
    [InlineData("file:\n  - viewer: | this", "domain.syntax")]
    [InlineData("file:\n  - a\nfile:\n  - b", "domain.syntax")] // exact-duplicate keys are rejected by YAML itself, before namespace identity is compared
    [InlineData("file:\n  - a: this\n    a: owner", "domain.syntax")] // duplicate keys inside a relationship mapping, likewise
    [InlineData("file: *missing", "domain.syntax")] // unresolved alias
    [InlineData("file: 5", "domain.namespace")]
    [InlineData("file:\n  a: b", "domain.namespace")]
    [InlineData("file: ''", "domain.namespace")] // only *plain* null forms mean an empty namespace; a quoted empty string is not one
    [InlineData("file: 'null'", "domain.namespace")]
    [InlineData("file: NuLL", "domain.namespace")] // the core-schema null forms are exact-case: null, Null, NULL
    [InlineData("'':\n  - owner", "namespace_name.empty")]
    [InlineData("file name:\n  - owner", "namespace_name.invalid")]
    [InlineData("file-name:\n  - owner", "namespace_name.invalid")]
    [InlineData("123file:\n  - owner", "namespace_name.invalid")]
    [InlineData("file.ext:\n  - owner", "namespace_name.invalid")]
    [InlineData("file:\n  - owner-name", "relationship_name.invalid")]
    [InlineData("file:\n  - 123owner", "relationship_name.invalid")]
    [InlineData("file:\n  - owner.ext", "relationship_name.invalid")]
    [InlineData("file:\n  - ", "relationship_name.empty")]
    [InlineData("file:\n  - : this", "domain.syntax")] // YamlDotNet cannot load this shape and throws ArgumentException, not YamlException; both translate
    [InlineData("file:\n  - [nested]", "domain.relationship")]
    [InlineData("file: &a [*a]", "domain.relationship")] // a self-referential alias resolves to a nested sequence, not a hang or a crash
    [InlineData("file:\n  - a: this\n    b: this", "domain.relationship")]
    [InlineData("file:\n  - viewer:", "domain.relationship")] // a pair missing its rewrite expression; the bare-name form is how SDL spells "no rewrite"
    [InlineData("file:\n  - viewer: ''", "domain.rewrite")] // a quoted empty string is not a missing value: it is an (empty, invalid) expression
    [InlineData("file:\n  - viewer: ~", "domain.rewrite")] // plain scalar text is expression source, and '~' cannot lex
    [InlineData("? [complex, key]\n: - owner", "domain.namespace")]
    [InlineData("file:\n  - ? [complex, key]\n    : this", "domain.relationship")]
    [InlineData("file:\n  - viewer:\n      - nested", "domain.relationship")]
    [InlineData("file:\n  - owner: invalid expression syntax", "domain.rewrite")]
    [InlineData("file:\n  - viewer: this |", "domain.rewrite")]
    [InlineData("file:\n  - viewer: this & & owner", "domain.rewrite")]
    [InlineData("file:\n  - viewer: invalid-identifier", "domain.rewrite")]
    [InlineData("file:\n  - viewer: (incomplete factset", "domain.rewrite")]
    [InlineData("file:\n  - viewer: (parent, child, extra)", "domain.rewrite")]
    [InlineData("file:\n  - viewer: 123invalid", "domain.rewrite")]
    [InlineData("file:\n  - this", "domain.relationship.reserved")]
    [InlineData("file:\n  - THIS", "domain.relationship.reserved")]
    [InlineData("file:\n  - this: owner", "domain.relationship.reserved")]
    [InlineData("file:\n  - '...'", "relationship_name.invalid")]
    [InlineData("file:\n  - '...': owner", "relationship_name.invalid")]
    [InlineData("file:\n  - viewer: editor", "namespace.dangling_reference")] // the namespace gate runs on the parse path too
    [InlineData("file:\n  - viewer: (parent, member)", "namespace.dangling_reference")] // a factset's first element resolves here; its second does not
    [InlineData("file:\n  - viewer: viewer", "namespace.rewrite_cycle")]
    [InlineData("file:\n  - editor: viewer\n  - viewer: editor", "namespace.rewrite_cycle")]
    public void Parse_InvalidNamespaceMaps_FailsWithExpectedCode(string namespaceMap, string expectedCode)
    {
        var errors = ParseFailure(Document(namespaceMap));

        Assert.All(errors, error => Assert.Equal(ErrorType.Validation, error.Type));
        Assert.Contains(errors, error => error.Code == expectedCode);
    }

    [Theory]
    [InlineData("", "domain.document")]
    [InlineData("   ", "domain.document")]
    [InlineData("null", "domain.document")]
    [InlineData("scalar", "domain.document")]
    [InlineData("[]", "domain.document")]
    [InlineData("{}", "domain.document")] // neither key present
    [InlineData("domain: acme\n---\ndomain: other", "domain.document")] // a SDL document is a single YAML document
    [InlineData("namespaces:\n  file:\n    - owner", "domain.document")] // no 'domain:' key
    [InlineData("domain: acme", "domain.document")] // no 'namespaces:' key
    [InlineData("domain: acme\nnamespaces: 5", "domain.document")] // 'namespaces:' is not a mapping
    [InlineData("domain: acme\nnamespaces: []", "domain.document")]
    [InlineData("domain: [acme]\nnamespaces:\n  file:\n    - owner", "domain.document")] // 'domain:' is not a scalar
    [InlineData("domain:\nnamespaces:\n  file:\n    - owner", "domain_name.empty")] // a valueless 'domain:' loads as an empty scalar, which the identifier grammar rejects
    [InlineData("file:\n  - owner", "domain.document")] // the bare namespace map is no longer a document
    [InlineData("domain: ''\nnamespaces:\n  file:\n    - owner", "domain_name.empty")]
    [InlineData("domain: acme corp\nnamespaces:\n  file:\n    - owner", "domain_name.invalid")]
    [InlineData("domain: 123acme\nnamespaces:\n  file:\n    - owner", "domain_name.invalid")]
    [InlineData("domain: acme-corp\nnamespaces:\n  file:\n    - owner", "domain_name.invalid")]
    public void Parse_InvalidEnvelope_FailsWithExpectedCode(string sdl, string expectedCode)
    {
        var errors = ParseFailure(sdl);

        Assert.All(errors, error => Assert.Equal(ErrorType.Validation, error.Type));
        Assert.Contains(errors, error => error.Code == expectedCode);
    }

    [Fact]
    public void Parse_SpecName_IsTheSpecsDomainKey()
    {
        var domain = ParseSuccess(Document("file:\n  - owner", name: "acme"));

        Assert.Equal(SpecId("acme"), domain.Name);
    }

    [Fact]
    public void Parse_MixedCaseSpecName_NormalizesToLowercase()
    {
        var domain = ParseSuccess(Document("file:\n  - owner", name: "ACME"));

        Assert.Equal(SpecId("acme"), domain.Name);
    }

    [Fact]
    public void Parse_DefectsInNameAndNamespaces_AccumulateAcrossBoth()
    {
        // Result.Apply accumulates the envelope's two halves: a bad spec name does not mask namespace defects
        var errors = ParseFailure("domain: 123acme\nnamespaces:\n  123file:\n    - owner");

        Assert.Equal(2, errors.Length);
        Assert.Equal("domain_name.invalid", errors[0].Code);
        Assert.Equal("namespace_name.invalid", errors[1].Code);
    }

    [Fact]
    public void Parse_MissingRewriteExpression_NamesTheRelationship()
    {
        var errors = ParseFailure(Document("file:\n  - viewer:"));

        var error = Assert.Single(errors);
        Assert.Equal("domain.relationship", error.Code);
        Assert.Contains("'viewer'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_PlainNullExpressionText_IsTheNullIdentifier()
    {
        // SDL owns the scalar's raw text, not YAML's typing: a plain 'null' value is a computed reference
        // to a relationship named null — which is also what lets that name survive a round trip, since the
        // renderer emits it unquoted (SpecPrinter.Print)
        var ns = Assert.Single(ParseSuccess(Document("file:\n  - null\n  - viewer: null")).Namespaces);

        ImmutableArray<Relationship> expected = [Bare("null"), new Relationship(Rel("viewer"), Computed("null"))];
        Assert.Equal(expected, ns.Relationships);
    }

    [Fact]
    public void Parse_DefectsInOneRelationshipPair_AccumulateAcrossNameAndExpression()
    {
        // Result.Apply accumulates both sides of a single '<name>: <expression>' pair, and the namespace name on top
        var errors = ParseFailure(Document("123file:\n  - 456bad: this |"));

        Assert.Equal(3, errors.Length);
        Assert.Equal("namespace_name.invalid", errors[0].Code);
        Assert.Equal("relationship_name.invalid", errors[1].Code);
        Assert.Equal("domain.rewrite", errors[2].Code);
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
        Assert.Equal("namespace_name.invalid", errors[0].Code);
        Assert.Equal("relationship_name.invalid", errors[1].Code);
        Assert.Equal("domain.rewrite", errors[2].Code);
    }

    [Fact]
    public void Parse_CaseVariantNamespaceKeys_FailsAsDuplicate()
    {
        // distinct YAML keys, one namespace identity after Parse's lowercase normalization
        var errors = ParseFailure(Document("file:\n  - owner\nFILE:\n  - viewer"));

        var error = Assert.Single(errors);
        Assert.Equal("domain.duplicate_namespace", error.Code);
        Assert.Contains("'file'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_DuplicateRelationshipNames_FailsThroughDefine()
    {
        var errors = ParseFailure(Document("file:\n  - owner\n  - owner"));

        var error = Assert.Single(errors);
        Assert.Equal("namespace.duplicate_relationship", error.Code);
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
                    new Relationship(
                        Rel("editor"),
                        Union([SubjectSetRewrite.This.Default, Computed("owner")])),
                ]),
        ];

        Assert.Equal(expected, ParseSuccess(Document(namespaceMap)).Namespaces);
    }

    [Fact]
    public void Parse_EmptyNamespaceMap_FailsAsEmptySpec()
    {
        // a spec is never empty: the absence of namespaces is the absence of a spec
        var errors = ParseFailure(Document("{}"));

        Assert.Equal("domain.empty", Assert.Single(errors).Code);
    }

    [Theory]
    [InlineData("file:")]
    [InlineData("file: null")]
    [InlineData("file: Null")]
    [InlineData("file: NULL")]
    [InlineData("file: ~")]
    [InlineData("file: []")]
    public void Parse_NamespaceWithoutRelationships_ReturnsEmptyRelationships(string namespaceMap)
    {
        var ns = Assert.Single(ParseSuccess(Document(namespaceMap)).Namespaces);

        Assert.Equal(Ns("file"), ns.Name);
        Assert.Empty(ns.Relationships);
    }
}
