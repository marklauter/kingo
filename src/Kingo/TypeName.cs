using System.Diagnostics.CodeAnalysis;

namespace Kingo;

internal static class TypeName<T> where T : notnull
{
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "it's fine")]
    public static string Value { get; } = typeof(T).Name;
}
