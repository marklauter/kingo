using Kingo.Policies;
using Results;
using System.Collections.Immutable;

namespace Kingo.Serialization.Pdl.Tests;

/// <summary>Shared construction and unwrap helpers for the PDL adapter tests — import with <c>using static</c>.</summary>
internal static class TestHelpers
{
    public static NamespaceIdentifier Ns(string value) => NamespaceIdentifier.Create(value);

    public static RelationshipIdentifier Rel(string value) => RelationshipIdentifier.Create(value);

    public static Relationship Bare(string name) => new(Rel(name));

    public static ComputedSubjectSetRewrite Computed(string name) => new(Rel(name));

    public static Namespace MakeNs(NamespaceIdentifier name, ImmutableArray<Relationship> relationships) =>
        Assert.IsType<Result<Namespace>.Success>(Namespace.Create(name, relationships)).Value;

    public static Policy ParseSuccess(string text) =>
        Assert.IsType<Result<Policy>.Success>(PdlSerializer.Parse(text)).Value;

    public static ImmutableArray<Error> ParseFailure(string text) =>
        Assert.IsType<Result<Policy>.Failure>(PdlSerializer.Parse(text)).Errors;
}
