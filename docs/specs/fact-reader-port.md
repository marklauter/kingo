---
title: fact-reader-port
type: specification
summary: "IFactReader — the interpreters' one port: snapshot-pinned reads of the facts stored under a subjectset, absence as the empty set, failure only when the snapshot could not be consulted."
tags: [ports]
created: 2026-07-26
status: evolving
cites:
  - "[[fact]]"
  - "[[subjectset]]"
  - "[[subject]]"
  - "[[kookie]]"
  - "[[contains]]"
  - "[[expand]]"
  - "[[kleene-absorption]]"
  - "[[four-service-split-by-load-profile]]"
  - "[[rewrite-interpreters]]"
---

# The fact-reader port

`IFactReader` (`Kingo.Closures`) is the domain's own declaration of fact lookup, and the only I/O either interpreter performs. It arrives at the [[contains]] and [[expand]] evaluators already snapshot-pinned. The pin is exposed as a `Kookie` property, the [[kookie]] copied into every result value without interpretation. Two adapters by design: Check's cached, hedged one and Read+Expand's plain one ([[four-service-split-by-load-profile]]).

## Operations

One verb, narrowed by overload, plus the pin. Both overloads answer the set-shaped question "which [[fact]]s are stored under this [[subjectset]]?" and return the same shape:

```csharp
ValueTask<Result<ImmutableArray<Fact>>> Read(SubjectSet subjectSet, CancellationToken cancellationToken);
ValueTask<Result<ImmutableArray<Fact>>> Read(SubjectSet subjectSet, SubjectId member, CancellationToken cancellationToken);
Kookie Kookie { get; }
```

- The wide overload returns every stored fact whose left-hand side is `subjectSet`, all member shapes included — the interpreters' error conditions require *meeting* wrong-shaped members, so the port never filters.
- The narrowed overload returns the zero-or-one `Fact.SubjectFact` whose member is the given [[subject]] id — the direct-match point question, and the home of the `(subjectset, member, snapshot)` cache key. A miss does not settle direct membership: `this` also admits the members of subjectset-valued facts stored under the same subjectset, so a miss still obliges the wide read.

The two overloads cover two storage access patterns, point lookup and range read. [[fact-store-access-patterns]] records both, along with the patterns the port doesn't expose: shape-filtered range and integer-encoded keys.

## Absence is the empty set

Both overloads answer absence with the empty set. A miss is the ordinary input to a false verdict, and to every walk that continues into member expansion. The port has no not-found error; a not-found failure would put expected control flow on the error channel, where `Bind` short-circuits exactly when the walk must continue.

## Failure means the snapshot could not be consulted

The `Result` fails only for the interpreter error taxonomy's family 3 ([[rewrite-interpreters]]):

- **Fact lookup failed** — the adapter's I/O broke and stayed broken after its own retries.
- **Snapshot unavailable** — the pin points past the store's retention horizon.

Substrate exceptions are translated to these values *at the port*, never propagated. The interpreters' [[kleene-absorption]] treats "operand unavailable" as a third truth value that an absorbing operand can dominate, and an exception cannot be absorbed. A throwing port would make verdicts depend on evaluation order. Cancellation and bugs have no seat in the algebra and still throw.

## Async shape

The port is shaped for its worst legitimate adapter, the network read, so it returns `ValueTask`. The cached adapter completes synchronously and pays no `Task` allocation per fact read on the hot path. `CancellationToken` threads through the port; cancellation surfaces as an exception at the host edge, never as a modeled error.

## Open question: a failed fact lookup has no category

Snapshot unavailable lands on `ErrorType.Gone`: the pin existed and the retention horizon took it. Fact lookup failed has nowhere to land. `NotFound` does not fit, because absence is the empty set and the port has no not-found error left to name. `Validation` and `Conflict` describe the caller's input. `Undefined` is documented as a bug rather than a domain outcome, and a transient I/O break is not a bug.

The larger question is whether a failed lookup stays a value at all. The rule elsewhere is that domain failure returns and substrate failure throws. A broken adapter is a substrate failure. It is a value here because [[kleene-absorption]] needs an unavailable operand to be dominated by an absorbing one. Under `(this | (parent, viewer)) ! banned`, a throwing `banned` read cannot be absorbed by an operand that already decides the answer, so the verdict would depend on which operand ran first.

- Keep it a value. `ErrorType` gains a category for unavailability, and the substrate-throws rule carries a named exception at this port.
- Let it throw. Absorption of an unavailable operand goes with it, and two rulings are rewritten: this document's translation of substrate exceptions at the port, and [[rewrite-interpreters]]'s "7–8 are the port's own error values."
