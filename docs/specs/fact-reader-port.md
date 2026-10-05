---
title: fact-reader-port
type: specification
summary: "IFactReader is the only port the contains and expand interpreters use. It reads the facts stored under a subjectset at a pinned snapshot. Absence comes back as the empty set, and the result fails only when the snapshot could not be consulted."
tags: [ports]
status: evolving
cites:
  - "[[fact]]"
  - "[[subjectset]]"
  - "[[subject]]"
  - "[[identity]]"
  - "[[kookie]]"
  - "[[snapshot]]"
  - "[[contains]]"
  - "[[expand]]"
  - "[[kleene-absorption]]"
  - "[[grouping-the-apis-into-services]]"
  - "[[implement-contains-and-expand]]"
---

# The fact-reader port

`IFactReader` (`Kingo.Closures`) is how the domain declares fact lookup, and it is the only I/O either interpreter performs. It reaches the [[contains]] and [[expand]] evaluators already pinned to a [[snapshot]]. The pin is exposed as a `Kookie` property, and the interpreters copy that [[kookie]] into every result value without interpreting it. Two adapters implement it: Check's cached and hedged one, and Read+Expand's plain one ([[grouping-the-apis-into-services]]).

## Operations

The port exposes one verb in two overloads, plus the pin. Both answer the same question, which [[fact]]s are stored under this [[subjectset]], and both return the same shape:

```csharp
ValueTask<Result<ImmutableArray<Fact>>> Read(SubjectSet subjectSet, CancellationToken cancellationToken);
ValueTask<Result<ImmutableArray<Fact>>> Read(SubjectSet subjectSet, Identity identity, CancellationToken cancellationToken);
Kookie Kookie { get; }
```

- The wide overload returns every stored fact whose left-hand side is `subjectSet`, including every [[subject]] shape. The interpreters raise their error conditions by *meeting* a wrong-shaped member, so the port never filters.
- The narrowed overload returns the zero-or-one `Fact.SubjectFact` whose subject is the given [[identity]]. That is the direct-match point question, and it carries the `(subjectset, identity, snapshot)` cache key. A miss does not settle direct membership. `this` also admits the members of subjectset-valued facts stored under the same subjectset, so a miss still obliges the wide read.

The two overloads cover two storage access patterns: point lookup and range read. [[fact-store-access-patterns]] records both, along with the patterns the port doesn't expose: shape-filtered range and integer-encoded keys.

## Absence is the empty set

Both overloads answer absence with the empty set. A miss is the ordinary input to a false verdict, and to every walk that continues into member expansion. The port has no not-found error. A not-found failure would put expected control flow on the error channel, where `Bind` short-circuits exactly when the walk must continue.

## Failure means the snapshot could not be consulted

The `Result` fails only for the interpreter error taxonomy's context-broke family ([[implement-contains-and-expand]]):

- **Fact lookup failed** — the adapter's I/O broke and stayed broken after its own retries.
- **Snapshot unavailable** — the pin points past the store's retention horizon.

Substrate exceptions are translated to these values *at the port*, never propagated. The interpreters' [[kleene-absorption]] treats "operand unavailable" as a third truth value that an absorbing operand can dominate, and an exception cannot be absorbed. A throwing port would make verdicts depend on evaluation order. Cancellation and bugs are outside the algebra and still throw.

## Async shape

The port is shaped for its slowest adapter, the network read, so it returns `ValueTask`. The cached adapter completes synchronously and pays no `Task` allocation per fact read on the hot path. `CancellationToken` threads through the port; cancellation surfaces as an exception at the host edge, never as a modeled error.

## Open question: a failed fact lookup has no category

*Snapshot unavailable* lands on `ErrorType.Gone`: the pin existed and the retention horizon took it. *Fact lookup failed* has nowhere to land. `NotFound` does not fit, because absence is the empty set and the port has no not-found error left to name. `Validation` and `Conflict` describe the caller's input. `Undefined` is documented as a bug rather than a domain outcome, and a transient I/O break is not a bug.

The larger question is whether a failed lookup stays a value. The rule elsewhere is that domain failure returns and substrate failure throws. A broken adapter is a substrate failure. A failed lookup is a value anyway, because [[kleene-absorption]] needs an unavailable operand to be dominated by an absorbing one. Under `(this | (parent, viewer)) ! banned`, a throwing `banned` read cannot be absorbed by an operand that already decides the answer, so the verdict would depend on which operand ran first.

- Keep it a value. `ErrorType` gains a category for unavailability, and the substrate-throws rule gains a documented exemption at this port.
- Let it throw. Absorption of an unavailable operand goes with it, and two rulings are rewritten: this document's translation of substrate exceptions at the port, and [[implement-contains-and-expand]]'s "fact lookup failed and snapshot unavailable are the port's own error values."
