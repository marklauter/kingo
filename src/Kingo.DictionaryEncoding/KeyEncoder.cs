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
    public Either<Error, ulong> Pack(Resource resource, Relationship relationship, CancellationToken cancellationToken) =>
        ReadIds(resource, relationship, cancellationToken)
            .Map(ids => Pack(ids.namespaceId, ids.resourceId, ids.relationshipId));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Pack(ulong namespaceId, ulong resourceId, ulong relationshipId) =>
        (namespaceId << NamespaceShift) | (relationshipId << RelationShift) | (resourceId << ResourceShift);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (ulong namespaceId, ulong relationId, ulong resourceId) Unpack(ulong key) =>
        (key >> NamespaceShift, (key >> RelationShift) & RelationMask, key & ResourceMask);

    private Either<Error, (ulong namespaceId, ulong resourceId, ulong relationshipId)> ReadIds(Resource resource, Relationship relationship, CancellationToken cancellationToken)
    {
        var nsId = GetOrCreateId(NamespaceKey, Key.From(resource.Namespace), cancellationToken);
        var resId = GetOrCreateId(ResourceKey, Key.From(resource.Name), cancellationToken);
        var relId = GetOrCreateId(RelationshipKey, Key.From(relationship), cancellationToken);

        return (Either<Error, (ulong namespaceId, ulong resourceId, ulong relationshipId)>)(nsId, resId, relId).Apply((n, r, l) => (n, r, l));
    }

    private Either<Error, ulong> GetOrCreateId(Key idType, Key key, CancellationToken cancellationToken)
    {
        var dictionaryHk = $"encoding/{idType}";

        return ReadId(dictionaryHk, key)
            .ToEither(Error.New("ID not found, attempting creation."))
            .BindLeft(_ => CreateId(dictionaryHk, idType, key, cancellationToken));
    }

    private Either<Error, ulong> CreateId(Key dictionaryHk, Key idType, Key key, CancellationToken cancellationToken) =>
        clock.Tick(idType, cancellationToken)
            .Bind(newId => WriteIdMapping(dictionaryHk, key, newId))
            .BindLeft(_ => ReadId(dictionaryHk, key)
                .ToEither(Error.New($"Failed to read ID for '{key}' after a suspected race condition.")));

    private Either<Error, ulong> WriteIdMapping(Key dictionaryHk, Key key, ulong newId) =>
        store.Put(Document.Cons(dictionaryHk, key, newId), CancellationToken.None) switch
        {
            DocumentStore.PutStatus.Success => newId,
            DocumentStore.PutStatus.DuplicateKeyError => Error.New("Duplicate key, potential race condition."),
            var status => Error.New($"Failed to create dictionary mapping. Status: {status}")
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Option<ulong> ReadId(Key dictionaryHk, Key key) =>
        store.Find<ulong>(dictionaryHk, key)
            .Map(x => x.Record);
}
