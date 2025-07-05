using Kingo.Storage.Keys;
using LanguageExt;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Kingo.Storage;

/// <summary>
/// encapsulation of optimistic concurrency
/// </summary>
internal static class DocumentIndex
{
    public sealed record Index(Map<Key, Map<Key, Document>> Map)
    {
        public static Index Empty = new(Prelude.Empty);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Index From(Map<Key, Map<Key, Document>> map) => new(map);
    }

    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "it's fine")]
    private static Index snapshot = Index.Empty;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Index Snapshot() => snapshot;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Exchange(Index snapshot, Index replacement) =>
        ReferenceEquals(Interlocked.CompareExchange(ref DocumentIndex.snapshot, replacement, snapshot), snapshot);
}
