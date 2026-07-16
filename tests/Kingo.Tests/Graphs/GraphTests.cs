using Kingo.Graphs;
using Results;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Kingo.Tests.Graphs;

public sealed class GraphTests
{
    private static Fact MakeFact(string text) =>
        Assert.IsType<Result<Fact>.Success>(Fact.Parse(text)).Value;

    private static Graph Make(ImmutableArray<Fact> facts) =>
        Assert.IsType<Result<Graph>.Success>(Graph.Create(facts)).Value;

    [Fact]
    public void Equals_ElementWiseEqualFacts_AreEqualWithMatchingHashCodes()
    {
        // Separately-constructed ImmutableArray instances with element-wise-equal contents.
        // Default record equality over ImmutableArray compares references and would fail this.
        ImmutableArray<Fact> left = [MakeFact("doc:readme#viewer@user:anne"), MakeFact("doc:readme#owner@user:bob")];
        ImmutableArray<Fact> right = [MakeFact("doc:readme#viewer@user:anne"), MakeFact("doc:readme#owner@user:bob")];

        var a = Make(left);
        var b = Make(right);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentFacts_NotEqual()
    {
        var a = Make([MakeFact("doc:readme#viewer@user:anne")]);
        var b = Make([MakeFact("doc:readme#viewer@user:bob")]);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equals_SameFactsDifferentOrder_NotEqual()
    {
        var a = Make([MakeFact("doc:readme#viewer@user:anne"), MakeFact("doc:readme#owner@user:bob")]);
        var b = Make([MakeFact("doc:readme#owner@user:bob"), MakeFact("doc:readme#viewer@user:anne")]);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equals_DifferentLengthsPrefix_NotEqual()
    {
        var a = Make([MakeFact("doc:readme#viewer@user:anne"), MakeFact("doc:readme#owner@user:bob")]);
        var b = Make([MakeFact("doc:readme#viewer@user:anne")]);

        Assert.NotEqual(a, b);
    }

    [Fact]
    [SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "always-false is the behavior under test: pins the null branch of the hand-written Equals")]
    public void Equals_Null_IsFalse()
    {
        var a = Make([MakeFact("doc:readme#viewer@user:anne")]);

        Assert.False(a.Equals(null));
    }

    [Fact]
    public void With_NoChanges_ProducesEqualValue()
    {
        var a = Make([MakeFact("doc:readme#viewer@user:anne")]);
        var b = a with { };

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Create_UniqueFacts_ReturnsSuccessPreservingOrder()
    {
        ImmutableArray<Fact> facts = [MakeFact("doc:readme#viewer@user:anne"), MakeFact("doc:readme#owner@user:bob")];

        var success = Assert.IsType<Result<Graph>.Success>(Graph.Create(facts));

        Assert.Equal(facts, success.Value.Facts);
    }

    [Fact]
    public void Create_NoFacts_ReturnsSuccess()
    {
        // unlike Schema, an empty graph is a legal state: nothing has been asserted yet
        var success = Assert.IsType<Result<Graph>.Success>(Graph.Create([]));

        Assert.Empty(success.Value.Facts);
    }

    [Fact]
    public void Create_DefaultArray_NormalizesToEmptyGraph()
    {
        var success = Assert.IsType<Result<Graph>.Success>(Graph.Create(default));

        Assert.Empty(success.Value.Facts);
        Assert.Equal(Make([]), success.Value);
    }

    [Fact]
    public void Create_DuplicateFact_ReturnsValidationFailure()
    {
        var failure = Assert.IsType<Result<Graph>.Failure>(
            Graph.Create([MakeFact("doc:readme#viewer@user:anne"), MakeFact("doc:readme#viewer@user:anne")]));

        var error = Assert.Single(failure.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("graph.duplicate_fact", error.Code);
    }

    [Fact]
    public void Create_MultipleDuplicateFacts_ReturnsOneErrorPerDuplicate()
    {
        var failure = Assert.IsType<Result<Graph>.Failure>(
            Graph.Create(
            [
                MakeFact("doc:readme#viewer@user:anne"),
                MakeFact("doc:readme#viewer@user:anne"),
                MakeFact("doc:readme#owner@user:bob"),
                MakeFact("doc:readme#owner@user:bob"),
            ]));

        Assert.Equal(2, failure.Errors.Length);
        Assert.All(failure.Errors, error => Assert.Equal("graph.duplicate_fact", error.Code));
    }
}
