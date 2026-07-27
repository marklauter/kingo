---
title: closures
type: specification
summary: "A Closure is one derived closure, fixed by a theory version and a snapshot. Its prepared rewrites, pinned fact reader, clock, and depth bound are constructor state, never per-call arguments."
tags: [evaluation]
created: 2026-07-27
status: evolving
cites:
  - "[[closure]]"
  - "[[subjectset]]"
  - "[[rewrite]]"
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

A [[closure]] is the set of subjects derivable for a [[subjectset]] from the stored facts and the catalog's rewrites. `Closure` (`Kingo.Closures`) names one such set as a value, fixed by a theory version and a snapshot, carrying [[contains]] and [[expand]] as the two questions asked of it. Contains judges one membership; Expand materializes one relation's [[rewrite]] tree, leaving referenced subjectsets as leaves.

## The assembly

`Closure.Create(prepared, factReader, timeProvider, depthBound)`. Everything an evaluation needs besides its question is constructor state, never a per-call argument:

- **The prepared rewrites** — the lookup-optimized projection that resolves names to probes and rewrites to direct references, the way a database prepares a statement. Immutable and opaque; its only consumer is `Closure`, and none of its internals appear in a signature. What it spans is open below.
- **The fact reader** — arrives already snapshot-pinned ([[fact-reader-port]]), so execution never sees a [[kookie]]. The kookie stamped into every result is read off the reader's property.
- **The clock** — an injected `TimeProvider`, supplying the wall timestamp on [[decision]] and [[expansion]].
- **The depth bound** — `Contains`-only configuration. `Expand` never recurses and ignores it.

The two move at different rates: rewrites change on admin action, the snapshot per request. The prepared projection is shared, and a fresh reader is assembled per snapshot. Everything inside is immutable, so requests at the same (`TheoryVersion`, `Kookie`) pair may share one instance.

## Open question: what the prepared projection spans

A closure is defined over the [[catalog]]'s rewrites, and [[catalog]] holds that a reference resolves against the catalog rather than the [[theory]] that wrote it. A walk leaves its theory by following facts. A fully-qualified member in another theory still needs its rewrite to continue, so a projection covering one theory cannot serve the lookup.

Version pulls the other way. Each theory carries its own and is the unit of atomic change, so a catalog-spanning projection has no single version to key on. The port serving it cannot be keyed on `TheoryVersion` alone. The same tension reaches [[rewrite-interpreters]], where a [[decision]] carries one opaque `TheoryVersion`: a walk that crosses theories leaves that slot under-specified.

## Open question: the factory's shape

A `ClosureFactory` holds the long-lived context (the reader for prepared rewrites, the `TimeProvider`, the depth bound) and assembles a `Closure` per request. Unsettled: whether `Create` receives an already-pinned `IFactReader` from the host edge, or takes the pair and pins the reader itself through a third port. The second moves mechanical pinning off the host edge. The host edge keeps the semantic work either way — resolving the request's kookie floor to a coherent (`Kookie`, `TheoryVersion`) pair ([[drift-prevention-at-the-write-edges]], [[storage-versioning-design]]).
