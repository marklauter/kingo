using Kingo.Storage;
using Kingo.Storage.Keys;
using LanguageExt;
using LanguageExt.Common;
using System.Runtime.CompilerServices;

namespace Kingo.DictionaryEncoding;

public class KeyEncoder(DocumentStore store)
{
    private readonly Clock clock = new(store);
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
    public ulong Pack(Resource resource, Relationship relationship, CancellationToken cancellationToken)
    {
        var (namespaceId, resourceId, relationshipId) = ReadIds(resource, relationship, cancellationToken);
        return Pack(namespaceId, resourceId, relationshipId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Pack(ulong namespaceId, ulong resourceId, ulong relationshipId) =>
        (namespaceId << NamespaceShift) | (relationshipId << RelationShift) | (resourceId << ResourceShift);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (ulong namespaceId, ulong relationId, ulong resourceId) Unpack(ulong key) =>
        (key >> NamespaceShift, (key >> RelationShift) & RelationMask, key & ResourceMask);

    private (ulong namespaceId, ulong resourceId, ulong relationshipId) ReadIds(Resource resource, Relationship relationship, CancellationToken cancellationToken) =>
    (
        GetOrCreateId(NamespaceKey, Key.From(resource.Namespace), cancellationToken),
        GetOrCreateId(ResourceKey, Key.From(resource.Name), cancellationToken),
        GetOrCreateId(RelationshipKey, Key.From(relationship), cancellationToken)
    );

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Option<ulong> ReadId(Key dictionaryHk, Key key) =>
        store.Find<ulong>(dictionaryHk, key)
            .Map(x => x.Record);

    private Either<Error, ulong> GetOrCreateId(Key idType, Key key, CancellationToken cancellationToken)
    {
        var dictionaryHk = $"encoding/{idType}";

        return ReadId(dictionaryHk, key)
            .Match(
                Some: id => id,
                None: () => clock.Tick(idType, cancellationToken)
                    .Map(newId =>
                    {
                        var putStatus = store.Put(Document.Cons(dictionaryHk, key, newId), CancellationToken.None);
                        return putStatus == DocumentStore.PutStatus.Success
                            ? newId
                            : putStatus == DocumentStore.PutStatus.DuplicateKeyError
                                ? store
                                    .Find<ulong>(dictionaryHk, key)
                                    .Match(
                                        Some: doc => doc.Record,
                                        None: () => Error.New($"Failed to read ID for '{key}' after race condition."))
                                : Error.New($"Failed to create dictionary mapping for '{key}'. Status: {putStatus}");
                    }));
    }
}
