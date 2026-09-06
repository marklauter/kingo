---
title: Representing domain collections
type: decision
summary: "The domain core is immutable, so collection types are chosen on use case and performance within that constraint. Build-once/read-many traversal makes ImmutableArray<T> the best match."
tags: [ddd, performance]
status: locked
cites:
  - "[[theories]]"
  - "[[grouping-the-apis-into-services]]"
---

# Domain collections are immutable, and the representation is chosen for the read path

The domain core is immutable. C# offers a wide selection of immutable collection types that vary in performance profile and use case.

Kingo's typical use case is build-once, read-many: a domain value is constructed whole and never edited in place, so traversal dominates. `ImmutableArray<T>` is the best match for this use case vs performance tradeoff. It's a struct wrapper over a plain `T[]`, contiguous, O(1) index, allocation-free enumeration, no per-element overhead.

## Rejected alternatives

- Mutable collections — `List<T>` or a bare array. Highest performance on every axis, and excluded by the premise: mutability increases entropy.
- `ImmutableList<T>`. An AVL tree with structural sharing. It buys O(log n) update, which Kingo never performs, and pays pointer-chasing on every read.
- `ImmutableHashSet<T>` and `ImmutableSortedSet<T>`. Set semantics by construction, at the cost of authored order, which theory-document round-trip fidelity needs. A sorted set would also make union and intersection order-insensitive, but only given a total order over rewrites invented for equality alone.
- `ImmutableQueue<T>` and `ImmutableStack<T>`. Access disciplines rather than collections: FIFO and LIFO, no indexing. Every domain collection is traversed whole.
- `IEnumerable<T>`. A sequence protocol rather than a collection. Deferred execution means two enumerations need not agree, so a value holding one is not a snapshot.
- A read-only wrapper over mutable storage — `IReadOnlyList<T>`, `ReadOnlyCollection<T>`. Immutability by promise: the underlying array stays mutable and the original holder writes through it.
- Keyed collections — `ImmutableDictionary<TKey, TValue>`, `FrozenDictionary`, `FrozenSet`. Lookup at the wrong layer. Keyed access to a rewrite is the interpreters' read-side projection, not the write-side model.

## Accepted tradeoffs

- Update is O(n). Every change copies the whole array, and nothing updates a domain collection. If incremental editing arrives, the builder lives outside the domain value.
- Custom structural equality is mandatory. Default equality compares the inner array *reference*, so a record holding one overrides `Equals` and `GetHashCode` with span-based `SequenceEqual`. A record without the override is a defect.
- `default(ImmutableArray<T>)` wraps a null array and throws on use, a trash value with no fail-loud treatment. Construction through the primary constructors avoids it; it surfaces at deserialization boundaries.
- Equality is order-sensitive. A namespace's relations need authored order, and nothing compares two rewrites built in different operand orders. A consumer that ever needs order-insensitive comparison does it at the comparison site, the same layering that keeps keyed lookup out of the model.

An array has no set semantics, so a field needing them enforces them at construction: a namespace rejects duplicate relation names, which makes a duplicate unrepresentable without a keyed collection.

What it buys is immutability by construction: no defensive copying, allocation-free traversal, and a value that cannot change under a concurrent reader.
