namespace Kingo.Schemas;

/// <summary>
/// A named relationship and the rewrite that computes its effective subject set. Spec-side: this is the definition inside a <see cref="Namespace"/>, not the
/// stored fact (that is <c>Kingo.Graphs.Fact</c>). A bare definition (no rewrite specified) defaults to <see cref="ThisRewrite"/> — direct membership
/// only.
/// </summary>
public sealed record Relationship(
    RelationshipPath Name,
    SubjectSetRewrite Rewrite)
{
    /// <summary>Constructs a definition with the implicit <see cref="ThisRewrite"/> — direct membership only.</summary>
    public Relationship(RelationshipPath name)
        : this(name, ThisRewrite.Default) { }
}
