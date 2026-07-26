using System.Collections.Immutable;

namespace Kingo.Theories;

internal static class SequenceHash
{
    public static int Of<T>(ImmutableArray<T> items)
    {
        var hash = new HashCode();
        foreach (var item in items)
            hash.Add(item);
        return hash.ToHashCode();
    }
}
