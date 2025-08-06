using Kingo.Storage;
using LanguageExt;

namespace Kingo.DictionaryEncoding;

public interface IKeyEncoding<T>
    where T : IKeyEncoding<T>
{
    string Key { get; }
    long Id { get; }
    static abstract int Size { get; }
}

public sealed record NamespaceEncoding(
    [property: HashKey] string Key,
    long Id)
    : IKeyEncoding<NamespaceEncoding>
{
    internal const int Bits = 18;
    public static int Size { get; } = Bits;
}

public sealed record RelationEncoding(
    [property: HashKey] string Key,
    long Id)
    : IKeyEncoding<NamespaceEncoding>
{
    internal const int Bits = 12;
    public static int Size { get; } = Bits;
}

public sealed record ResourceEncoding(
    [property: HashKey] string Key,
    long Id)
    : IKeyEncoding<NamespaceEncoding>
{
    internal const int Bits = 34;
    public static int Size { get; } = Bits;
}

public sealed class DicionaryEncoder<D>(
    IDocumentReader<D> reader,
    IDocumentWriter<D> writer,
    ISequence<int> sequence)
    where D : IKeyEncoding<D>
{
    public Eff<IKeyEncoding<D>> Encode<D>(string key)
        where D : IKeyEncoding<D> =>
        // 1. if exists, return existing value
        reader.Find(key)
            .Match<IKeyEncoding<D>>(
             Succ: o => o.IfNone(() => new NullEncoding(key)),
             Fail: e => e);// 2. allocate a sequence, write the value// 3. if size of id won't fit bit size, throw
}

public sealed class KeyEncoder<D>(
    IDocumentReader<D> reader,
    IDocumentWriter<D> writer)
{
    // compile-time check to ensure bit allocations sum to 64
    // if it turns red check the packing sizes defined in the encoding classes
    private const int TotalBitsMustBe64 = 1 / (NamespaceEncoding.Bits + RelationEncoding.Bits + ResourceEncoding.Bits == 64 ? 1 : 0);

    //    // packing shifts
    //    private const int RelationShift = ResourceBits;
    //    private const int NamespaceShift = RelationShift + RelationBits;

    //    // unpacking masks
    //    private const ulong ResourceMask = (1UL << ResourceBits) - 1;
    //    private const ulong RelationMask = (1UL << RelationBits) - 1;

    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    public Either<Error, ulong> Pack(Resource resource, Relationship relationship, CancellationToken ct) =>
    //        ReadIds(resource, relationship, ct)
    //        .Map(ids => Pack(ids.namespaceId, ids.resourceId, ids.relationshipId));

    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    public Either<Error, ulong> Pack(SubjectSet subjectSet, CancellationToken ct) =>
    //        Pack(subjectSet.Resource, subjectSet.Relationship, ct);

    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    internal static ulong Pack(ulong namespaceId, ulong resourceId, ulong relationshipId) =>
    //        (namespaceId << NamespaceShift) | (relationshipId << RelationShift) | resourceId;

    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    internal static (ulong namespaceId, ulong relationId, ulong resourceId) Unpack(ulong key) =>
    //        (key >> NamespaceShift, (key >> RelationShift) & RelationMask, key & ResourceMask);

    //    private Either<Error, (ulong namespaceId, ulong resourceId, ulong relationshipId)> ReadIds(
    //        Resource resource,
    //        Relationship relationship,
    //        CancellationToken ct) =>
    //        Prelude.Right<Error, Func<ulong, ulong, ulong, (ulong, ulong, ulong)>>(static (ns, res, rel) => (ns, res, rel))
    //        .Apply(GetOrCreateId(NamespaceKey, Key.From(resource.Policy), ct))
    //        .Apply(GetOrCreateId(ResourceKey, Key.From(resource.Name), ct))
    //        .Apply(GetOrCreateId(RelationshipKey, Key.From(relationship), ct));

    //    private Either<Error, ulong> GetOrCreateId(Key idKind, Key rangeKey, CancellationToken ct)
    //    {
    //        var hashKey = DictionaryHk(idKind);
    //        return GetId(hashKey, rangeKey)
    //            .Match(
    //                Some: Prelude.Right<Error, ulong>,
    //                None: () => CreateId(idKind, hashKey, rangeKey, ct));
    //    }

    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    private static Key DictionaryHk(Key idType) => $"enc/{idType}";

    //    private Either<Error, ulong> CreateId(Key idType, Key hashKey, Key rangeKey, CancellationToken ct) =>
    //        sequence.Next(idType, ct)
    //        .Bind(newId => WriteIdMapping(hashKey, rangeKey, newId, ct))
    //        .BindLeft(err => GetId(hashKey, rangeKey)
    //        .ToEither(Error.New(err.Code, $"failed to read ID for {hashKey}/{rangeKey} after a suspected race condition.", err)));

    //    private Either<Error, ulong> WriteIdMapping(Key hashKey, Key rangeKey, ulong newId, CancellationToken ct) =>
    //        writer.Insert(Document.Cons(hashKey, rangeKey, newId), ct)
    //        .Map(_ => newId);

    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    private Option<ulong> GetId(Key hashKey, Key rangeKey) =>
    //        reader.Find<ulong>(hashKey, rangeKey)
    //        .Map(x => x.Record);
}

