using Results;
using System.Collections.Immutable;

namespace Kingo.Domains;

/// <summary>
/// A spec <b>as a value</b>: a set of namespace definitions curated together under a name, immutable with structural equality. The config-side aggregate root,
/// with <see cref="Namespace"/> now an entity within it. Namespace-name uniqueness is an intra-aggregate invariant, and the spec is the unit of atomic config
/// change. <see cref="Create"/> is the only construction path, so a <c>Domain</c> that exists satisfies its invariants. The root of the config tree: it owns its
/// namespaces, so it supplies their qualification, and nothing beneath it carries a qualified path ([[split-identities-at-ownership-boundaries]]).
/// </summary>
public sealed record Domain
{
    /// <summary>The spec's domain key, name-as-identity (provisional; see <see cref="SpecName"/>).</summary>
    public SpecName Name { get; }

    public ImmutableArray<Namespace> Namespaces { get; }

    private Domain(SpecName name, ImmutableArray<Namespace> namespaces) =>
        (Name, Namespaces) = (name, namespaces);

    /// <summary>
    /// Constructs a spec from its name and namespaces, validating for untrusted and trusted callers alike. <paramref name="name"/> arrives already valid, because
    /// <see cref="SpecName.Parse"/> owns its grammar. The only construction path.
    /// </summary>
    /// <returns>
    /// A successful <see cref="Result{T}"/> carrying the spec. Otherwise a failure when the namespace set is empty (<c>spec.empty</c>: a spec is never empty, and
    /// the absence of namespaces is the absence of a spec, modeled as not having one), or on duplicate namespace names (<c>spec.duplicate_namespace</c>, one
    /// <see cref="ErrorType.Validation"/> error per duplicated name in first-occurrence order; names are already case-normalized by <see cref="NamespaceName"/>).
    /// </returns>
    public static Result<Domain> Create(SpecName name, ImmutableArray<Namespace> namespaces)
    {
        if (namespaces.IsDefaultOrEmpty)
            return Result.Failure<Domain>(
                Error.Validation("spec.empty", "a spec requires at least one namespace; the absence of namespaces is the absence of a spec"));

        var duplicates = namespaces
            .GroupBy(ns => ns.Name)
            .Where(group => group.Count() > 1)
            .Select(group => Error.Validation(
                "spec.duplicate_namespace",
                $"namespace '{group.Key}' is defined more than once in the spec"))
            .ToImmutableArray();

        return duplicates.IsEmpty
            ? Result.Success(new Domain(name, namespaces))
            : Result.Failure<Domain>(duplicates);
    }

    public bool Equals(Domain? other) =>
        other is not null
        && Name == other.Name
        && Namespaces.AsSpan().SequenceEqual(other.Namespaces.AsSpan());

    public override int GetHashCode() => HashCode.Combine(Name, SequenceHash.Of(Namespaces));
}
