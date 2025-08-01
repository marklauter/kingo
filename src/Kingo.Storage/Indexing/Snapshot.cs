﻿using LanguageExt;
using System.Runtime.CompilerServices;

namespace Kingo.Storage.Indexing;

public static class Snapshot
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Snapshot<HK, RK> Empty<HK, RK>()
    where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>
        => new(Prelude.Empty);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Snapshot<HK, RK> From<HK, RK>(Map<HK, Map<RK, Document<HK, RK>>> map)
    where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>
        => new(map);
}

public sealed record Snapshot<HK, RK>(Map<HK, Map<RK, Document<HK, RK>>> Map)
    where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>;
