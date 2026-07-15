using Results;
using System.Collections.Immutable;

namespace Kingo.Schemas;

/// <summary>
/// A schema <b>as a value</b> — a set of namespace definitions curated together, immutable with structural
/// equality. The config-side aggregate root, replacing <see cref="Namespace"/> (now an entity within it):
/// namespace-name uniqueness is an intra-aggregate invariant, and the schema is the unit of atomic config
/// change. <see cref="Create"/> is the only construction path, so a <c>Schema</c> that exists satisfies its
/// invariants. Its domain key and how schema scope appears in references are still open
/// (docs/notes/domain-language.md).
/// </summary>
public sealed record Schema
{
    public ImmutableArray<Namespace> Namespaces { get; }

    private Schema(ImmutableArray<Namespace> namespaces) => Namespaces = namespaces;

    /// <summary>
    /// The only construction path — validating construction for untrusted and trusted callers alike: rejects an empty namespace set (<c>schema.empty</c> — a
    /// schema is never empty; the absence of namespaces is the absence of a schema, modeled as not having one) and duplicate namespace names
    /// (<c>schema.duplicate_namespace</c>, one <see cref="ErrorType.Validation"/> error per duplicated name in first-occurrence order — names are already
    /// case-normalized by <see cref="NamespaceIdentifier"/>).
    /// </summary>
    public static Result<Schema> Create(ImmutableArray<Namespace> namespaces)
    {
        if (namespaces.IsDefaultOrEmpty)
            return Result.Failure<Schema>(
                Error.Validation("schema.empty", "a schema requires at least one namespace; the absence of namespaces is the absence of a schema"));

        var duplicates = namespaces
            .GroupBy(ns => ns.Name)
            .Where(group => group.Count() > 1)
            .Select(group => Error.Validation(
                "schema.duplicate_namespace",
                $"namespace '{group.Key}' is defined more than once in the schema"))
            .ToImmutableArray();

        return duplicates.IsEmpty
            ? Result.Success(new Schema(namespaces))
            : Result.Failure<Schema>(duplicates);
    }

    public bool Equals(Schema? other) =>
        other is not null
        && Namespaces.AsSpan().SequenceEqual(other.Namespaces.AsSpan());

    public override int GetHashCode() => RewriteHash.OfSequence(Namespaces);
}
