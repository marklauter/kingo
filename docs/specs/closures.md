---
title: closures
type: specification
summary: "A Closure is one derived closure assembled per pin: the prepared rewrites, the snapshot-pinned fact reader, the clock, and Contains's depth bound. Constructor state, never per-call arguments."
tags: [evaluation]
created: 2026-07-27
status: evolving
cites:
  - "[[closure]]"
  - "[[contains]]"
  - "[[expand]]"
  - "[[decision]]"
  - "[[expansion]]"
  - "[[catalog]]"
  - "[[theory]]"
  - "[[kookie]]"
  - "[[fact-reader-port]]"
  - "[[rewrite-interpreters]]"
---

# Closures

A [[closure]] is the set of subjects derivable for a subjectset from the stored facts and the catalog's rewrites. `Closure` (`Kingo.Closures`) names one such set as a value, fixed by a pin, carrying [[contains]] and [[expand]] as the two questions asked of it. Contains judges one membership; Expand materializes one level of the tree.

## Assembled per pin

`Closure.Create(prepared, IFactReader, TimeProvider, depthBound)`. Everything an evaluation needs besides its question is constructor state, never a per-call argument:

- **The prepared rewrites** — the lookup-optimized projection that resolves names to probes and rewrites to direct references, the way a database prepares a statement. Immutable and opaque; its only consumer is `Closure`, and none of its internals appear in a signature. What it spans is open below.
- **The fact reader** — arrives already snapshot-pinned ([[fact-reader-port]]), so execution never sees a [[kookie]]. The one stamped into every result is read off the reader's property.
- **The clock** — an injected `TimeProvider`, supplying the wall timestamp on [[decision]] and [[expansion]].
- **The depth bound** — `Contains`-only configuration. `Expand` never recurses and ignores it.

The two pins move at different rates: rewrites change on admin action, the fact snapshot per request. The prepared projection is shared, and a fresh reader is assembled per pin. Everything inside is immutable, so requests at the same pin may share one instance.

## Open question: what the prepared projection spans

A closure is defined over the [[catalog]]'s rewrites, and [[catalog]] holds that a reference resolves against the catalog rather than the [[theory]] that wrote it. A walk leaves its theory by following facts, and a fully-qualified member in another theory still needs its rewrite to continue, so a projection covering one theory cannot serve the lookup.

Version pulls the other way. Each theory carries its own and is the unit of atomic change, so a catalog-spanning projection has no single version to key on, and the port serving it cannot be keyed on `TheoryVersion` alone. The same tension reaches [[rewrite-interpreters]], where a [[decision]] carries one opaque `TheoryVersion`: a walk that crosses theories leaves that slot under-specified.

## Open question: the factory's shape

A `ClosureFactory` holds the long-lived context (the reader for prepared rewrites, the `TimeProvider`, the depth bound) and assembles a `Closure` per request. Unsettled: whether `Create` receives an already-pinned `IFactReader` from the host edge, or takes the pin and pins the reader itself through a third port. The second moves mechanical pinning off the host edge, which keeps the semantic work either way — resolving the request's kookie floor to a coherent pin ([[drift-prevention-at-the-write-edges]], [[storage-versioning-design]]).
