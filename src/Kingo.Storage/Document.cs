using LanguageExt;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;

namespace Kingo.Storage;

[AttributeUsage(AttributeTargets.Class)]
public sealed class NameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class HashKeyAttribute : Attribute;

[AttributeUsage(AttributeTargets.Property)]
public sealed class RangeKeyAttribute : Attribute;

[AttributeUsage(AttributeTargets.Property)]
public sealed class VersionAttribute : Attribute;

internal static class DocumentTypeCache<D>
{
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "this is fine")]
    public static string Name { get; } =
        typeof(D).GetCustomAttribute<NameAttribute>(true)?.Name
        ?? typeof(D).Name;

    public static PropertyInfo[] MappedProperties { get; } = [.. typeof(D)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(pi => pi.CanRead)
        .Where(pi => !pi.IsDefined(typeof(NotMappedAttribute), true))
        .Where(pi => pi.GetIndexParameters().Length == 0)];

    public static string[] PropertyNames { get; } = [.. MappedProperties.Select(pi => pi.Name)];

    public static PropertyInfo HashKeyProperty { get; } =
        MappedProperties.Single(pi => pi.IsDefined(typeof(HashKeyAttribute), true));

    public static Option<PropertyInfo> RangeKeyProperty { get; } =
        MappedProperties.SingleOrDefault(pi => pi.IsDefined(typeof(RangeKeyAttribute), true));

    public static Option<PropertyInfo> VersionProperty { get; } =
        MappedProperties.SingleOrDefault(pi => pi.IsDefined(typeof(VersionAttribute), true));

    static DocumentTypeCache() =>
        _ = VersionProperty.Match(
            Some: pi =>
            typeof(INumber<>).MakeGenericType(pi.PropertyType).IsAssignableFrom(pi.PropertyType)
                ? Prelude.unit
                : throw new InvalidOperationException($"Version property '{pi.Name}' of type '{pi.PropertyType.Name}' must implement INumber<{pi.PropertyType.Name}>"),
            None: () => Prelude.unit);
}
