---
title: Choosing the storage substrate
type: decision
summary: "Code Kingo against AWSSDK.DynamoDBv2 and use DynamoDbLite (SQLite-backed) locally — the local-vs-prod switch is a client-construction choice with no port. Settled 2026-07-14: DynamoDbLite is production-ready and storage access uses the key/value store style (low-level PK/SK items, not the DynamoDBContext ORM)."
tags: [storage]
created: 2026-05-12
status: evolving
---

# Choosing the storage substrate

[DynamoDbLite](https://github.com/marklauter/DynamoDbLite) is a SQLite-backed implementation of the AWS DynamoDB v2 SDK. The decision: code Kingo against `AWSSDK.DynamoDBv2` and use DynamoDbLite locally, rather than build a key-value store on SQLite by hand (the `dictionary-encoding` quarry approach). The local-vs-prod switch becomes a client-construction choice with no port.

## Why it fits

Zanzibar's data model is built from DynamoDB-shaped primitives. The mapping is direct, so the adapter stays thin.

| Zanzibar / Kingo concept | DynamoDB primitive |
|---|---|
| `(resource#relation, subject)` facts | `(PK, SK)` items |
| `DocumentWriter` version-conflict CAS | `[DynamoDBVersion]` optimistic locking |
| Atomic multi-fact writes | `TransactWriteItems` |
| Reverse index ("what can subject X see?") | GSI on `subject` |
| Watch API / change feed | DynamoDB Streams |
| MVCC header + journal split | Items + separate journal table |
| Range scan in `SubjectSetRewrite.FactToSubjectSet` | `Query` with `KeyConditionExpression` |

Zanzibar production runs on Spanner, but the abstract model is a partition+sort key store with conditional writes, which is DynamoDB's shape.

## What this removes from earlier production-gap reviews

Several gaps called out in earlier reviews go away once Kingo's domain sits on DynamoDB:

- **Persistence** — substrate handles it
- **Watch API** — DynamoDB Streams
- **Reverse index** — GSI
- **Atomic multi-fact writes** — `TransactWriteItems`
- **Optimistic locking** — `[DynamoDBVersion]` natively
- **Pagination** — SDK handles it

## What still has to be solved at the application layer

DynamoDB as substrate does not help with the Zanzibar-specific problems:

- **Kookies / external consistency** — needs an app-layer commit-timestamp protocol. No store provides it.
- **Leopard-style set-fold caching** — application concern.
- **Theory administration** — separate from storage. The YAML PDL parser on the `dictionary-encoding` quarry is the closest existing work.

## Settled 2026-07-14

- **DynamoDbLite is production-ready** (Mark's call as its author) — the go/no-go spike recommended earlier is no longer needed; storage work proceeds directly against the substrate.
- **Key/value store style, not the ORM.** Storage access uses the low-level (PK, SK) item operations with hand-mapped `Dictionary<string, AttributeValue>` in the storage adapter, not `DynamoDBContext` with `[DynamoDBHashKey]`-attributed POCOs. No record in the system carries a persistence attribute; the earlier ORM preference would have required attribute-carrying storage POCOs. The adapter gets direct control of the item shape the mapping table above depends on.

## Refined 2026-07-20 — the drift ruling (dry-run finding F8)

The decision stands; three rows of the table above gained detail:

- **Item shape.** Fact items are interval-stamped — created/tombstoned [[kookie]] attributes; delete is a tombstone write, a snapshot read filters at the pin, and GC advances a store-wide retention horizon. The flat `facts → (PK, SK) items` mapping now carries those attributes, and "MVCC header + journal split" is replaced by this model.
- **A second reverse access pattern.** Beyond the subject GSI, the theory-write guard needs a reverse existence query — do any live facts reference this namespace or relation. Cold path, theory-write time only.
- **Theory storage is first-class.** Theories live in the store as an append-only changelog of whole `Theory` values, versioned on the same timeline as facts (intervals close by supersession; whole-theory deletion is a terminal marker entry). "Theory administration — separate from storage" above still describes the admin workflow, not the storage artifact.

## Caveats

- **API ergonomics.** The AWS SDK is verbose to read, and the key/value style more so. Plan on a thin Kingo-shaped facade behind a port (`IStatementStore.Read(subjectSet)`-shaped) rather than scattering `PutItemAsync` through the evaluator; the hand-mapping lives entirely inside that adapter.
- **Cost at scale.** Pay-per-request on hot ACL lookups gets expensive. Not a near-term concern for Kingo.
- **Out of scope in DynamoDbLite** (on purpose): backup & PITR, Global Tables, Kinesis streaming, PartiQL, resource policies. None of these matter for Kingo's design.

## References

- NuGet: `MSL.DynamoDbLite` (current: `0.0.0`)
- Project: `D:\dynamodblite\DynamoDbLite\`
- Architecture: `D:\dynamodblite\DynamoDbLite\docs\architecture-decisions.md`
- API parity matrix: https://github.com/marklauter/DynamoDbLite/wiki/API-Parity
- Storage schema: https://github.com/marklauter/DynamoDbLite/wiki/Storage-Architecture
