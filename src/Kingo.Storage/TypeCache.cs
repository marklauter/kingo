using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Kingo.Storage;

internal static class TypeCache<T> where T : notnull
{
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "it's fine")]
    public static string Value { get; } = typeof(T).Name;

    private static readonly HashSet<string> ExcludedNames = ["HashKey", "RangeKey"];

    public static string[] PropertyNames { get; } = [.. typeof(T)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(pi => pi.CanRead)
        .Where(pi => pi.GetIndexParameters().Length == 0)
        .Where(pi => !ExcludedNames.Contains(pi.Name))
        .Select(pi => pi.Name)];
}
