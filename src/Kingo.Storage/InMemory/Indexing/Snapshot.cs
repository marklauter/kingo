using LanguageExt;
using System.Runtime.CompilerServices;

namespace Kingo.Storage.InMemory.Indexing;

public static class Snapshot
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Snapshot<HK> Empty<HK>()
        where HK : IEquatable<HK>, IComparable<HK> =>
        new(Prelude.Empty);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Snapshot<HK> Cons<HK>(Map<HK, Document<HK>> map)
        where HK : IEquatable<HK>, IComparable<HK> =>
        new(map);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Snapshot<HK, RK> Empty<HK, RK>()
        where HK : IEquatable<HK>, IComparable<HK>
        where RK : IEquatable<RK>, IComparable<RK> =>
        new(Prelude.Empty);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Snapshot<HK, RK> Cons<HK, RK>(Map<HK, Map<RK, Document<HK, RK>>> map)
        where HK : IEquatable<HK>, IComparable<HK>
        where RK : IEquatable<RK>, IComparable<RK> =>
        new(map);
}

public sealed record Snapshot<HK>(Map<HK, Document<HK>> Map)
    where HK : IEquatable<HK>, IComparable<HK>;

public sealed record Snapshot<HK, RK>(Map<HK, Map<RK, Document<HK, RK>>> Map)
    where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>;
