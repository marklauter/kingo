namespace Kingo.Theories;

public sealed record Relation(
    RelationName Name,
    SubjectSetRewrite Rewrite)
{
    public Relation(RelationName name)
        : this(name, SubjectSetRewrite.This.Default) { }
}
