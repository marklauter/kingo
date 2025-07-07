using Kingo.Namespaces.Serializable;
using Kingo.Storage;
using LanguageExt;
using LanguageExt.Common;

namespace Kingo.Namespaces;

public sealed class NamespaceWriter(DocumentWriter writer)
{
    public Arr<Either<Error, Unit>> Insert(string json, CancellationToken cancellationToken) =>
        Insert(NamespaceSpec.FromJson(json), cancellationToken);

    public Arr<Either<Error, Unit>> Insert(NamespaceSpec spec, CancellationToken cancellationToken) =>
        [.. spec.TransformRewrite().Map(d => Insert(d, cancellationToken))];

    private Either<Error, Unit> Insert(Document<SubjectSetRewrite> document, CancellationToken cancellationToken) =>
        writer.Insert(document, cancellationToken)
        .MapLeft(e => Error.New(e.Code, $" failed to insert {nameof(SubjectSetRewrite)}: {document.HashKey}/{document.RangeKey}", e));

    public Arr<Either<Error, Unit>> Update(string json, CancellationToken cancellationToken) =>
        Update(NamespaceSpec.FromJson(json), cancellationToken);

    public Arr<Either<Error, Unit>> Update(NamespaceSpec spec, CancellationToken cancellationToken) =>
        [.. spec.TransformRewrite().Map(d => Update(d, cancellationToken))];

    private Either<Error, Unit> Update(Document<SubjectSetRewrite> document, CancellationToken cancellationToken) =>
        writer.Update(document, cancellationToken)
        .MapLeft(e => Error.New(e.Code, $" failed to update {nameof(SubjectSetRewrite)}: {document.HashKey}/{document.RangeKey}", e));
}
