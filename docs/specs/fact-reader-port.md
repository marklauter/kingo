---
title: fact-reader-port
summary: "IFactReader — the interpreters' one port: snapshot-pinned reads of the facts stored under a subject set, absence as the empty set, failure only when the snapshot could not be consulted."
tags: [spec, acl, ports]
created: 2026-07-21
status: locked
cites:
  - "[[fact]]"
  - "[[subject-set]]"
  - "[[subject]]"
  - "[[kookie]]"
  - "[[contains]]"
  - "[[expand]]"
  - "[[kleene-absorption]]"
---

# The fact-reader port

`IFactReader` (`Kingo.Closures`) is the only I/O either interpreter performs — the domain's own declaration of fact lookup, drawn in domain vocabulary. It arrives at the [[contains]] and [[expand]] evaluator already snapshot-pinned; the pin is exposed as a `Kookie` property, the [[kookie]] copied into every result value without interpretation. Two adapters by design: Check's cached, hedged one and Read+Expand's plain one ([[four-service-split-by-load-profile]]).

## Operations

One verb, narrowed by overload. Both overloads answer the set-shaped question "which [[fact]]s are stored under this [[subject-set]]?" and return the same shape:

```csharp
ValueTask<Result<ImmutableArray<Fact>>> Read(SubjectSet subjectSet, CancellationToken cancellationToken);
ValueTask<Result<ImmutableArray<Fact>>> Read(SubjectSet subjectSet, SubjectIdentifier member, CancellationToken cancellationToken);
Kookie Kookie { get; }
```

- The wide overload returns every stored fact whose left-hand side is `subjectSet`, all member shapes included — the interpreters' error conditions require *meeting* wrong-shaped members, so the port never filters.
- The narrowed overload returns the zero-or-one `Fact.SubjectFact` whose member is the given [[subject]] identifier — the direct-match point question, and the home of the `(subject set, member, snapshot)` cache key.

The two overloads are one port concept covering two storage access patterns, point lookup and range read. Both are recorded distinctly in [[fact-store-access-patterns]], which also holds the deferred patterns (shape-filtered range, integer-encoded keys) the port doesn't expose.

## Absence is a value, not an error

A miss is the ordinary input to a false verdict and to every walk that continues into member expansion, so both overloads answer absence with the empty set. The port has no not-found error; a not-found failure would put expected control flow on the error channel, where `Bind` short-circuits exactly when the walk must continue.

## Failure means the snapshot could not be consulted

The `Result` fails only for the interpreter error taxonomy's family 3 ([[rewrite-interpreters]]):

- **Fact lookup failed** — the adapter's I/O broke and stayed broken after its own retries.
- **Snapshot unavailable** — the pin points past the store's retention horizon.

Substrate exceptions are translated to these values *at the port*, not propagated: the interpreters' [[kleene-absorption]] treats "operand unavailable" as a third truth value that an absorbing operand can dominate, and an exception cannot be absorbed — a throwing port would make verdicts depend on evaluation order. Cancellation and bugs have no seat in the algebra and still throw.

## Shape

`ValueTask` because the port is shaped for its worst legitimate adapter, the network read, while the cached adapter completes synchronously and must not pay a `Task` allocation per fact read on the hot path. `CancellationToken` threads through as mechanical plumbing; cancellation surfaces as an exception at the host edge, never as a modeled error.
