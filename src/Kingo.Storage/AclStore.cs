using Kingo.Facts;
using Kingo.Storage.Ranges;
using LanguageExt;
using System.Runtime.CompilerServices;

namespace Kingo.Storage;

// <summary>
/// This is a demo store. A production store would use DynamoDB, Casandra, or other versioned key-value store.
/// </summary>
public sealed class AclStore(DocumentStore documentStore)
{
    public enum AssociateResponse
    {
        Success,
        TimeoutError,
        VersionCheckFailedError,
    }

    // todo: instead of passing namespace, look it up from the DocumentStore or something
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsAMemberOf(Subject subject, SubjectSet subjectSet, NamespaceTree tree) =>
        tree.Relationships.TryGetValue(subjectSet.Relationship, out var rewrite)
        && EvaluateRewrite(subject, subjectSet, tree, rewrite);

    private bool EvaluateRewrite(Subject subject, SubjectSet subjectSet, NamespaceTree namespaceTree, SubjectSetRewrite node)
        => node switch
        {
            This => documentStore.Find<Subject>(subjectSet.AsKey(), subject.AsKey()).IsSome,

            ComputedSubjectSetRewrite computedSet =>
                IsAMemberOf(subject, new SubjectSet(subjectSet.Resource, computedSet.Relationship), namespaceTree),

            UnionRewrite union =>
                union.Children.Any(child => EvaluateRewrite(subject, subjectSet, namespaceTree, child)),

            IntersectionRewrite intersection =>
                intersection.Children.All(child => EvaluateRewrite(subject, subjectSet, namespaceTree, child)),

            ExclusionRewrite exclusion =>
                EvaluateRewrite(subject, subjectSet, namespaceTree, exclusion.Include)
            && !EvaluateRewrite(subject, subjectSet, namespaceTree, exclusion.Exclude),

            TupleToSubjectSetRewrite tupleToSubjectSet =>
                documentStore.Find<SubjectSet>(
                    subjectSet.Resource.AsKey(tupleToSubjectSet.TuplesetRelation),
                    KeyRange.Unbound)
                    .Any(parentSubjectSet =>
                        IsAMemberOf(
                            subject,
                            new SubjectSet(parentSubjectSet.Record.Resource, tupleToSubjectSet.ComputedSubjectSetRelation),
                            namespaceTree)),

            _ => throw new NotSupportedException()
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AssociateResponse Associate(Resource resource, Relationship relationship, Either<Subject, SubjectSet> subject, CancellationToken cancellationToken) =>
        subject.Match(
            Left: subject => StoreDocument(Document.Cons(resource.AsKey(relationship), subject.AsKey(), subject), cancellationToken),
            Right: subjectSet => StoreDocument(Document.Cons(resource.AsKey(relationship), subjectSet.AsKey(), subjectSet), cancellationToken));

    private AssociateResponse StoreDocument<R>(Document<R> document, CancellationToken cancellationToken) where R : notnull =>
        documentStore.TryPutOrUpdate(document, cancellationToken) switch
        {
            DocumentStore.UpdateResponse.Success => AssociateResponse.Success,
            DocumentStore.UpdateResponse.VersionCheckFailedError => AssociateResponse.VersionCheckFailedError,
            DocumentStore.UpdateResponse.TimeoutError => AssociateResponse.TimeoutError,
            _ => throw new NotSupportedException()
        };
}
