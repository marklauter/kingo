---
title: evaluation-context
summary: "Closure — the evaluator both interpreters live on: a per-pin assembly of PreparedSchema (the lookup-optimized projection of one schema version, served by the ISchemaReader port), the pinned fact reader, the clock, and Contains's depth bound."
tags: [spec, acl, evaluation]
created: 2026-07-21
status: locked
cites:
  - "[[closure]]"
  - "[[contains]]"
  - "[[expand]]"
  - "[[schema]]"
  - "[[kookie]]"
  - "[[depth-bound]]"
---

# The evaluation context

The evaluation context is everything an evaluation needs besides its question: the schema version to interpret rewrites under, the snapshot-pinned facts, the clock, and [[contains]]'s [[depth-bound]]. It is constructor state, never per-call arguments. The evaluation bounded context (`Kingo.Closures`) is two ports, a prepared projection, and the `Closure` as the unit of execution.

## PreparedSchema — one schema version, ready to execute

`PreparedSchema.Create(Schema, SchemaVersion)` prepares one [[schema]] value for evaluation, the way a database prepares a statement: names resolve to O(1) probes, rewrites to direct references, the `SchemaVersion` riding along. Immutable and opaque; its only consumer is `Closure`.

The `Schema` aggregate stays the source of truth and stays construction-shaped. The projection is the evaluator's private read-model, per the [[domain-language]] guardrail that read-models live in consumers, never as domain values. Nothing about its internals (integer-indexed names, frozen dictionaries) appears in any signature.

## ISchemaReader — the schema port

The evaluation context declares its schema need as its second port, beside [[fact-reader-port]]:

```csharp
ValueTask<Result<PreparedSchema>> Read(SchemaVersion schemaVersion, CancellationToken cancellationToken);
```

The port declares the need; how it is served is the adapter's concern. A hot-path adapter caches `PreparedSchema` per version (preparation front-loads its cost, and schema versions change on admin action, not per request); a test fake returns a fixture. Caching is invisible through the port, the same way integer-encoded fact storage is invisible through `IFactReader`. Whether `SchemaVersion` alone keys a schema or needs `SchemaIdentifier` beside it depends on the version encoding, which is [[storage-versioning-design]]'s open work.

## Closure — assembled per pin

`Closure.Create(PreparedSchema, IFactReader, TimeProvider, depthBound)` is the evaluator: one derived [[closure]], determined by one schema version and one snapshot, carrying `Contains` and [[expand]] as its operations. The reader arrives already pinned ([[fact-reader-port]]), so execution never sees a [[kookie]]; the one stamped into results comes from the reader's property. The two pins move at different rates (the schema version on admin action, the fact snapshot per request), so the shared `PreparedSchema` is assembled with a fresh reader per pin. Everything inside is immutable: requests at the same (`SchemaVersion`, `Kookie`) may share one instance. The depth bound is `Contains`-only configuration; `Expand` never recurses and ignores it.

**Open question — the factory's shape.** A `ClosureFactory` holds the long-lived context (`ISchemaReader`, `TimeProvider`, depth bound) and assembles `Closure`s per request. Unsettled: whether `Create` receives an already-pinned `IFactReader` from the host edge, or takes `(SchemaVersion, Kookie)` and pins the reader itself through a third port (`IFactReaderSource.Pin`). The second shape moves mechanical pinning from the host edge into the factory (the edge keeps the semantic work: resolving the request's floor to a coherent pair) and its pin scope depends on the schema-as-scope decision.

## Routing happens before construction

A `Closure` is pinned to one (`Schema`, `SchemaVersion`) pair; its operations never take a schema identifier. Selecting the schema is the host edge's routing problem, two-dimensional: which schema (scope; how scope appears in requests is [[domain-language]]'s open item, not decided here) and which version (time; the request's kookie floor resolves through the store's one timeline to a coherent (`Kookie`, `SchemaVersion`) pair, per [[drift-prevention-at-the-write-edges]]). A single-schema installation degenerates to version routing only. Pinning the reader at the resolved kookie is the host edge's work too; the factory and the interpreter signatures never see a kookie or a schema identifier.
