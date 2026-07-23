using Results;
using System.Collections.Immutable;

namespace Kingo.Schemas;

/// <summary>
/// A spec <b>as a value</b> — a set of namespace definitions curated together under a name, immutable with
/// structural equality. The config-side aggregate root, replacing <see cref="Namespace"/> (now an entity within it):
/// namespace-name uniqueness is an intra-aggregate invariant, and the spec is the unit of atomic config
/// change. <see cref="Create"/> is the only construction path, so a <c>Spec</c> that exists satisfies its
/// invariants. How spec scope appears in references is still open ([[domain-language]]).
/// </summary>
public sealed record Spec
{
    /// <summary>The spec's domain key — name-as-identity (provisional; see <see cref="SpecPath"/>).</summary>
    public SpecPath Name { get; }

    public ImmutableArray<Namespace> Namespaces { get; }

    private Spec(SpecPath name, ImmutableArray<Namespace> namespaces) =>
        (Name, Namespaces) = (name, namespaces);

    /// <summary>
    /// The only construction path — validating construction for untrusted and trusted callers alike: rejects an empty namespace set (<c>spec.empty</c> — a
    /// spec is never empty; the absence of namespaces is the absence of a spec, modeled as not having one) and duplicate namespace names
    /// (<c>spec.duplicate_namespace</c>, one <see cref="ErrorType.Validation"/> error per duplicated name in first-occurrence order — names are already
    /// case-normalized by <see cref="NamespacePath"/>). <paramref name="name"/> arrives already valid — <see cref="SpecPath.Parse"/> owns its grammar.
    /// </summary>
    public static Result<Spec> Create(SpecPath name, ImmutableArray<Namespace> namespaces)
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

    public override int GetHashCode() => HashCode.Combine(Name, RewriteHash.OfSequence(Namespaces));
}
