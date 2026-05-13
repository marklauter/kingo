# ComputeSequenceHash lives on PdlDocument but is called by sibling records

Severity: nit
Type: code
Location: `Kingo.Pdl/PdlDocument.cs:18, Kingo.Pdl/PdlDocument.cs:33, Kingo.Pdl/PdlDocument.cs:64, Kingo.Pdl/PdlDocument.cs:73`
Principle: One source of truth
Shared hashing helper sits on one record while three other records reach across to call it.

## Observation
`PdlDocument` defines `internal static int ComputeSequenceHash<T>(ImmutableArray<T> items)` and consumes it in its own `GetHashCode`. `Namespace`, `UnionRewrite`, and `IntersectionRewrite` also call `PdlDocument.ComputeSequenceHash(...)`. The helper is structurally an internal utility, not a member of `PdlDocument`'s API.

## Why it matters
The reader of `Namespace.GetHashCode` jumps to `PdlDocument` to understand the call. Future records that hold collections must remember "the helper lives over there," creating a name-coupling that the type system doesn't reinforce. A standalone helper makes ownership obvious and the call site reads at face value.

## Suggested fix
Move the helper to a dedicated internal static class in the same file:

```csharp
internal static class PolicyHash
{
    public static int OfSequence<T>(ImmutableArray<T> items)
    {
        var hash = new HashCode();
        foreach (var item in items)
            hash.Add(item);
        return hash.ToHashCode();
    }
}
```

Update the four call sites (including `PdlDocument` itself) to `PolicyHash.OfSequence(...)`.
