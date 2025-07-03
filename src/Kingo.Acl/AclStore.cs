using Kingo.Namespaces;
using Kingo.Storage;
using Kingo.Storage.Keys;
using LanguageExt;
using System.Runtime.CompilerServices;

namespace Kingo.Acl;

/// <summary>
/// This is a demo store. A production store would use DynamoDB, Casandra, or other versioned key-value store.
/// </summary>
public sealed class AclStore(DocumentStore documentStore)
{
    private readonly SubjectSetRewriteReader nsReader = new(documentStore);

    public enum AssociateResponse
    {
        Success,
        TimeoutError,
        VersionCheckFailedError,
    }

    // todo: instead of passing namespace, look it up from the DocumentStore or something
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
            This => documentStore.Find<Subject>(subjectSet.AsKey(), subject.AsKey()).IsSome,

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
                documentStore.Find<SubjectSet>(
                    subjectSet.Resource.AsKey(tupleToSubjectSet.TuplesetRelation),
                    KeyRange.Unbound)
                    .Any(parentSubjectSet =>
                        IsAMemberOf(
                            subject,
                            new SubjectSet(parentSubjectSet.Record.Resource, tupleToSubjectSet.ComputedSubjectSetRelation))),

            _ => throw new NotSupportedException()
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AssociateResponse Associate(Resource resource, Relationship relationship, Either<Subject, SubjectSet> subject, CancellationToken cancellationToken) =>
        subject.Match(
            Left: subject => TryPutOrUpdate(Document.Cons(resource.AsKey(relationship), subject.AsKey(), subject), cancellationToken),
            Right: subjectSet => TryPutOrUpdate(Document.Cons(resource.AsKey(relationship), subjectSet.AsKey(), subjectSet), cancellationToken));

    private AssociateResponse TryPutOrUpdate<R>(Document<R> document, CancellationToken cancellationToken) where R : notnull =>
        documentStore.Update(document, cancellationToken) switch
        {
            DocumentStore.UpdateResponse.Success => AssociateResponse.Success,
            DocumentStore.UpdateResponse.VersionConflictError => AssociateResponse.VersionCheckFailedError,
            DocumentStore.UpdateResponse.TimeoutError => AssociateResponse.TimeoutError,
            _ => throw new NotSupportedException()
        };
}
