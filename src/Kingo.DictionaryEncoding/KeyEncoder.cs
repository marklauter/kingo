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

    // packing sizes
    private const int NamespaceBits = 16;
    private const int RelationBits = 14;
    private const int ResourceBits = 34;
    // compile-time check to ensure bit allocations sum to 64
    // if it turns red check the packing sizes above ^
    private const int TotalBitsMustBe64 = 1 / (NamespaceBits + RelationBits + ResourceBits == 64 ? 1 : 0);

    // packing shifts
    private const int RelationShift = ResourceBits;
    private const int NamespaceShift = RelationShift + RelationBits;

    // unpacking masks
    private const ulong ResourceMask = (1UL << ResourceBits) - 1;
    private const ulong RelationMask = (1UL << RelationBits) - 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Either<Error, ulong> Pack(SubjectSet subjectSet, CancellationToken ct) =>
        ReadIds(subjectSet.Resource, subjectSet.Relationship, ct)
            .Map(ids => Pack(ids.namespaceId, ids.resourceId, ids.relationshipId));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong Pack(ulong namespaceId, ulong resourceId, ulong relationshipId) =>
        (namespaceId << NamespaceShift) | (relationshipId << RelationShift) | resourceId;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static (ulong namespaceId, ulong relationId, ulong resourceId) Unpack(ulong key) =>
        (key >> NamespaceShift, (key >> RelationShift) & RelationMask, key & ResourceMask);

    private Either<Error, (ulong namespaceId, ulong resourceId, ulong relationshipId)> ReadIds(Resource resource, Relationship relationship, CancellationToken ct) =>
        Prelude.Right<Error, Func<ulong, ulong, ulong, (ulong, ulong, ulong)>>(static (ns, res, rel) => (ns, res, rel))
            .Apply(GetOrCreateId(NamespaceKey, Key.From(resource.Namespace), ct))
            .Apply(GetOrCreateId(ResourceKey, Key.From(resource.Name), ct))
            .Apply(GetOrCreateId(RelationshipKey, Key.From(relationship), ct));

    private Either<Error, ulong> GetOrCreateId(Key idType, Key rK, CancellationToken ct)
    {
        var hK = DictionaryHk(idType);
        return GetId(hK, rK)
            .Match(
                Some: Prelude.Right<Error, ulong>,
                None: () => CreateId(hK, idType, rK, ct));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Key DictionaryHk(Key idType) => $"enc/{idType}";

    private Either<Error, ulong> CreateId(Key hk, Key idType, Key rK, CancellationToken ct) =>
        sequence.Next(idType, ct)
            .Bind(newId => WriteIdMapping(hk, rK, newId, ct))
            .BindLeft(_ => GetId(hk, rK)
                .ToEither(Error.New($"Failed to read ID for '{rK}' after a suspected race condition.")));

    private Either<Error, ulong> WriteIdMapping(Key hk, Key rK, ulong newId, CancellationToken ct) =>
        writer.Insert(Document.Cons(hk, rK, newId), ct)
        .Map(_ => newId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Option<ulong> GetId(Key hk, Key rK) =>
        reader.Find<ulong>(hk, rK)
            .Map(x => x.Record);
}
