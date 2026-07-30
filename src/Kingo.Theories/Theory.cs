using Results;
using System.Collections.Immutable;

namespace Kingo.Theories;

/// <summary>
/// A domain <b>as a value</b>: a set of namespace definitions curated together under a name, immutable with structural equality. The config-side aggregate root,
/// with <see cref="Namespace"/> now an entity within it. Namespace-name uniqueness is an intra-aggregate invariant, and the domain is the unit of atomic config
/// change. <see cref="Create"/> is the only construction path, so a <c>Theory</c> that exists satisfies its invariants. The root of the config tree: it owns its
/// namespaces, so it supplies their qualification, and nothing beneath it carries a qualified path ([[split-identities-at-ownership-boundaries]]).
/// </summary>
public sealed record Theory
{
    /// <summary>The domain's key, name-as-identity (provisional; see <see cref="TheoryName"/>).</summary>
    public TheoryName Name { get; }

    public ImmutableArray<Namespace> Namespaces { get; }

    private Theory(TheoryName name, ImmutableArray<Namespace> namespaces) =>
        (Name, Namespaces) = (name, namespaces);

    /// <summary>
    /// Constructs a domain from its name and namespaces, validating for untrusted and trusted callers alike. <paramref name="name"/> arrives already valid, because
    /// <see cref="TheoryName.Parse"/> owns its grammar. The only construction path.
    /// </summary>
    /// <returns>
    /// A successful <see cref="Result{T}"/> carrying the domain. Otherwise a failure when the namespace set is empty (<c>theory.empty</c>: a domain is never empty, and
    /// the absence of namespaces is the absence of a domain, modeled as not having one), or on duplicate namespace names (<c>theory.duplicate_namespace</c>, one
    /// <see cref="ErrorType.Validation"/> error per duplicated name in first-occurrence order; names are already case-normalized by <see cref="NamespaceName"/>).
    /// </returns>
    public static Result<Theory> Create(TheoryName name, ImmutableArray<Namespace> namespaces)
    {
        if (namespaces.IsDefaultOrEmpty)
            return Result.Failure<Theory>(
                Error.Validation(Diagnostics.ErrorCodes.Theory.Empty, ErrorMessage.Unchecked("a domain requires at least one namespace; the absence of namespaces is the absence of a domain")));

        var duplicates = namespaces
            .GroupBy(ns => ns.Name)
            .Where(group => group.Count() > 1)
            .Select(group => Error.Validation(
                Diagnostics.ErrorCodes.Theory.DuplicateNamespace,
                ErrorMessage.Unchecked($"namespace '{group.Key}' is defined more than once in the domain")))
            .ToImmutableArray();

        return duplicates.IsEmpty
            ? Result.Success(new Theory(name, namespaces))
            : Result.Failure<Theory>(duplicates);
    }

    public bool Equals(Theory? other) =>
        other is not null
        && Name == other.Name
        && Namespaces.AsSpan().SequenceEqual(other.Namespaces.AsSpan());

    public override int GetHashCode() => HashCode.Combine(Name, SequenceHash.Of(Namespaces));
}
