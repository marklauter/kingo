using Kingo.Namespaces;
using Kingo.Storage;
using Kingo.Storage.Keys;
using System.Runtime.CompilerServices;

namespace Kingo.Acl;

public sealed class AclReader(DocumentStore store)
{
    private readonly RewriteReader nsReader = new(store);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsAMemberOf(Subject subject, SubjectSet subjectSet) =>
        nsReader
            .Read(subjectSet.Resource.Namespace, subjectSet.Relationship)
            .Match(
                Some: rewrite => EvaluateRewrite(subject, subjectSet, rewrite),
                None: () => false);

    private bool EvaluateRewrite(Subject subject, SubjectSet subjectSet, SubjectSetRewrite node)
        => node switch
        {
            This => store.Find<Subject>(subjectSet.AsKey(), subject.AsKey()).IsSome,

            ComputedSubjectSetRewrite computedSet =>
                IsAMemberOf(subject, new SubjectSet(subjectSet.Resource, computedSet.Relationship)),

            UnionRewrite union =>
                union.Children.Any(child => EvaluateRewrite(subject, subjectSet, child)),

            IntersectionRewrite intersection =>
                intersection.Children.All(child => EvaluateRewrite(subject, subjectSet, child)),

            ExclusionRewrite exclusion =>
                EvaluateRewrite(subject, subjectSet, exclusion.Include)
            && !EvaluateRewrite(subject, subjectSet, exclusion.Exclude),

            TupleToSubjectSetRewrite tupleToSubjectSet =>
                store.Find<SubjectSet>(
                    subjectSet.Resource.AsKey(tupleToSubjectSet.TuplesetRelation),
                    KeyRange.Unbound)
                    .Any(parentSubjectSet =>
                        IsAMemberOf(
                            subject,
                            new SubjectSet(parentSubjectSet.Record.Resource, tupleToSubjectSet.ComputedSubjectSetRelation))),

            _ => throw new NotSupportedException($"node type {node.GetType()} no supported")
        };
}
