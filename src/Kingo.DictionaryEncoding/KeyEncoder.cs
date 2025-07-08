using Kingo.Storage;
using Kingo.Storage.Keys;
using LanguageExt;
using LanguageExt.Common;
using System.Runtime.CompilerServices;

namespace Kingo.DictionaryEncoding;

// todo: key encoder needs reader/writer that don't require range key
public sealed class KeyEncoder(DocumentReader<BigId> reader)
//DocumentWriter<Key> writer)
{
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
    public Either<Error, ulong> Pack(Resource resource, Relationship relationship, CancellationToken ct) =>
        ReadIds(resource, relationship, ct)
        .Map(ids => Pack(ids.namespaceId, ids.resourceId, ids.relationshipId));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Either<Error, ulong> Pack(SubjectSet subjectSet, CancellationToken ct) =>
        Pack(subjectSet.Resource, subjectSet.Relationship, ct);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong Pack(ulong namespaceId, ulong resourceId, ulong relationshipId) =>
        (namespaceId << NamespaceShift) | (relationshipId << RelationShift) | resourceId;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static (ulong namespaceId, ulong relationId, ulong resourceId) Unpack(ulong key) =>
        (key >> NamespaceShift, (key >> RelationShift) & RelationMask, key & ResourceMask);

    private Either<Error, (ulong namespaceId, ulong resourceId, ulong relationshipId)> ReadIds(
        Resource resource,
        Relationship relationship,
        CancellationToken ct) =>
        Prelude.Right<Error, Func<ulong, ulong, ulong, (ulong, ulong, ulong)>>(static (ns, res, rel) => (ns, res, rel))
        .Apply(GetOrCreateId(NamespaceKey, Key.From(resource.Namespace), ct))
        .Apply(GetOrCreateId(ResourceKey, Key.From(resource.Name), ct))
        .Apply(GetOrCreateId(RelationshipKey, Key.From(relationship), ct));

    private Either<Error, ulong> GetOrCreateId(Key idKind, Key rangeKey, CancellationToken ct)
    {
        var hashKey = DictionaryHk(idKind);
        return GetId(hashKey, rangeKey)
            .Match(
                Some: Prelude.Right<Error, ulong>,
                None: () => CreateId(idKind, hashKey, rangeKey, ct));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Key DictionaryHk(Key idType) => $"enc/{idType}";

    private Either<Error, ulong> CreateId(Key idType, Key hashKey, Key rangeKey, CancellationToken ct) =>
        sequence.Next(idType, ct)
        .Bind(newId => WriteIdMapping(hashKey, rangeKey, newId, ct))
        .BindLeft(err => GetId(hashKey, rangeKey)
        .ToEither(Error.New(err.Code, $"failed to read ID for {hashKey}/{rangeKey} after a suspected race condition.", err)));

    private Either<Error, ulong> WriteIdMapping(Key hashKey, Key rangeKey, ulong newId, CancellationToken ct) =>
        writer.Insert(Document.Cons(hashKey, rangeKey, newId), ct)
        .Map(_ => newId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Option<ulong> GetId(Key hashKey, Key rangeKey) =>
        reader.Find<ulong>(hashKey, rangeKey)
        .Map(x => x.Record);
}
