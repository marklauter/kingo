---
type: decision
title: DynamoDbLite as the storage substrate
summary: "Rather than hand-roll a key-value store on SQLite, code Kingo against AWSSDK.DynamoDBv2 and use DynamoDbLite (SQLite-backed) locally — the local-vs-prod switch is a client-construction choice with no port. Spike pending before domain code commits."
tags: [note, decision, storage]
created: 2026-05-12
status: evolving
---

# DynamoDbLite as the storage substrate

[DynamoDbLite](https://github.com/marklauter/DynamoDbLite) is a SQLite-backed implementation of the AWS DynamoDB v2 SDK. The decision: rather than hand-roll a key-value store on SQLite (the `dictionary-encoding` quarry approach), code Kingo against `AWSSDK.DynamoDBv2` and use DynamoDbLite locally. The local-vs-prod switch becomes a client-construction choice with no port.

## Why it fits

Zanzibar's data model is essentially specified in DynamoDB-shaped primitives. The mapping is direct enough that very little adapter work is required.

| Zanzibar / Kingo concept | DynamoDB primitive |
|---|---|
| `(object#relation, subject)` tuples | `(PK, SK)` items |
| `DocumentWriter` version-conflict CAS | `[DynamoDBVersion]` optimistic locking |
| Atomic multi-tuple writes | `TransactWriteItems` |
| Reverse index ("what can user X see?") | GSI on `subject` |
| Watch API / change feed | DynamoDB Streams |
| MVCC header + journal split | Items + separate journal table |
| Range scan in `TupleToSubjectSetRewrite` | `Query` with `KeyConditionExpression` |

Zanzibar production runs on Spanner, but the abstract model is a partition+sort key store with conditional writes — DynamoDB's exact shape.

## What this dissolves from earlier production-gap reviews

Several gaps called out in [[sources]] and earlier reviews collapse the moment Kingo's domain sits on top of DynamoDB:

- **Persistence** — substrate handles it
- **Watch API** — DynamoDB Streams
- **Reverse index** — GSI
- **Atomic multi-tuple writes** — `TransactWriteItems`
- **Optimistic locking** — `[DynamoDBVersion]` natively
- **Pagination** — SDK handles it

## What still has to be solved at the application layer

DynamoDb-as-substrate is neutral on the genuinely hard Zanzibar-specific problems:

- **Zookies / external consistency** — needs an app-layer commit-timestamp protocol. No store gives you this for free.
- **Leopard-style set-fold caching** — application concern.
- **Policy authoring (PAP)** — separate from storage. The YAML PDL parser on the `dictionary-encoding` quarry is the closest existing work.

## Caveats

- **Adapter cost.** Domain types need to become DynamoDB-shaped. Prefer the high-level `DynamoDBContext` ORM with `[DynamoDBHashKey] / [DynamoDBRangeKey] / [DynamoDBVersion]` attributes on POCOs over hand-mapping `Dictionary<string, AttributeValue>`. DynamoDbLite's Phase 12 validated the ORM path with 50+ tests.
- **API ergonomics.** The AWS SDK reads verbosely. Plan on a thin Kingo-shaped facade (`IAclRepository.Get(subjectSet, subject)`) rather than scattering `PutItemAsync` through the evaluator.
- **Phase 14 parity tests are scaffolding only.** Behavioral drift between DynamoDbLite and real AWS DynamoDB has not been end-to-end validated. For a study project this is fine; before committing prod traffic, finishing parity tests is the gate.
- **Cost at scale.** Pay-per-request on hot ACL lookups gets expensive. Not a near-term concern for Kingo.
- **Out of scope in DynamoDbLite** (intentionally): backup & PITR, Global Tables, Kinesis streaming, PartiQL, resource policies. None of these matter for Kingo's design.

## Recommended first spike

Before any domain code commits to the substrate:

1. Add the NuGet package (and its transitive `AWSSDK.DynamoDBv2` dependency) to a spike project — or directly to `Kingo.Tests` for the smoke test: `dotnet add package MSL.DynamoDbLite --version 0.0.0`.
2. Define one POCO — e.g. `AclTupleRecord` with `[DynamoDBHashKey]` on the encoded `(object#relation)`, `[DynamoDBRangeKey]` on the subject, `[DynamoDBVersion]` on a `Version` long.
3. Run a write-then-read smoke test through `DynamoDBContext` using DynamoDbLite's in-memory store.

That's a ~1-hour spike with a real go/no-go signal: does the substrate behave the way Kingo needs it to, and does the ORM mapping cover the domain shape without contortion.

## References

- NuGet: `MSL.DynamoDbLite` (current: `0.0.0`)
- Project: `D:\dynamodblite\DynamoDbLite\`
- Architecture: `D:\dynamodblite\DynamoDbLite\docs\architecture-decisions.md`
- API parity matrix: https://github.com/marklauter/DynamoDbLite/wiki/API-Parity
- Storage schema: https://github.com/marklauter/DynamoDbLite/wiki/Storage-Architecture
