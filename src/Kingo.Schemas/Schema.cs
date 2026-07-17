using Results;
using System.Collections.Immutable;

namespace Kingo.Schemas;

/// <summary>
/// A schema <b>as a value</b> — a set of namespace definitions curated together under a name, immutable with
/// structural equality. The config-side aggregate root, replacing <see cref="Namespace"/> (now an entity within it):
/// namespace-name uniqueness is an intra-aggregate invariant, and the schema is the unit of atomic config
/// change. <see cref="Create"/> is the only construction path, so a <c>Schema</c> that exists satisfies its
/// invariants. How schema scope appears in references is still open ([[domain-language]]).
/// </summary>
public sealed record Schema
{
    /// <summary>The schema's domain key — name-as-identity (provisional; see <see cref="SchemaIdentifier"/>).</summary>
    public SchemaIdentifier Name { get; }

    public ImmutableArray<Namespace> Namespaces { get; }

    private Schema(SchemaIdentifier name, ImmutableArray<Namespace> namespaces) =>
        (Name, Namespaces) = (name, namespaces);

    /// <summary>
    /// The only construction path — validating construction for untrusted and trusted callers alike: rejects an empty namespace set (<c>schema.empty</c> — a
    /// schema is never empty; the absence of namespaces is the absence of a schema, modeled as not having one) and duplicate namespace names
    /// (<c>schema.duplicate_namespace</c>, one <see cref="ErrorType.Validation"/> error per duplicated name in first-occurrence order — names are already
    /// case-normalized by <see cref="NamespaceIdentifier"/>). <paramref name="name"/> arrives already valid — <see cref="SchemaIdentifier.Parse"/> owns its grammar.
    /// </summary>
    public static Result<Schema> Create(SchemaIdentifier name, ImmutableArray<Namespace> namespaces)
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
            ? Result.Success(new Schema(name, namespaces))
            : Result.Failure<Schema>(duplicates);
    }

    public bool Equals(Schema? other) =>
        other is not null
        && Name == other.Name
        && Namespaces.AsSpan().SequenceEqual(other.Namespaces.AsSpan());

    public override int GetHashCode() => HashCode.Combine(Name, RewriteHash.OfSequence(Namespaces));
}
