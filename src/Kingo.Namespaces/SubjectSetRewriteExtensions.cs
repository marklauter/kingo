using Kingo.Namespaces.Serializable;
using Kingo.Storage;
using Kingo.Storage.Keys;
using LanguageExt;

namespace Kingo.Namespaces;

public static class SubjectSetRewriteExtensions
{
    public static SubjectSetRewrite TransformRewrite(this Serializable.SubjectSetRewrite rewrite) =>
        rewrite switch
        {
            Serializable.This => This.Default,
            Serializable.ComputedSubjectSetRewrite computedSet => ComputedSubjectSetRewrite.From(computedSet.Relationship),
            Serializable.UnionRewrite union => UnionRewrite.From(union.Children.Select(TransformRewrite)),
            Serializable.IntersectionRewrite intersection => IntersectionRewrite.From(intersection.Children.Select(TransformRewrite)),
            Serializable.ExclusionRewrite exclusion => ExclusionRewrite.From(TransformRewrite(exclusion.Include), TransformRewrite(exclusion.Exclude)),
            Serializable.TupleToSubjectSetRewrite tupleToSubjectSet => TupleToSubjectSetRewrite.From(tupleToSubjectSet.TuplesetRelation, tupleToSubjectSet.ComputedSubjectSetRelation),
            _ => throw new NotSupportedException()
        };

    internal static Seq<Document<Key, Key, SubjectSetRewrite>> TransformRewrite(this NamespaceSpec spec) =>
        spec.Relationships.TransformRewrite($"{nameof(Namespace)}/{spec.Name}");

    private static Seq<Document<Key, Key, SubjectSetRewrite>> TransformRewrite(
        this IReadOnlyList<RelationshipSpec> relationships,
        Key namespaceHk) =>
        Seq.createRange(relationships.Select(r => r.ToDocument(namespaceHk)));

    private static Document<Key, Key, SubjectSetRewrite> ToDocument(this RelationshipSpec relationship, Key namespaceHk) =>
        Document.Cons(
            namespaceHk,
            Key.From(relationship.Name),
            relationship.SubjectSetRewrite.TransformRewrite());
}

