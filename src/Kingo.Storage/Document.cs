using Kingo.Storage.Clocks;
using Kingo.Storage.Keys;
using LanguageExt;

namespace Kingo.Storage;

public record Document(
    Revision Version,
    Map<Key, object> Data);

public record Document<HK>(
    Revision Version,
    Map<Key, object> Data,
    HK HashKey)
    : Document(Version, Data)
    where HK : IEquatable<HK>, IComparable<HK>;

public record Document<HK, RK>(
    Revision Version,
    Map<Key, object> Data,
    HK HashKey,
    RK RangeKey)
    : Document<HK>(Version, Data, HashKey)
    where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>;
