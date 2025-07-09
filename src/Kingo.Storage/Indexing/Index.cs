using System.Runtime.CompilerServices;

namespace Kingo.Storage.Indexing;

public static class Index
{
    public static Index<HK> Empty<HK>()
        where HK : IEquatable<HK>, IComparable<HK> =>
        new();

    public static Index<HK, RK> Empty<HK, RK>()
        where HK : IEquatable<HK>, IComparable<HK>
        where RK : IEquatable<RK>, IComparable<RK> =>
        new();
}

public sealed class Index<HK>
    where HK : IEquatable<HK>, IComparable<HK>
{
    private Snapshot<HK> snapshot = Indexing.Snapshot.Empty<HK>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Snapshot<HK> Snapshot() => snapshot;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Exchange(Snapshot<HK> snapshot, Snapshot<HK> replacement) =>
        ReferenceEquals(Interlocked.CompareExchange(ref this.snapshot, replacement, snapshot), snapshot);
}

public sealed class Index<HK, RK>
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
