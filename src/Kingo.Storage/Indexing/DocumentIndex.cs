using System.Runtime.CompilerServices;

namespace Kingo.Storage.Indexing;

/// <summary>
/// encapsulates optimistic concurrency
/// </summary>
public sealed class DocumentIndex
{
    private Snapshot snapshot = Indexing.Snapshot.Empty;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Snapshot Snapshot() => snapshot;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Exchange(Snapshot snapshot, Snapshot replacement) =>
        ReferenceEquals(Interlocked.CompareExchange(ref this.snapshot, replacement, snapshot), snapshot);
}
