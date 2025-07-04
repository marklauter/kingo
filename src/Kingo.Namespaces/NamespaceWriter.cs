using Kingo.Namespaces.Serializable;
using Kingo.Storage;
using Kingo.Storage.Keys;
using LanguageExt;

namespace Kingo.Namespaces;

public sealed class NamespaceWriter(DocumentStore store)
{
    public enum PutStatus
    {
        Success,
        TimeoutError,
        DuplicateKeyError,
    }

    public enum UpdateStatus
    {
        Success,
        NotFoundError,
        TimeoutError,
        VersionConflictError,
    }

    public (PutStatus Status, Key DocumentId)[] Put(string json, CancellationToken cancellationToken) =>
        Put(NamespaceSpec.FromJson(json), cancellationToken);

    public (PutStatus Status, Key DocumentId)[] Put(NamespaceSpec spec, CancellationToken cancellationToken) =>
        Put($"{nameof(Namespace)}/{spec.Name}", spec.Relationships, cancellationToken);

    private (PutStatus Status, Key DocumentId)[] Put(Key namespaceHashKey, IReadOnlyList<RelationshipSpec> relationships, CancellationToken cancellationToken) =>
        [.. relationships
            .Select(r => Document
                .Cons(
                    namespaceHashKey,
                    Key.From(r.Name),
                    ConvertRewrite(r.SubjectSetRewrite)))
            .Select(d => Put(d, cancellationToken))];

    private (PutStatus Status, Key DocumentId) Put(Document<SubjectSetRewrite> document, CancellationToken cancellationToken) =>
        store.Put(document, cancellationToken) switch
        {
            DocumentStore.PutStatus.Success => (PutStatus.Success, $"{document.HashKey}/{document.RangeKey}"),
            DocumentStore.PutStatus.TimeoutError => (PutStatus.TimeoutError, $"{document.HashKey}/{document.RangeKey}"),
            DocumentStore.PutStatus.DuplicateKeyError => (PutStatus.DuplicateKeyError, $"{document.HashKey}/{document.RangeKey}"),
            _ => throw new NotSupportedException()
        };

    public (UpdateStatus Status, Key DocumentId)[] Update(string json, CancellationToken cancellationToken) =>
            Update(NamespaceSpec.FromJson(json), cancellationToken);

    public (UpdateStatus Status, Key DocumentId)[] Update(NamespaceSpec spec, CancellationToken cancellationToken) =>
        Update($"{nameof(Namespace)}/{spec.Name}", spec.Relationships, cancellationToken);

    private (UpdateStatus Status, Key DocumentId)[] Update(Key namespaceHashKey, IReadOnlyList<RelationshipSpec> relationships, CancellationToken cancellationToken) =>
        [.. relationships
            .Select(r => Document
                .Cons(
                    namespaceHashKey,
                    Key.From(r.Name),
                    ConvertRewrite(r.SubjectSetRewrite)))
            .Select(d => Update(d, cancellationToken))];

    private (UpdateStatus Status, Key DocumentId) Update(Document<SubjectSetRewrite> document, CancellationToken cancellationToken) =>
        store.Update(document, cancellationToken) switch
        {
            DocumentStore.UpdateStatus.Success => (UpdateStatus.Success, $"{document.HashKey}/{document.RangeKey}"),
            DocumentStore.UpdateStatus.NotFoundError => (UpdateStatus.NotFoundError, $"{document.HashKey}/{document.RangeKey}"),
            DocumentStore.UpdateStatus.TimeoutError => (UpdateStatus.TimeoutError, $"{document.HashKey}/{document.RangeKey}"),
            DocumentStore.UpdateStatus.VersionConflictError => (UpdateStatus.VersionConflictError, $"{document.HashKey}/{document.RangeKey}"),
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

