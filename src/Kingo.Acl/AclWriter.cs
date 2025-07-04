using Kingo.Storage;
using LanguageExt;
using System.Runtime.CompilerServices;

namespace Kingo.Acl;

public sealed class AclWriter(DocumentStore store)
{
    public enum AssociateResponse
    {
        Success,
        TimeoutError,
        VersionCheckFailedError,
        NotFoundError,
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AssociateResponse Associate(Resource resource, Relationship relationship, Either<Subject, SubjectSet> subject, CancellationToken cancellationToken) =>
        subject.Match(
            Left: subject => PutOrUpdate(Document.Cons(resource.AsKey(relationship), subject.AsKey(), subject), cancellationToken),
            Right: subjectSet => PutOrUpdate(Document.Cons(resource.AsKey(relationship), subjectSet.AsKey(), subjectSet), cancellationToken));

    private AssociateResponse PutOrUpdate<R>(Document<R> document, CancellationToken cancellationToken) where R : notnull =>
        store.PutOrUpdate(document, cancellationToken) switch
        {
            DocumentStore.UpdateStatus.Success => AssociateResponse.Success,
            DocumentStore.UpdateStatus.VersionConflictError => AssociateResponse.VersionCheckFailedError,
            DocumentStore.UpdateStatus.TimeoutError => AssociateResponse.TimeoutError,
            DocumentStore.UpdateStatus.NotFoundError => AssociateResponse.NotFoundError,
            _ => throw new NotSupportedException()
        };
}
