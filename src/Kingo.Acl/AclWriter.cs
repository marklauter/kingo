using Kingo.Storage;
using LanguageExt;
using LanguageExt.Common;
using System.Runtime.CompilerServices;

namespace Kingo.Acl;

public sealed class AclWriter(DocumentWriter writer)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Either<Error, Unit> Associate(SubjectSet subjectSet, Either<Subject, SubjectSet> subject, CancellationToken cancellationToken) =>
        subject.Match(
            Left: sub => writer.InsertOrUpdate(AsDocument(subjectSet, sub), cancellationToken),
            Right: set => writer.InsertOrUpdate(AsDocument(subjectSet, set), cancellationToken));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Document<Subject> AsDocument(SubjectSet subjectSet, Subject subject) =>
        Document.Cons(subjectSet.AsKey(), subject.AsKey(), subject);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Document<SubjectSet> AsDocument(SubjectSet subjectSet, SubjectSet subject) =>
        Document.Cons(subjectSet.AsKey(), subject.AsKey(), subject);
}
