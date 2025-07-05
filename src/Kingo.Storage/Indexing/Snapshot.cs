using Kingo.Storage.Keys;
using LanguageExt;
using System.Runtime.CompilerServices;

namespace Kingo.Storage.Indexing;

public sealed record Snapshot(Map<Key, Map<Key, Document>> Map)
{
    public static Snapshot Empty { get; } = new(Prelude.Empty);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Snapshot From(Map<Key, Map<Key, Document>> map) => new(map);
}
