---
title: Precomputed closure index for the hot path
type: todo
summary: "Contains walks the graph on every check, so a deep rewrite tree costs many fact reads per decision. Precompute closures into an index the Check host reads instead of walking. Deferred: the interpreters and the store do not exist yet, and invalidation is the unsolved part."
tags: [closures, performance]
created: 2026-08-08
priority: medium
status: deferred
blocked-by:
  - "[[storage-versioning-design]]"
  - "[[implement-contains-and-expand]]"
---

# Precomputed closure index for the hot path

Contains judges membership by walking: it reads facts, discovers which rewrites apply, and evaluates the assembled expression. The walk runs per check, so a subjectset whose rewrites nest deeply costs many fact reads for one decision. An index that materializes closures ahead of time turns that walk into a lookup.

The index belongs to the Check host and nowhere else. Caching, hedging, and hot-spot handling are already scoped there. A read-side compiled form of a write-side value is host-level projection.

## What acting on it needs

- **Invalidation is the design.** A closure is derived from the graph under the catalog at one snapshot, so an index entry is pinned to a Kookie. A single fact write invalidates every entry derived through that fact, and finding those entries is the reverse of the derivation the index exists to avoid. Solve this before anything else.
- **The trigger is measurement.** Build it when check latency on deep rewrite trees is measured and unacceptable. Nothing establishes that cost today, because neither interpreter nor store exists.
- **The artifact needs a term if it becomes real.** `closure` and `contains` are locked; the index is a third thing and unnamed. "Closure index" is a placeholder, not a ruling.
- **Both blockers are structural.** The index derives from facts at a snapshot, so it cannot be designed before the snapshot mechanism is, and it optimizes a walk nobody has written.
