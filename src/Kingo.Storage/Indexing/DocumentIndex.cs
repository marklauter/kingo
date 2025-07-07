using System.Runtime.CompilerServices;

namespace Kingo.Storage.Indexing;

public static class DocumentIndex
{
    public static DocumentIndex<HK, RK> Empty<HK, RK>()
    where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>
        => new();
}

/// <summary>
/// encapsulates optimistic concurrency
/// </summary>
public sealed class DocumentIndex<HK, RK>
    where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>
{
    private Snapshot<HK, RK> snapshot = Indexing.Snapshot.Empty<HK, RK>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Snapshot<HK, RK> Snapshot() => snapshot;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Exchange(Snapshot<HK, RK> snapshot, Snapshot<HK, RK> replacement) =>
        ReferenceEquals(Interlocked.CompareExchange(ref this.snapshot, replacement, snapshot), snapshot);
}
