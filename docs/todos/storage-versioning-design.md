---
title: Storage versioning design
summary: "Before storage work lands, design the versioning system: snapshot semantics for zookies, optimistic concurrency for writes, the changelog, and the schema-version changelog. The shapes are settled (2026-07-20: interval-stamped fact rows, supersession-closed schema changelog, one timeline); the encodings and conditional-write patterns are this design's to fill."
tags: [note, todo, storage, versioning, zookies]
created: 2026-07-14
status: open
priority: high
effort: medium
---

# Storage versioning design

## Observation

Versioning shows up three ways in the Zanzibar design, and Kingo has settled decisions that touch each without designing the mechanism itself:

- **Snapshot reads / zookies** — [[four-service-split-by-load-profile]] locks zookies into every API contract from day one and names the zookie/snapshot model as the first design decision the hosts force: timestamp source, and what "snapshot no earlier than zookie" means against the storage target. A check at a fixed snapshot is the cache key that makes the hot path work.
- **Optimistic concurrency on writes** — the Write host is the sole writer and appends the changelog (the zookie source). The [[dynamodblite-substrate]] mapping table leaned on `[DynamoDBVersion]` for CAS, but the key/value store style settled 2026-07-14 drops the ORM — version-checked writes are now hand-rolled `ConditionExpression`s in the storage adapter.
- **MVCC / changelog shape** — the mapping table sketches "MVCC header + journal split → items + separate journal table" and "Watch API → DynamoDB Streams." The item-level scheme is settled in shape (2026-07-20, dry-run finding F8): fact rows are interval-stamped — created at one Kookie, tombstoned at another, never mutated between — a snapshot read filters at the pin, and GC advances a store-wide retention horizon. The attribute encoding of the stamps is this design's to fill; the earlier three-way choice (per-fact version attributes / per-partition sequence / commit-timestamp items) is resolved to interval stamps.

## Interpretation

**The snapshot token has two roles, and resolving between them is a service** (2026-07-16, [[rewrite-interpreters]]): request-side the token is a *floor* ("evaluate no earlier than this"); Decision-side it is the *pin* (the exact snapshot evaluated at — the reproducibility ingredient). Floor → pin resolution is performed by a zookie-aware service in the storage layer that applies the induced latency (waits out clock uncertainty, since DynamoDB has no TrueTime) so global order is trustworthy. The host edge resolves the floor, pins the fact port, and the interpreter never sees the mechanism — it copies the pin into the `Decision`. Kingo's name for the token is **`Kookie`**; "zookie" is the paper-facing alias. Working assumption (Mark, 2026-07-16): commit-wait over NTP in place of TrueTime — the same protocol with a wider uncertainty window, paid as added commit latency on the Write path, not on Check.

**Retention is the audit guarantee, not a GC knob** (2026-07-16, [[authz-event-logging]]): a `Decision` + zookie is reproducible only while the store retains the fact versions at that snapshot. The version-retention window must be at least the audit-replay window — beyond it, old Decisions degrade from reproducible claims to assertions (or periodic snapshot archives extend the horizon, as Zanzibar's offline pipeline does). The mechanism (2026-07-20): GC advances a store-wide watermark, and a pin behind it is refused outright as the snapshot-unavailable error ([[rewrite-interpreters]] condition 8) rather than served a partial world. The schema changelog gets the same discipline — trimmed only from the tail, never past a version some in-horizon Decision records.

**Both artifacts version on one timeline, and evaluation reads the coherent pair** (2026-07-20, dry-run finding F8). The schema store is an append-only changelog of whole `Schema` values: version N's interval closes by supersession (N+1's start Kookie), and whole-schema deletion is a terminal marker entry, since an empty `Schema` is unrepresentable (`schema.empty`). `SchemaVersion` identifies a changelog entry, and the covering version is derivable from a Kookie ("the schema at K"), so a host cannot pick a mismatched pair by accident; `Decision`/`Expansion` still record both, because replay must not depend on re-derivation. Facts carry no schema version — the Write-service drift invariants ([[drift-prevention-at-the-write-edges]]) make valid-at-write-time imply valid-under-every-schema-since. Two access patterns join the design from the same ruling: the schema-at-K lookup, and the schema-write guard's reverse existence query (do any live facts reference this namespace or relationship — cold path, schema-write time only).

One design, three consumers: the zookie a client holds, the condition a write asserts, and the cursor a Watch stream resumes from should all derive from the same versioning scheme. Designing them separately risks three incompatible clocks. The hard part is the commit-timestamp protocol — DynamoDB gives conditional writes, not Spanner's TrueTime, so "snapshot no earlier than zookie" needs an app-layer answer (the paper's §2.2 zookie protocol, reworked for a store without global commit timestamps).

## Next

- Design session when storage work begins (queued behind the SDL adapter and converters per [[index]]): encode the settled shapes — the interval-stamp attributes on fact items, the schema changelog's item form — define `Kookie` and `SchemaVersion` encodings as functions of them, and specify the conditional-write patterns the Write adapter uses, including the schema-write reverse existence query and the schema-at-K lookup.
- Validate the design against all three consumers before code: Check's snapshot-pinned reads, Write's CAS, Watch's cursors.

## Related

- [[four-service-split-by-load-profile]] — zookie-bounded snapshot semantics as the performance enabler
- [[dynamodblite-substrate]] — the substrate the scheme must be expressed in
