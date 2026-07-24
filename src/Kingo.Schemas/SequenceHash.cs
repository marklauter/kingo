using System.Collections.Immutable;

namespace Kingo.Schemas;

/// <summary>
/// Order-sensitive structural hash over an <see cref="ImmutableArray{T}"/> — the counterpart to the element-wise <c>Equals</c> the spec model's
/// collection-bearing records declare, since a record's synthesized <c>GetHashCode</c> would hash the array by reference and disagree with them.
/// </summary>
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
