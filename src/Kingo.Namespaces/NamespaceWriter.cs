using Kingo.Namespaces.Serializable;
using Kingo.Storage;
using Kingo.Storage.Keys;
using LanguageExt;

namespace Kingo.Namespaces;

public sealed class NamespaceWriter(DocumentStore documentStore)
{
    public enum WriteStatus
    {
        Success,
        TimeoutError,
        VersionCheckFailedError,
    }

    public (WriteStatus Status, Key DocumentId)[] Write(string json, CancellationToken cancellationToken) =>
        Write(NamespaceSpec.FromJson(json), cancellationToken);

    public (WriteStatus Status, Key DocumentId)[] Write(NamespaceSpec spec, CancellationToken cancellationToken) =>
        Write($"{nameof(Namespace)}/{spec.Name}", spec.Relationships, cancellationToken);

    private (WriteStatus Status, Key DocumentId)[] Write(Key namespaceHashKey, IReadOnlyList<RelationshipSpec> relationships, CancellationToken cancellationToken) =>
        [.. relationships
            .Select(r => Document
                .Cons(
                    namespaceHashKey,
                    Key.From(r.Name),
                    ConvertRewrite(r.SubjectSetRewrite)))
            .Select(d => TryPutOrUpdate(d, cancellationToken))];

    private (WriteStatus Status, Key DocumentId) TryPutOrUpdate(Document<SubjectSetRewrite> document, CancellationToken cancellationToken) =>
        documentStore.Update(document, cancellationToken) switch
        {
            DocumentStore.UpdateResponse.Success => (WriteStatus.Success, $"{document.HashKey}/{document.RangeKey}"),
            DocumentStore.UpdateResponse.VersionConflictError => (WriteStatus.VersionCheckFailedError, $"{document.HashKey}/{document.RangeKey}"),
            DocumentStore.UpdateResponse.TimeoutError => (WriteStatus.TimeoutError, $"{document.HashKey}/{document.RangeKey}"),
            _ => throw new NotSupportedException()
        };

    internal static SubjectSetRewrite ConvertRewrite(Serializable.SubjectSetRewrite rule) =>
        rule switch
        {
            Serializable.This => This.Default,
            Serializable.ComputedSubjectSetRewrite computedSet => ComputedSubjectSetRewrite.From(computedSet.Relationship),
            Serializable.UnionRewrite union => UnionRewrite.From([.. union.Children.Select(ConvertRewrite)]),
            Serializable.IntersectionRewrite intersection => IntersectionRewrite.From([.. intersection.Children.Select(ConvertRewrite)]),
            Serializable.ExclusionRewrite exclusion => ExclusionRewrite.From(ConvertRewrite(exclusion.Include), ConvertRewrite(exclusion.Exclude)),
            Serializable.TupleToSubjectSetRewrite tupleToSubjectSet => TupleToSubjectSetRewrite.From(tupleToSubjectSet.TuplesetRelation, tupleToSubjectSet.ComputedSubjectSetRelation),
            _ => throw new NotSupportedException()
        };
}

