using Results;
using System.Collections.Immutable;

namespace Kingo.Policies;

/// <summary>
/// A policy <b>as a value</b> — a set of namespace definitions curated together, immutable with structural equality. The config-side aggregate root, replacing <see cref="Namespace"/> (now an entity within it): namespace-name uniqueness is an intra-aggregate invariant owned by <see cref="Define"/>, and the policy is the unit of atomic config change. Its domain key and how policy scope appears in references are still open (docs/notes/domain-language.md). A policy is never empty: the absence of namespaces is the absence of a policy, which callers model as not having one. Like the rest of the policy model it has no core <c>Parse</c> — its text forms (PDL, JSON) live in serialization adapters, which project a <c>Policy</c> from a successfully parsed document.
/// </summary>
public sealed record Policy(
    ImmutableArray<Namespace> Namespaces)
{
    /// <summary>
    /// Structured validating construction for untrusted input (documents, the Write API): rejects an empty namespace set (<c>policy.empty</c>) and duplicate namespace names (<c>policy.duplicate_namespace</c>, one <see cref="ErrorType.Validation"/> error per duplicated name in first-occurrence order — names are already case-normalized by <see cref="NamespaceIdentifier"/>). The constructor is pure assignment for trusted sources (misuse is the caller's defect), mirroring the <c>Create</c>/<c>Parse</c> split on the value types.
    /// </summary>
    public static Result<Policy> Define(ImmutableArray<Namespace> namespaces)
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
