namespace Kingo.Policies;

/// <summary>
/// A named relationship and the rewrite that computes its effective subject set. Policy-side: this is the definition inside a <see cref="Namespace"/>, not the stored fact (that is <c>Kingo.Statements.Statement</c>). A bare definition (no rewrite specified) defaults to <see cref="ThisRewrite"/> — direct membership only.
/// </summary>
public sealed record Relationship(
    RelationshipIdentifier Name,
    SubjectSetRewrite Rewrite)
{
    /// <summary>Constructs a definition with the implicit <see cref="ThisRewrite"/> — direct membership only.</summary>
    public Relationship(RelationshipIdentifier name)
        : this(name, ThisRewrite.Default) { }
}
