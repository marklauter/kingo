# ImmutableArray for domain collections

Tags: decision,ddd,performance
Domain values carry their collections as `ImmutableArray<T>`: policy values are build-once/read-many snapshots, and mutation never touches these types — the Write side constructs whole new values.

## Observation

`ImmutableArray<T>` is a thin struct wrapper around a plain `T[]` — contiguous memory, O(1) index, allocation-free enumeration, zero per-element overhead. Its trade is on mutation: every "update" copies the whole array. `ImmutableList<T>` is the opposite profile — an AVL tree with structural sharing, O(log n) access and update, pointer-chasing everywhere.

Current uses: `UnionRewrite.Children`, `IntersectionRewrite.Children`, `Namespace.Relationships` (and `Result<T>.Failure.Errors` in the Results project, same pattern).

## Interpretation

Domain values in Kingo are read-optimized by construction. "Mutation" in this design means the Write service constructs a whole new `Namespace` value and persists it as the next version — nothing ever edits a policy definition in place. Downstream (Check, Expand, Read) only ever traverses. That is exactly `ImmutableArray`'s sweet spot; `ImmutableList`'s structural sharing would pay its overhead for update operations that never happen.

Two caveats that ride along with the choice:

- **Custom structural equality is mandatory.** The struct's default equality compares the inner array *reference*, so records carrying an `ImmutableArray` override `Equals`/`GetHashCode` with span-based `SequenceEqual` (see `UnionRewrite`, `Namespace`, `Result<T>.Failure`). A new record carrying an `ImmutableArray` without the override is a defect.
- **`default(ImmutableArray<T>)` wraps a null array** and throws on use — same trash-value class as `default(Error)`, minus the fail-loud treatment. Non-issue while construction flows through primary constructors, but worth remembering at deserialization boundaries.

On `Namespace.Relationships` specifically, the array is a deliberate *document-shaped* choice: it preserves authored order (PDL round-trip fidelity) and gives cheap order-sensitive structural equality. The two gaps it leaves are intentional deferrals, not oversights:

- Duplicate relationship names are representable; the invariant lands in a `Result`-returning factory when Write validation is built.
- Keyed lookup (`RelationshipIdentifier → SubjectSetRewrite`) is the interpreters' concern, not the model's: the Check host compiles a `Namespace` into its own read-side form (e.g. `FrozenDictionary` — built for the build-once/read-forever profile). Write-side-vs-read-side projection applied one level down.

If incremental policy editing ever becomes a real workflow, builder/`ImmutableList` machinery belongs inside the PAP/Write context, converting to the flat array when it mints the final value. The domain type stays read-shaped.

## Next

- Enforce the pattern when tests land: an ArchUnit-style check (or reviewer grep) that records carrying `ImmutableArray` override `Equals`/`GetHashCode`.
- Duplicate-relationship-name validation arrives with the Write service's namespace-config validation.

## Related

- [domain-language](domain-language.md) — the types these collections live in.
- [four-service-split-by-load-profile](four-service-split-by-load-profile.md) — why read-side compiled forms (FrozenDictionary) live in the hosts, not the model.
