---
title: Storage versioning design
summary: "Before storage work lands, design the versioning system: snapshot semantics for zookies, optimistic concurrency for writes, and the changelog — all hand-rolled as conditional writes now that the key/value store style is settled."
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
- **MVCC / changelog shape** — the mapping table sketches "MVCC header + journal split → items + separate journal table" and "Watch API → DynamoDB Streams," but the item-level versioning scheme (per-tuple version attributes? per-partition sequence? commit-timestamp items?) is undesigned.

## Interpretation

**The snapshot token has two roles, and resolving between them is a service** (2026-07-16, [[rewrite-interpreters]]): request-side the token is a *floor* ("evaluate no earlier than this"); Decision-side it is the *pin* (the exact snapshot evaluated at — the reproducibility ingredient). Floor → pin resolution is performed by a zookie-aware service in the storage layer that applies the induced latency (waits out clock uncertainty, since DynamoDB has no TrueTime) so global order is trustworthy. The host edge resolves the floor, pins the fact port, and the interpreter never sees the mechanism — it copies the pin into the `Decision`. Kingo's name for the token is **`Kookie`**; "zookie" is the paper-facing alias. Working assumption (Mark, 2026-07-16): commit-wait over NTP in place of TrueTime — the same protocol with a wider uncertainty window, paid as added commit latency on the Write path, not on Check.

**Retention is the audit guarantee, not a GC knob** (2026-07-16, [[authz-event-logging]]): a `Decision` + zookie is reproducible only while the store retains the tuple versions at that snapshot. The version-retention window must be at least the audit-replay window — beyond it, old Decisions degrade from reproducible claims to assertions (or periodic snapshot archives extend the horizon, as Zanzibar's offline pipeline does).

One design, three consumers: the zookie a client holds, the condition a write asserts, and the cursor a Watch stream resumes from should all derive from the same versioning scheme. Designing them separately risks three incompatible clocks. The hard part is the commit-timestamp protocol — DynamoDB gives conditional writes, not Spanner's TrueTime, so "snapshot no earlier than zookie" needs an app-layer answer (the paper's §2.2 zookie protocol, reworked for a store without global commit timestamps).

## Next

- Design session when storage work begins (queued behind the SDL adapter and converters per [[index]]): pick the version representation (per-item version, partition sequence, commit-timestamp table), define zookie encoding as a function of it, and specify the conditional-write patterns the Write adapter uses.
- Validate the design against all three consumers before code: Check's snapshot-pinned reads, Write's CAS, Watch's cursors.

## Related

- [[four-service-split-by-load-profile]] — zookie-bounded snapshot semantics as the performance enabler
- [[dynamodblite-substrate]] — the substrate the scheme must be expressed in
