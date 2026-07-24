using System.Collections.Immutable;

namespace Kingo.Domains;

/// <summary>
/// Order-sensitive structural hash over an <see cref="ImmutableArray{T}"/>. It is the counterpart to the element-wise <c>Equals</c> the domain model's
/// collection-bearing records declare, because a record's synthesized <c>GetHashCode</c> would hash the array by reference and disagree with them.
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
