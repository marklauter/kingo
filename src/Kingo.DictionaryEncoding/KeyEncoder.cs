using Kingo.Storage;
using Kingo.Storage.Clocks;
using Kingo.Storage.Keys;
using LanguageExt;
using LanguageExt.Common;
using System.Runtime.CompilerServices;

namespace Kingo.DictionaryEncoding;

public class KeyEncoder(
    DocumentReader reader,
    DocumentWriter writer)
{
    private readonly Sequence<ulong> sequence = new(reader, writer);
    private static readonly Key NamespaceKey = Key.From("namespace");
    private static readonly Key ResourceKey = Key.From("resource");
    private static readonly Key RelationshipKey = Key.From("relationship");

    // Bit allocation
    private const int NamespaceBits = 16;
    private const int RelationBits = 14;
    private const int ResourceBits = 34;

    // Bit shifts for packing
    private const int ResourceShift = 0; // Not strictly needed, but good for clarity
    private const int RelationShift = ResourceBits;
    private const int NamespaceShift = RelationShift + RelationBits;

    // Masks for unpacking
    private const ulong ResourceMask = (1UL << ResourceBits) - 1;
    private const ulong RelationMask = (1UL << RelationBits) - 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Either<Error, ulong> Pack(SubjectSet subjectSet, CancellationToken cancellationToken) =>
        ReadIds(subjectSet.Resource, subjectSet.Relationship, cancellationToken)
            .Map(ids => Pack(ids.namespaceId, ids.resourceId, ids.relationshipId));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong Pack(ulong namespaceId, ulong resourceId, ulong relationshipId) =>
        (namespaceId << NamespaceShift) | (relationshipId << RelationShift) | (resourceId << ResourceShift);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static (ulong namespaceId, ulong relationId, ulong resourceId) Unpack(ulong key) =>
        (key >> NamespaceShift, (key >> RelationShift) & RelationMask, key & ResourceMask);

    private Either<Error, (ulong namespaceId, ulong resourceId, ulong relationshipId)> ReadIds(Resource resource, Relationship relationship, CancellationToken cancellationToken)
    {
        var nsId = GetOrCreateId(NamespaceKey, Key.From(resource.Namespace), cancellationToken);
        var resId = GetOrCreateId(ResourceKey, Key.From(resource.Name), cancellationToken);
        var relId = GetOrCreateId(RelationshipKey, Key.From(relationship), cancellationToken);

        return (Either<Error, (ulong namespaceId, ulong resourceId, ulong relationshipId)>)(nsId, resId, relId).Apply((n, r, l) => (n, r, l));
    }

    private Either<Error, ulong> GetOrCreateId(Key idType, Key rK, CancellationToken cancellationToken)
    {
        var hK = DictionaryHk(idType);
        return GetId(hK, rK)
            .Match(
                Some: Prelude.Right<Error, ulong>,
                None: () => CreateId(hK, idType, rK, cancellationToken));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Key DictionaryHk(Key idType) => $"enc/{idType}";

    private Either<Error, ulong> CreateId(Key hk, Key idType, Key rK, CancellationToken cancellationToken) =>
        sequence.Next(idType, cancellationToken)
            .Bind(newId => WriteIdMapping(hk, rK, newId))
            .BindLeft(_ => GetId(hk, rK)
                .ToEither(Error.New($"Failed to read ID for '{rK}' after a suspected race condition.")));

    private Either<Error, ulong> WriteIdMapping(Key hk, Key rK, ulong newId) =>
        writer.Insert(Document.Cons(hk, rK, newId), CancellationToken.None)
        .Map(_ => newId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Option<ulong> GetId(Key hk, Key rK) =>
        reader.Find<ulong>(hk, rK)
            .Map(x => x.Record);
}
