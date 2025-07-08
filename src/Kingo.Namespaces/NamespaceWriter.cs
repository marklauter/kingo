using Kingo.Namespaces.Serializable;
using Kingo.Storage;
using Kingo.Storage.Clocks;
using Kingo.Storage.Keys;
using LanguageExt;
using LanguageExt.Common;
using System.Runtime.CompilerServices;

namespace Kingo.Namespaces;

public enum KeyType
{
    Namespace,
    Resource,
    Relationship,
}

public sealed class KeyEncoder(
    DocumentWriter<Key, Key> writer,
    DocumentReader<Key, Key> reader,
    Sequence<ulong> sequence)
{
    private static readonly Key NamespaceKey = Key.From("enc/nam");
    private static readonly Key ResourceKey = Key.From("enc/res");
    private static readonly Key RelationshipKey = Key.From("enc/rel");

    public Either<Error, ulong> MapId(
        KeyType type,
        Key value,
        CancellationToken ct) =>
        type switch
        {
            KeyType.Namespace => MapId(NamespaceKey, value, ct),
            KeyType.Resource => MapId(ResourceKey, value, ct),
            KeyType.Relationship => MapId(RelationshipKey, value, ct),
            _ => throw new NotSupportedException(),
        };

    private Either<Error, ulong> MapId(
        Key hashKey,
        Key rangeKey,
        CancellationToken ct) =>
        sequence.Next(hashKey, ct)
        .Bind(newId => WriteIdMapping(hashKey, rangeKey, newId, ct))
        .BindLeft(err => GetId(hashKey, rangeKey)
        .ToEither(Error.New(err.Code, $"failed to read ID for {hashKey}/{rangeKey} after a suspected race condition.", err)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Either<Error, ulong> WriteIdMapping(Key hashKey, Key rangeKey, ulong newId, CancellationToken ct) =>
        writer.Insert(Document.Cons(hashKey, rangeKey, Document.ConsData("id", newId)), ct)
        .Map(_ => newId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Option<ulong> GetId(Key hashKey, Key rangeKey) =>
        reader.Find(hashKey, rangeKey)
        .Bind(d => d.Field<ulong>("id"));
}

public sealed class NamespaceWriter(
    DocumentWriter<Key, Key> writer,
    KeyEncoder keyEncoder)
{
    private static readonly Key RewriteValueKey = Key.From("ssr");

    public Arr<Either<Error, Unit>> Insert(string json, CancellationToken cancellationToken) =>
        Insert(NamespaceSpec.FromJson(json), cancellationToken);

    public Arr<Either<Error, Unit>> Insert(NamespaceSpec spec, CancellationToken cancellationToken) =>
        [.. spec.TransformRewrite().Map(d => Insert(d, cancellationToken))];

    private Either<Error, Unit> Insert(Document<Key, Key> document, CancellationToken cancellationToken) =>
        writer.Insert(document, cancellationToken)
        .MapLeft(e => Error.New(e.Code, $" failed to insert {nameof(SubjectSetRewrite)}: {document.HashKey}/{document.RangeKey}", e));

    public Arr<Either<Error, Unit>> Update(string json, CancellationToken cancellationToken) =>
        Update(NamespaceSpec.FromJson(json), cancellationToken);

    public Arr<Either<Error, Unit>> Update(NamespaceSpec spec, CancellationToken cancellationToken) =>
        [.. spec.TransformRewrite().Map(d => Update(d, cancellationToken))];

    private Either<Error, Unit> Update(Document<Key, Key> document, CancellationToken cancellationToken) =>
        writer.Update(document, cancellationToken)
        .MapLeft(e => Error.New(e.Code, $" failed to update {nameof(SubjectSetRewrite)}: {document.HashKey}/{document.RangeKey}", e));
}
