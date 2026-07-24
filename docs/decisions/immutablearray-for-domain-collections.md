---
title: ImmutableArray for domain collections
summary: "Domain values carry their collections as ImmutableArray<T>: spec values are build-once/read-many snapshots and mutation never touches these types — with custom structural equality and the default-instance trap riding along as mandatory caveats."
tags: [decision, ddd, performance]
created: 2026-07-14
status: locked
---

# ImmutableArray for domain collections

## Observation

`ImmutableArray<T>` is a thin struct wrapper around a plain `T[]` — contiguous memory, O(1) index, allocation-free enumeration, zero per-element overhead. Its trade is on mutation: every "update" copies the whole array. `ImmutableList<T>` is the opposite profile — an AVL tree with structural sharing, O(log n) access and update, pointer-chasing everywhere.

Current uses: `SubjectSetRewrite.Union.Children`, `SubjectSetRewrite.Intersection.Children`, `Namespace.Relationships` (and `Result<T>.Failure.Errors` in the Results project, same pattern).

## Interpretation

Domain values in Kingo are read-optimized by construction. "Mutation" in this design means the Write service constructs a whole new `Namespace` value and persists it as the next version — nothing ever edits a spec definition in place. Downstream (Check, Expand, Read) only ever traverses. That is exactly `ImmutableArray`'s sweet spot; `ImmutableList`'s structural sharing would pay its overhead for update operations that never happen.

Two caveats that ride along with the choice:

- **Custom structural equality is mandatory.** The struct's default equality compares the inner array *reference*, so records carrying an `ImmutableArray` override `Equals`/`GetHashCode` with span-based `SequenceEqual` (see `SubjectSetRewrite.Union`, `Namespace`, `Result<T>.Failure`). A new record carrying an `ImmutableArray` without the override is a defect.
- **`default(ImmutableArray<T>)` wraps a null array** and throws on use — same trash-value class as `default(Error)`, minus the fail-loud treatment. Non-issue while construction flows through primary constructors, but worth remembering at deserialization boundaries.

On `Namespace.Relationships` specifically, the array is a deliberate *document-shaped* choice: it preserves authored order (SDL round-trip fidelity) and gives cheap order-sensitive structural equality. The two gaps it leaves are intentional deferrals, not oversights:

- Duplicate relationship names are unrepresentable: `Namespace.Create` is the only construction path (ctor private, 2026-07-14) and rejects them with one `Validation` error per duplicated name.
- Keyed lookup (`RelationshipPath → SubjectSetRewrite`) is the interpreters' concern, not the model's: the Check host compiles a `Namespace` into its own read-side form (e.g. `FrozenDictionary` — built for the build-once/read-forever profile). Write-side-vs-read-side projection applied one level down.

If incremental spec editing ever becomes a real workflow, builder/`ImmutableList` machinery belongs inside the Write context, converting to the flat array when it mints the final value. The domain type stays read-shaped.

## Next

- Enforce the pattern when tests land: an ArchUnit-style check (or reviewer grep) that records carrying `ImmutableArray` override `Equals`/`GetHashCode`.

## Related

- [[domain-language]] — the types these collections live in.
- [[four-service-split-by-load-profile]] — why read-side compiled forms (FrozenDictionary) live in the hosts, not the model.
