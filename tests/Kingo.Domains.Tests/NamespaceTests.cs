using Results;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using static Kingo.Domains.Tests.TestHelpers;

namespace Kingo.Domains.Tests;

public sealed class NamespaceTests
{
    // a namespace name is bare: the domain that owns it supplies the qualification ([[identifiers]])
    private static NamespaceName Ns(string name) => NamespaceName.Unchecked(name);

    private static Relationship Def(string name) => new(RelationshipName.Unchecked(name));

    private static Relationship Def(string name, SubjectSetRewrite rewrite) => new(RelationshipName.Unchecked(name), rewrite);

    private static Namespace Make(NamespaceName name, ImmutableArray<Relationship> relationships) =>
        Assert.IsType<Result<Namespace>.Success>(Namespace.Create(name, relationships)).Value;

    [Fact]
    public void Equals_SameNameAndElementWiseEqualRelationships_AreEqualWithMatchingHashCodes()
    {
        // Separately-constructed ImmutableArray instances with element-wise-equal contents.
        // Default record equality over ImmutableArray compares references and would fail this.
        ImmutableArray<Relationship> left = [Def("viewer"), Def("editor")];
        ImmutableArray<Relationship> right = [Def("viewer"), Def("editor")];

        var a = Make(Ns("doc"), left);
        var b = Make(Ns("doc"), right);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Create_NameAndRelationships_AreCarriedOntoTheNamespace()
    {
        ImmutableArray<Relationship> relationships = [Def("viewer"), Def("editor")];

        var ns = Make(Ns("doc"), relationships);

        Assert.Equal(Ns("doc"), ns.Name);
        Assert.Equal(relationships, ns.Relationships);
    }

    [Fact]
    public void Equals_DifferentName_NotEqual()
    {
        var a = Make(Ns("doc"), [Def("viewer")]);
        var b = Make(Ns("folder"), [Def("viewer")]);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equals_SameNameDifferentRelationships_NotEqual()
    {
        var a = Make(Ns("doc"), [Def("viewer")]);
        var b = Make(Ns("doc"), [Def("editor")]);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equals_SameRelationshipsDifferentOrder_NotEqual()
    {
        var a = Make(Ns("doc"), [Def("viewer"), Def("editor")]);
        var b = Make(Ns("doc"), [Def("editor"), Def("viewer")]);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equals_DifferentLengthsPrefix_NotEqual()
    {
        var a = Make(Ns("doc"), [Def("viewer"), Def("editor")]);
        var b = Make(Ns("doc"), [Def("viewer")]);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equals_BothEmptyRelationships_AreEqual()
    {
        ImmutableArray<Relationship> left = [];
        ImmutableArray<Relationship> right = [];

        var a = Make(Ns("doc"), left);
        var b = Make(Ns("doc"), right);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    [SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "always-false is the behavior under test: pins the null branch of the hand-written Equals")]
    public void Equals_Null_IsFalse()
    {
        var a = Make(Ns("doc"), [Def("viewer")]);

        Assert.False(a.Equals(null));
    }

    [Fact]
    public void With_NoChanges_ProducesEqualValue()
    {
        var a = Make(Ns("doc"), [Def("viewer")]);

        var b = a with { };

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void OperatorEquals_IsConsistentWithEquals()
    {
        var a = Make(Ns("doc"), [Def("viewer"), Def("editor")]);
        var b = Make(Ns("doc"), [Def("viewer"), Def("editor")]);
        var c = Make(Ns("doc"), [Def("viewer")]);

        Assert.True(a == b);
        Assert.False(a != b);
        Assert.False(a == c);
        Assert.True(a != c);
    }

    [Fact]
    public void Create_UniqueRelationshipNames_ReturnsSuccessEqualToConstructed()
    {
        var result = Namespace.Create(Ns("doc"), [Def("viewer"), Def("editor")]);

        var success = Assert.IsType<Result<Namespace>.Success>(result);
        Assert.Equal(Make(Ns("doc"), [Def("viewer"), Def("editor")]), success.Value);
    }

    [Fact]
    public void Create_EmptyRelationships_ReturnsSuccess()
    {
        var result = Namespace.Create(Ns("doc"), []);

        var success = Assert.IsType<Result<Namespace>.Success>(result);
        Assert.Empty(success.Value.Relationships);
    }

    [Fact]
    public void Create_DefaultRelationships_NormalizesToEmpty()
    {
        // a default (uninitialized) array is the empty namespace, not an unmodeled crash:
        // construction is total, and the stored value always enumerates
        var result = Namespace.Create(Ns("doc"), default);

        var success = Assert.IsType<Result<Namespace>.Success>(result);
        Assert.False(success.Value.Relationships.IsDefault);
        Assert.Empty(success.Value.Relationships);
    }

    [Fact]
    public void Create_DuplicateRelationshipName_ReturnsValidationFailure()
    {
        var result = Namespace.Create(Ns("doc"), [Def("viewer"), Def("editor"), Def("viewer")]);

        var failure = Assert.IsType<Result<Namespace>.Failure>(result);
        var error = Assert.Single(failure.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("namespace.duplicate_relationship", error.Code);
        Assert.Contains("'viewer'", error.Message, StringComparison.Ordinal);
        Assert.Contains("'doc'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_SameNameDifferentRewrites_IsStillADuplicate()
    {
        // Uniqueness is by Name alone — two definitions for the same relationship are
        // the conflict, regardless of whether their rewrites agree.
        var viewerDirect = Def("viewer");
        var viewerComputed = Def("viewer", Computed("editor"));

        var result = Namespace.Create(Ns("doc"), [viewerDirect, viewerComputed]);

        var failure = Assert.IsType<Result<Namespace>.Failure>(result);
        var error = Assert.Single(failure.Errors);
        Assert.Equal("namespace.duplicate_relationship", error.Code);
    }

    [Fact]
    public void Create_MultipleDuplicatedNames_AccumulatesOneErrorPerNameInFirstOccurrenceOrder()
    {
        var result = Namespace.Create(
            Ns("doc"),
            [Def("viewer"), Def("editor"), Def("viewer"), Def("editor")]);

        var failure = Assert.IsType<Result<Namespace>.Failure>(result);
        Assert.Equal(2, failure.Errors.Length);
        Assert.All(failure.Errors, error => Assert.Equal("namespace.duplicate_relationship", error.Code));
        Assert.Contains("'viewer'", failure.Errors[0].Message, StringComparison.Ordinal);
        Assert.Contains("'editor'", failure.Errors[1].Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_NamesDifferingOnlyByCase_AreDistinct()
    {
        // Uniqueness is ordinal over canonical values. Parsed relationship names are
        // always lowercase; mixed case here is only reachable through the trusted
        // Create path, and Create compares what it is given.
        var result = Namespace.Create(Ns("doc"), [Def("viewer"), Def("Viewer")]);

        _ = Assert.IsType<Result<Namespace>.Success>(result);
    }

    // ---- Dangling references ----

    [Fact]
    public void Create_DanglingComputedReference_ReturnsValidationFailure()
    {
        var result = Namespace.Create(Ns("doc"), [Def("viewer", Computed("editor"))]);

        var failure = Assert.IsType<Result<Namespace>.Failure>(result);
        var error = Assert.Single(failure.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("namespace.dangling_reference", error.Code);
        Assert.Contains("'viewer'", error.Message, StringComparison.Ordinal);
        Assert.Contains("'editor'", error.Message, StringComparison.Ordinal);
        Assert.Contains("'doc'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_DanglingFactsetReference_ReturnsValidationFailure()
    {
        var result = Namespace.Create(Ns("doc"), [Def("viewer", FactTo("parent", "viewer"))]);

        var failure = Assert.IsType<Result<Namespace>.Failure>(result);
        var error = Assert.Single(failure.Errors);
        Assert.Equal("namespace.dangling_reference", error.Code);
        Assert.Contains("'parent'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_FactsetComputedSubjectSetRelationship_IsNotValidated()
    {
        // the factset's second element resolves in another namespace, unknown until facts resolve
        // the factset's resources — it stays the interpreter's condition 4, not a construction check
        var result = Namespace.Create(Ns("doc"), [Def("parent"), Def("viewer", FactTo("parent", "missing"))]);

        _ = Assert.IsType<Result<Namespace>.Success>(result);
    }

    [Fact]
    public void Create_DanglingReferences_AccumulateThroughNestedOperators()
    {
        var rewrite = Union(
        [
            Computed("a"),
            Intersection([Computed("b"), Exclusion(SubjectSetRewrite.This.Default, Computed("c"))]),
        ]);

        var result = Namespace.Create(Ns("doc"), [Def("viewer", rewrite)]);

        var failure = Assert.IsType<Result<Namespace>.Failure>(result);
        Assert.Equal(3, failure.Errors.Length);
        Assert.All(failure.Errors, error => Assert.Equal("namespace.dangling_reference", error.Code));
        Assert.Contains("'a'", failure.Errors[0].Message, StringComparison.Ordinal);
        Assert.Contains("'b'", failure.Errors[1].Message, StringComparison.Ordinal);
        Assert.Contains("'c'", failure.Errors[2].Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_RepeatedReferenceToOneMissingTarget_ReportsOnce()
    {
        var result = Namespace.Create(
            Ns("doc"),
            [Def("viewer", Union([Computed("missing"), Computed("missing")]))]);

        var failure = Assert.IsType<Result<Namespace>.Failure>(result);
        _ = Assert.Single(failure.Errors);
    }

    [Fact]
    public void Create_ResolvedReferences_Succeed()
    {
        var result = Namespace.Create(
            Ns("doc"),
            [
                Def("owner"),
                Def("parent"),
                Def("editor", Union([SubjectSetRewrite.This.Default, Computed("owner")])),
                Def("viewer", Exclusion(Union([SubjectSetRewrite.This.Default, Computed("editor"), FactTo("parent", "viewer")]), Computed("owner"))),
            ]);

        _ = Assert.IsType<Result<Namespace>.Success>(result);
    }

    // ---- Staging ----

    [Fact]
    public void Create_DuplicateNames_MaskDanglingAndCycleChecks()
    {
        // duplicates make reference resolution ambiguous, so later stages never run
        var result = Namespace.Create(
            Ns("doc"),
            [Def("viewer", Computed("missing")), Def("viewer", Computed("viewer"))]);

        var failure = Assert.IsType<Result<Namespace>.Failure>(result);
        Assert.All(failure.Errors, error => Assert.Equal("namespace.duplicate_relationship", error.Code));
    }

    [Fact]
    public void Create_DanglingReferences_MaskCycleCheck()
    {
        // dangling references make the cycle graph ill-defined, so the cycle stage never runs
        var result = Namespace.Create(
            Ns("doc"),
            [Def("a", Computed("a")), Def("b", Computed("missing"))]);

        var failure = Assert.IsType<Result<Namespace>.Failure>(result);
        Assert.All(failure.Errors, error => Assert.Equal("namespace.dangling_reference", error.Code));
    }

    // ---- Cycles ----

    [Fact]
    public void Create_SelfReference_ReturnsCycleFailure()
    {
        var result = Namespace.Create(Ns("doc"), [Def("viewer", Computed("viewer"))]);

        var failure = Assert.IsType<Result<Namespace>.Failure>(result);
        var error = Assert.Single(failure.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("namespace.rewrite_cycle", error.Code);
        Assert.Contains("'viewer' -> 'viewer'", error.Message, StringComparison.Ordinal);
        Assert.Contains("'doc'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_CycleError_CarriesTheFullCyclePath()
    {
        var result = Namespace.Create(
            Ns("doc"),
            [Def("a", Computed("b")), Def("b", Computed("c")), Def("c", Computed("a"))]);

        var failure = Assert.IsType<Result<Namespace>.Failure>(result);
        var error = Assert.Single(failure.Errors);
        Assert.Equal("namespace.rewrite_cycle", error.Code);
        Assert.Contains("'a' -> 'b' -> 'c' -> 'a'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_CycleThroughNestedOperators_IsDetected()
    {
        var result = Namespace.Create(
            Ns("doc"),
            [
                Def("a", Union([SubjectSetRewrite.This.Default, Exclusion(SubjectSetRewrite.This.Default, Computed("b"))])),
                Def("b", Computed("a")),
            ]);

        var failure = Assert.IsType<Result<Namespace>.Failure>(result);
        Assert.Contains(failure.Errors, error => error.Code == "namespace.rewrite_cycle");
    }

    [Fact]
    public void Create_MultipleDisjointCycles_AccumulateOneErrorEach()
    {
        var result = Namespace.Create(
            Ns("doc"),
            [Def("a", Computed("a")), Def("b", Computed("c")), Def("c", Computed("b"))]);

        var failure = Assert.IsType<Result<Namespace>.Failure>(result);
        Assert.Equal(2, failure.Errors.Length);
        Assert.All(failure.Errors, error => Assert.Equal("namespace.rewrite_cycle", error.Code));
    }

    [Fact]
    public void Create_FactsetArm_IsNotACycleEdge()
    {
        // a factset walk cannot recurse without consuming a stored fact: it belongs to the
        // evaluator's depth bound, not the zero-fact recursion graph
        var result = Namespace.Create(Ns("doc"), [Def("viewer", FactTo("viewer", "viewer"))]);

        _ = Assert.IsType<Result<Namespace>.Success>(result);
    }

    // ---- Stack depth ----

    [Fact]
    public void Create_LongAcyclicReferenceChain_DoesNotOverflowTheStack()
    {
        // untrusted input must not pick the validation gate's stack depth: a flat chain
        // r0 -> r1 -> ... -> rN is linear in relationships, not in expression nesting,
        // so it reaches Create without stressing any parser first
        const int depth = 20_000;
        ImmutableArray<Relationship> relationships =
        [
            .. Enumerable.Range(0, depth - 1).Select(i => Def($"r{i}", Computed($"r{i + 1}"))),
            Def($"r{depth - 1}"),
        ];

        _ = Assert.IsType<Result<Namespace>.Success>(Namespace.Create(Ns("doc"), relationships));
    }

    [Fact]
    public void Create_OperatorTreeAtTheDepthBound_Validates()
    {
        // trees past MaxDepth are unrepresentable — the factories refuse them — so the deepest
        // constructible nest is the worst case the validation traversals can ever meet
        var rewrite = Enumerable.Range(0, SubjectSetRewrite.MaxDepth - 1)
            .Aggregate((SubjectSetRewrite)SubjectSetRewrite.This.Default, (accumulated, _) => Exclusion(accumulated, SubjectSetRewrite.This.Default));

        Assert.Equal(SubjectSetRewrite.MaxDepth, rewrite.Depth);
        _ = Assert.IsType<Result<Namespace>.Success>(Namespace.Create(Ns("doc"), [Def("viewer", rewrite)]));
    }

    [Fact]
    public void Create_DiamondReferences_AreNotACycle()
    {
        var result = Namespace.Create(
            Ns("doc"),
            [
                Def("a", Union([Computed("b"), Computed("c")])),
                Def("b", Computed("d")),
                Def("c", Computed("d")),
                Def("d"),
            ]);

        _ = Assert.IsType<Result<Namespace>.Success>(result);
    }
}
