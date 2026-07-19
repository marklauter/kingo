---
title: Authorization event logging — CloudTrail-style audit
summary: "Every authorization decision and every control-plane change emits a durable audit event, CloudTrail-style: writes are management events (the changelog already is that record), Check decisions are data events shipped asynchronously as serialized Decisions."
tags: [note, audit, design]
created: 2026-07-15
status: evolving
---

# Authorization event logging — CloudTrail-style audit

Kingo needs an audit trail with CloudTrail semantics (Mark, 2026-07-15): a durable, queryable record of authorization activity, for compliance and forensics — not debug logging.

CloudTrail's event split maps cleanly onto Kingo's load profiles:

- **Management events** (control plane, low volume, always on): schema changes and fact writes — grants and revokes. The Zanzibar changelog is already this record: Watch tails it, so a durable ordered write-log falls out of the storage design nearly for free ([[storage-versioning-design]]).
- **Data events** (data plane, high volume): Check decisions. This is the class that needs new design.

## The event is the Decision

The audit event is the host's request envelope plus the `Decision` (`Kingo.Closures`) serialized. The Decision carries exactly what replay needs: the `Fact` judged (subjectset, subject), the verdict, the snapshot pin (`Kookie`), the schema version, the evaluation timestamp. Caller identity (the service asking, distinct from the subject being asked about) and correlation id live in the envelope, never in the Decision: the same question from a different caller is the same judgment ([[caller-identity]], decided 2026-07-17). A failed evaluation has no Decision; its audit event is the envelope plus the serialized error. The Kookie is what makes an entry **reproducible** — re-run the check against that snapshot and get the same answer — which is the CloudTrail property compliance actually wants. This is why the interpreters return `Decision` rather than bool from day one ([[rewrite-interpreters]]).

## Emission is asynchronous, never in the hot path

Check returns; the event goes to a bounded in-process buffer and a background shipper delivers batches (CloudTrail itself is eventually-delivered). Architecturally a port (a decision sink) implemented at the host edge — the pure interpreter never sees it ([[four-service-split-by-load-profile]]).

**Open policy question:** what happens when the buffer is full — drop events (availability wins) or backpressure Check (auditability wins)? A deployment policy knob, not a code detail; decide when the host work begins.
