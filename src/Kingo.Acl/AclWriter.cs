using Kingo.DictionaryEncoding;
using Kingo.Storage;
using LanguageExt;
using LanguageExt.Common;
using System.Runtime.CompilerServices;

namespace Kingo.Acl;

public sealed class AclWriter(
    DocumentWriter<BigId, BigId> writer,
    KeyEncoder encoder)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Either<Error, Unit> Associate(SubjectSet subjectSet, Either<Subject, SubjectSet> subject, CancellationToken ct) =>
        subject.Match(
            Left: sub =>
                AsDocument(subjectSet, sub, ct)
                .Bind(d => writer.InsertOrUpdate(d, ct)),
            Right: set =>
                AsDocument(subjectSet, set, ct)
                .Bind(d => writer.InsertOrUpdate(d, ct)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Either<Error, Document<BigId, BigId, Subject>> AsDocument(SubjectSet subjectSet, Subject subject, CancellationToken ct) =>
        encoder.Pack(subjectSet, ct)
        .Map(BigId.From)
        .Map(hk => Document.Cons(hk, subject.Id, subject));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Either<Error, Document<BigId, BigId, SubjectSet>> AsDocument(SubjectSet subjectSet, SubjectSet subject, CancellationToken ct) =>
        Prelude.Right<Error, Func<ulong, ulong, Document<BigId, BigId, SubjectSet>>>(
                (hk, rk) => Document.Cons(BigId.From(hk), BigId.From(rk), subject))
            .Apply(encoder.Pack(subjectSet, ct))
            .Apply(encoder.Pack(subject, ct));
}
