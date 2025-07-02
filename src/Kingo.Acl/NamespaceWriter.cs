using Kingo.Acl.SerializableNamespace;
using Kingo.Storage;
using Kingo.Storage.Keys;
using LanguageExt;

namespace Kingo.Acl;

public sealed class NamespaceWriter(DocumentStore documentStore)
{
    public enum WriteStatus
    {
        Success,
        TimeoutError,
        VersionCheckFailedError,
    }

    public (WriteStatus Status, Key DocumentId)[] Write(NamespaceSpec spec, CancellationToken cancellationToken) =>
        [.. Write($"{nameof(Namespace)}/{spec.Name}", spec, cancellationToken)];

    private IEnumerable<(WriteStatus Status, Key DocumentId)> Write(Key hashKey, NamespaceSpec spec, CancellationToken cancellationToken) =>
        spec.Relationships
            .Select(r => Document
                .Cons(
                    hashKey,
                    r.Name.AsRangeKey(),
                    ConvertRewrite(r.SubjectSetRewrite)))
            .Select(d => TryPutOrUpdate(d, cancellationToken));

    private (WriteStatus Status, Key DocumentId) TryPutOrUpdate(Document<SubjectSetRewrite> document, CancellationToken cancellationToken) =>
        documentStore.TryPutOrUpdate(document, cancellationToken) switch
        {
            DocumentStore.UpdateResponse.Success => (WriteStatus.Success, $"{document.HashKey}/{document.RangeKey}"),
            DocumentStore.UpdateResponse.VersionCheckFailedError => (WriteStatus.VersionCheckFailedError, $"{document.HashKey}/{document.RangeKey}"),
            DocumentStore.UpdateResponse.TimeoutError => (WriteStatus.TimeoutError, $"{document.HashKey}/{document.RangeKey}"),
            _ => throw new NotSupportedException()
        };

    internal static SubjectSetRewrite ConvertRewrite(SerializableNamespace.SubjectSetRewrite rule) =>
        rule switch
        {
            SerializableNamespace.This => This.Default,
            SerializableNamespace.ComputedSubjectSetRewrite computedSet => ComputedSubjectSetRewrite.From(computedSet.Relationship),
            SerializableNamespace.UnionRewrite union => UnionRewrite.From([.. union.Children.Select(ConvertRewrite)]),
            SerializableNamespace.IntersectionRewrite intersection => IntersectionRewrite.From([.. intersection.Children.Select(ConvertRewrite)]),
            SerializableNamespace.ExclusionRewrite exclusion => ExclusionRewrite.From(ConvertRewrite(exclusion.Include), ConvertRewrite(exclusion.Exclude)),
            SerializableNamespace.TupleToSubjectSetRewrite tupleToSubjectSet => TupleToSubjectSetRewrite.From(tupleToSubjectSet.TuplesetRelation, tupleToSubjectSet.ComputedSubjectSetRelation),
            _ => throw new NotSupportedException()
        };
}

