using Results;
using System.Collections.Immutable;

namespace Kingo.Schemas;

/// <summary>
/// A namespace's definition <b>as a value</b> — an immutable snapshot of its relationships and their rewrites,
/// with structural equality. Parse-agnostic and storable. An entity within the <see cref="Schema"/> aggregate,
/// not a root: its identity is local — <see cref="Name"/> unique (case-insensitive) within its schema, immutable
/// (there is no rename, only a new namespace). <see cref="Create"/> is the only construction path, so a
/// <c>Namespace</c> that exists satisfies its invariants. Entity-ness (versioning, lifecycle, optimistic
/// concurrency, authorship) is the Write/PAP context's wrapper and never lives in core: if this type ever grows
/// a version field, a timestamp, or a mutation method, it has crossed the line and belongs to a service
/// (docs/notes/domain-language.md).
/// </summary>
public sealed record Namespace
{
    public NamespaceIdentifier Name { get; }

    public ImmutableArray<Relationship> Relationships { get; }

    private Namespace(NamespaceIdentifier name, ImmutableArray<Relationship> relationships)
    {
        Name = name;
        Relationships = relationships;
    }

    /// <summary>
    /// The only construction path — validating construction for untrusted and trusted callers alike: rejects duplicate relationship names, accumulating one
    /// <see cref="ErrorType.Validation"/> error per duplicated name in first-occurrence order. The schema model has no core <c>Parse</c> — its text forms live
    /// in serialization adapters, which call this after decoding (docs/notes/domain-language.md).
    /// </summary>
    public static Result<Namespace> Create(NamespaceIdentifier name, ImmutableArray<Relationship> relationships)
    {
        var duplicates = relationships
            .GroupBy(relationship => relationship.Name)
            .Where(group => group.Count() > 1)
            .Select(group => Error.Validation(
                "namespace.duplicate_relationship",
                $"relationship '{group.Key}' is defined more than once in namespace '{name}'"))
            .ToImmutableArray();

        return duplicates.IsEmpty
            ? Result.Success(new Namespace(name, relationships))
            : Result.Failure<Namespace>(duplicates);
    }

    public bool Equals(Namespace? other) =>
        other is not null
        && Name.Equals(other.Name)
        && Relationships.AsSpan().SequenceEqual(other.Relationships.AsSpan());

    public override int GetHashCode() => HashCode.Combine(Name, RewriteHash.OfSequence(Relationships));
}
