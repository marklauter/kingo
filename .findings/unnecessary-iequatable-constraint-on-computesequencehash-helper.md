# Unnecessary IEquatable constraint on ComputeSequenceHash helper

Severity: nit
Type: code
Location: `Kingo.Policies/PdlDocument.cs:18`
Principle: Inference, not annotation
The helper's body only calls HashCode.Add, which uses EqualityComparer<T>.Default and works for any T.

## Observation
```csharp
internal static int ComputeSequenceHash<T>(ImmutableArray<T> items)
    where T : IEquatable<T>
{
    var hash = new HashCode();
    foreach (var item in items)
        hash.Add(item);
    return hash.ToHashCode();
}
```
The body never relies on `IEquatable<T>`. `HashCode.Add<T>(T)` uses `EqualityComparer<T>.Default` internally, which works for any `T`.

## Why it matters
The constraint advertises a requirement the implementation does not have. A future caller passing a type that isn't `IEquatable<T>` would hit a compile error pointing at the constraint, not at the actual behaviour. Constraints should narrow only what the implementation truly needs.

## Suggested fix
Drop the `where T : IEquatable<T>` clause:

```csharp
internal static int ComputeSequenceHash<T>(ImmutableArray<T> items)
{
    var hash = new HashCode();
    foreach (var item in items)
        hash.Add(item);
    return hash.ToHashCode();
}
```
