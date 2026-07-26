namespace Kingo.Theories;

/// <summary>
/// A named rewrite. Theory-side: this is the definition inside a <see cref="Namespace"/>, not the
/// stored fact (that is <c>Kingo.Facts.Fact</c>). A bare definition with no rewrite specified defaults to <see cref="SubjectSetRewrite.This"/>, direct membership
/// only.
/// <para>
/// <see cref="Name"/> is bare, like every name in the theory tree. A relation exists only inside a <see cref="Namespace"/>, which exists only inside a
/// <see cref="Theory"/>, so containment already says which relation this is. A qualified path held here would be a second source of truth that could disagree
/// with its container. It also puts the definition's own name in the same currency as the names its rewrite
/// references, so <c>Namespace.Create</c> resolves them without qualifying either side.
/// </para>
/// </summary>
public sealed record Relation(
    RelationName Name,
    SubjectSetRewrite Rewrite)
{
    /// <summary>Constructs a definition with the implicit <see cref="SubjectSetRewrite.This"/>, direct membership only.</summary>
    public Relation(RelationName name)
        : this(name, SubjectSetRewrite.This.Default) { }
}
