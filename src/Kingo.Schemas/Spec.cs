using Results;
using System.Collections.Immutable;

namespace Kingo.Schemas;

/// <summary>
/// A spec <b>as a value</b>: a set of namespace definitions curated together under a name, immutable with structural equality. The config-side aggregate root,
/// with <see cref="Namespace"/> now an entity within it. Namespace-name uniqueness is an intra-aggregate invariant, and the spec is the unit of atomic config
/// change. <see cref="Create"/> is the only construction path, so a <c>Spec</c> that exists satisfies its invariants. The root of the config tree: it owns its
/// namespaces, so it supplies their qualification, and nothing beneath it carries a qualified path ([[split-identities-at-ownership-boundaries]]).
/// </summary>
public sealed record Spec
{
    /// <summary>The spec's domain key, name-as-identity (provisional; see <see cref="SpecName"/>).</summary>
    public SpecName Name { get; }

    public ImmutableArray<Namespace> Namespaces { get; }

    private Spec(SpecName name, ImmutableArray<Namespace> namespaces) =>
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
    public static Result<Spec> Create(SpecName name, ImmutableArray<Namespace> namespaces)
    {
        if (namespaces.IsDefaultOrEmpty)
            return Result.Failure<Spec>(
                Error.Validation("spec.empty", "a spec requires at least one namespace; the absence of namespaces is the absence of a spec"));

        var duplicates = namespaces
            .GroupBy(ns => ns.Name)
            .Where(group => group.Count() > 1)
            .Select(group => Error.Validation(
                "spec.duplicate_namespace",
                $"namespace '{group.Key}' is defined more than once in the spec"))
            .ToImmutableArray();

        return duplicates.IsEmpty
            ? Result.Success(new Spec(name, namespaces))
            : Result.Failure<Spec>(duplicates);
    }

    public bool Equals(Spec? other) =>
        other is not null
        && Name == other.Name
        && Namespaces.AsSpan().SequenceEqual(other.Namespaces.AsSpan());

    public override int GetHashCode() => HashCode.Combine(Name, SequenceHash.Of(Namespaces));
}
