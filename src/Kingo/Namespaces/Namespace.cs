using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Kingo.Namespaces;

/// <summary>
/// A namespace's policy definition <b>as a value</b> — an immutable snapshot of its relationship definitions and their rewrites, with structural equality. Parse-agnostic and storable. An aggregate root; its domain key is <see cref="Name"/> (unique, case-insensitive, immutable — there is no rename, only a new namespace). Entity-ness (versioning, lifecycle, optimistic concurrency, authorship) is the Write/PAP context's wrapper and never lives in core: if this type ever grows a version field, a timestamp, or a mutation method, it has crossed the line and belongs to a service (docs/notes/domain-language.md).
/// </summary>
[SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "the domain word is 'namespace'")]
public sealed record Namespace(NamespaceIdentifier Name, ImmutableArray<RelationshipDefinition> Definitions)
{
    public bool Equals(Namespace? other) =>
        other is not null
        && Name.Equals(other.Name)
        && Definitions.AsSpan().SequenceEqual(other.Definitions.AsSpan());

    public override int GetHashCode() => HashCode.Combine(Name, RewriteHash.OfSequence(Definitions));
}

/// <summary>
/// A named relationship definition and the rewrite that computes its effective subject set. Policy-side: this is the definition inside a <see cref="Namespace"/>, not the stored fact (that is <c>Kingo.Relationships.Relationship</c>). A bare definition (no rewrite specified) defaults to <see cref="ThisRewrite"/> — direct membership only.
/// </summary>
public sealed record RelationshipDefinition(RelationshipIdentifier Name, SubjectSetRewrite Rewrite)
{
    /// <summary>Constructs a definition with the implicit <see cref="ThisRewrite"/> — direct membership only.</summary>
    public RelationshipDefinition(RelationshipIdentifier name)
        : this(name, ThisRewrite.Default) { }
}
