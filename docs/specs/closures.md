---
title: closures
type: specification
summary: "A Closure is one derived closure, fixed by one pin. Its prepared rewrites, pinned fact reader, clock, and depth bound are constructor state, never per-call arguments."
tags: [evaluation]
status: evolving
cites:
  - "[[closure]]"
  - "[[graph]]"
  - "[[subjectset]]"
  - "[[rewrite]]"
  - "[[contains]]"
  - "[[expand]]"
  - "[[decision]]"
  - "[[expansion]]"
  - "[[glossary/catalog]]"
  - "[[specs/catalog]]"
  - "[[theory]]"
  - "[[kookie]]"
  - "[[fact-reader-port]]"
  - "[[implement-contains-and-expand]]"
---

# Closures

A [[closure]] is the set of subjects derivable for a [[subjectset]] from the [[graph]] under the catalog. `Closure` (`Kingo.Closures`) names one such set as a value, fixed by one pin, carrying [[contains]] and [[expand]] as the two questions asked of it. Contains judges one membership; Expand materializes one subjectset's [[rewrite]] tree, leaving referenced subjectsets as leaves.

## The assembly

`Closure.Create(prepared, factReader, timeProvider, depthBound)`. Everything an evaluation needs besides its question is constructor state, never a per-call argument:

- **The prepared rewrites** — the lookup-optimized projection that resolves names to probes and rewrites to direct references, the way a database prepares a statement. Immutable and opaque; its only consumer is `Closure`, and none of its internals appear in a signature. What it spans is open below.
- **The fact reader** — arrives already snapshot-pinned ([[fact-reader-port]]), so execution never sees a [[kookie]]. The kookie stamped into every result is read off the reader's property.
- **The clock** — an injected `TimeProvider`, supplying the wall timestamp on [[decision]] and [[expansion]].
- **The depth bound** — `Contains`-only configuration. `Expand` never recurses and ignores it.

There is one pin. A [[kookie]] names one snapshot on the store's one timeline. The changelog versions each theory on its own, and the kookie selects one version of each; that selection is the catalog snapshot ([[specs/catalog]], [[storage-versioning-design]]). What differs is churn: the theory changelog advances on admin action, fact intervals on every write. So one prepared projection serves many closures while the reader is pinned per request, and requests at the same kookie may share one instance, everything inside being immutable.

## The prepared projection spans the catalog

A closure is defined over the catalog's rewrites, and [[specs/catalog]] holds that a reference resolves against the catalog rather than the [[theory]] that wrote it. A walk leaves its theory by following facts, and a fully-qualified member in another theory still needs its rewrite to continue, so the projection covers the catalog at the pin. It is keyed on the kookie, not on any theory's version, and a [[decision]] carries the kookie alone (ruled 2026-09-08, [[implement-contains-and-expand]]).

## Open question: the factory's shape

A `ClosureFactory` holds the long-lived context (the reader for prepared rewrites, the `TimeProvider`, the depth bound) and assembles a `Closure` per request. Unsettled: whether `Create` receives an already-pinned `IFactReader` from the host edge, or takes the pin and pins the reader itself through a third port. The second moves mechanical pinning off the host edge. The host edge keeps the semantic work either way — resolving the request's kookie floor to a coherent (`Kookie`, `TheoryVersion`) pair ([[preventing-drift-between-facts-and-theories]], [[storage-versioning-design]]).
