using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Kingo.Storage;

public abstract record Document(
    Revision Version)
{
    public Document()
        : this(Revision.Zero)
    { }
};

public abstract record Document<HK>(
    HK HashKey,
    Revision Version)
    : Document(Version)
    where HK : IEquatable<HK>, IComparable<HK>
{
    public Document(HK hashKey)
        : this(hashKey, Revision.Zero)
    { }
}

public abstract record Document<HK, RK>(
    HK HashKey,
    RK RangeKey,
    Revision Version)
    : Document<HK>(HashKey, Version)
    where HK : IEquatable<HK>, IComparable<HK>
    where RK : IEquatable<RK>, IComparable<RK>
{
    public Document(HK hashKey, RK rangeKey)
        : this(hashKey, rangeKey, Revision.Zero)
    { }
}

internal static class DocumentTypeCache<D>
    where D : Document
{
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "this is fine")]
    public static string TypeName { get; } = typeof(D).Name;

    public static string[] PropertyNames { get; } = [.. typeof(D)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(pi => pi.CanRead)
        .Where(pi=> !pi.IsDefined(typeof(NotMappedAttribute), true))
        .Where(pi => pi.GetIndexParameters().Length == 0)
        .Select(pi => pi.Name)];
}
