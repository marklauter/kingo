using Results;
using System.Collections.Immutable;

namespace Kingo.Policies;

/// <summary>
/// A policy <b>as a value</b> — a set of namespace definitions curated together, immutable with structural
/// equality. The config-side aggregate root, replacing <see cref="Namespace"/> (now an entity within it):
/// namespace-name uniqueness is an intra-aggregate invariant, and the policy is the unit of atomic config
/// change. <see cref="Create"/> is the only construction path, so a <c>Policy</c> that exists satisfies its
/// invariants. Its domain key and how policy scope appears in references are still open
/// (docs/notes/domain-language.md).
/// </summary>
public sealed record Policy
{
    public ImmutableArray<Namespace> Namespaces { get; }

    private Policy(ImmutableArray<Namespace> namespaces) => Namespaces = namespaces;

    /// <summary>
    /// The only construction path — validating construction for untrusted and trusted callers alike: rejects an empty namespace set (<c>policy.empty</c> — a
    /// policy is never empty; the absence of namespaces is the absence of a policy, modeled as not having one) and duplicate namespace names
    /// (<c>policy.duplicate_namespace</c>, one <see cref="ErrorType.Validation"/> error per duplicated name in first-occurrence order — names are already
    /// case-normalized by <see cref="NamespaceIdentifier"/>).
    /// </summary>
    public static Result<Policy> Create(ImmutableArray<Namespace> namespaces)
    {
        if (namespaces.IsDefaultOrEmpty)
            return Result.Failure<Policy>(Error.Validation("policy.empty", "a policy requires at least one namespace; the absence of namespaces is the absence of a policy"));

        var duplicates = namespaces
            .GroupBy(ns => ns.Name)
            .Where(group => group.Count() > 1)
            .Select(group => Error.Validation(
                "policy.duplicate_namespace",
                $"namespace '{group.Key}' is defined more than once in the policy"))
            .ToImmutableArray();

        return duplicates.IsEmpty
            ? Result.Success(new Policy(namespaces))
            : Result.Failure<Policy>(duplicates);
    }

    public bool Equals(Policy? other) =>
        other is not null
        && Namespaces.AsSpan().SequenceEqual(other.Namespaces.AsSpan());

    public override int GetHashCode() => RewriteHash.OfSequence(Namespaces);
}
