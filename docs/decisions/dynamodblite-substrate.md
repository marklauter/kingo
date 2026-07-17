---
title: DynamoDbLite as the storage substrate
summary: "Code Kingo against AWSSDK.DynamoDBv2 and use DynamoDbLite (SQLite-backed) locally — the local-vs-prod switch is a client-construction choice with no port. Settled 2026-07-14: DynamoDbLite is production-ready and storage access uses the key/value store style (low-level PK/SK items, not the DynamoDBContext ORM)."
tags: [decision, storage]
created: 2026-05-12
status: locked
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
- **Schema administration** — separate from storage. The YAML PDL parser on the `dictionary-encoding` quarry is the closest existing work.

## Settled 2026-07-14

- **DynamoDbLite is production-ready** (Mark's call as its author) — the previously recommended go/no-go spike is moot; storage work proceeds directly against the substrate.
- **Key/value store style, not the ORM.** Storage access uses the low-level (PK, SK) item operations with hand-mapped `Dictionary<string, AttributeValue>` in the storage adapter — not `DynamoDBContext` with `[DynamoDBHashKey]`-attributed POCOs. This keeps DynamoDB attributes off every record in the system (persistence-ignorance all the way down: the earlier ORM preference would have required attribute-carrying storage POCOs) and gives the adapter direct control of the item shape the Zanzibar mapping table above depends on.

## Caveats

- **API ergonomics.** The AWS SDK reads verbosely, and the key/value style more so. Plan on a thin Kingo-shaped facade behind a port (`IStatementStore.Read(subjectSet)`-shaped) rather than scattering `PutItemAsync` through the evaluator; the hand-mapping lives entirely inside that adapter.
- **Cost at scale.** Pay-per-request on hot ACL lookups gets expensive. Not a near-term concern for Kingo.
- **Out of scope in DynamoDbLite** (intentionally): backup & PITR, Global Tables, Kinesis streaming, PartiQL, resource policies. None of these matter for Kingo's design.

## References

- NuGet: `MSL.DynamoDbLite` (current: `0.0.0`)
- Project: `D:\dynamodblite\DynamoDbLite\`
- Architecture: `D:\dynamodblite\DynamoDbLite\docs\architecture-decisions.md`
- API parity matrix: https://github.com/marklauter/DynamoDbLite/wiki/API-Parity
- Storage schema: https://github.com/marklauter/DynamoDbLite/wiki/Storage-Architecture
