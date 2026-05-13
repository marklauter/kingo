# Identifier value types are readonly struct rather than readonly record struct

Severity: nit
Type: code
Location: `Kingo.Policies/NamespaceIdentifier.cs:11, Kingo.Policies/RelationIdentifier.cs:10`
Principle: Make invalid states unrepresentable
Hand-rolled struct with manual equality and operators duplicates what a record struct would synthesize.

## Observation
`NamespaceIdentifier` and `RelationIdentifier` are declared as `public readonly struct ... : IEquatable<...>, IComparable<...>` with hand-written `Equals(T)`, `Equals(object?)`, `GetHashCode()`, `==`, `!=`, `<`, `<=`, `>`, `>=`. The pattern was carried verbatim from the dictionary-encoding quarry.

## Why it matters
writing-csharp: "`readonly record struct` for value-type wrappers. Wrap `string` → `FilePath`... value semantics, no heap allocation, equality and `ToString` for free."

`readonly record struct` would synthesize `IEquatable<TSelf>`, `==`, `!=`, and `GetHashCode` automatically. The custom `IEquatable<string>` / `IComparable<string>` and the implicit operators would still need to live by hand, but the volume of boilerplate shrinks meaningfully and the type clearly signals "value semantics" at the declaration site.

This is the first slice of an identifier value type on `reboot`. Future identifier types will pattern-match against whichever shape lands here, so it's worth getting right.

## Suggested fix
Convert both to `public readonly record struct`. Keep the manual `ToString()`, `IEquatable<string>`, `IComparable<string>`, and the implicit operators (record struct doesn't synthesize those). Delete the now-redundant `Equals(NamespaceIdentifier)`, `Equals(object?)`, `GetHashCode()`, and the six comparison operators.

Re-run the test suite — `Equals` and ordering semantics need to match the existing behaviour, particularly the `StringComparison.Ordinal` choice baked into the current hand-rolled equality.
