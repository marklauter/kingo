---
type: todo
title: Rewrite interpreters — Contains and Expand
summary: "One algebra, two interpreters: Contains (Check's short-circuiting membership predicate) and Expand (full tree materialization) over SubjectSetRewrite — pure core, fact lookup as the first genuine port, DecisionRecord as the result. Requirements only; design clean-room."
tags: [note, todo, acl, interpreters]
created: 2026-07-15
status: open
priority: high
effort: high
---

# Rewrite interpreters — Contains and Expand

## Clean-room constraint

Design this in a fresh context from this note, the Zanzibar paper (`5068.pdf` on the archive branches), and the current domain code. **Do not read the archive branches' ACL implementation** (`src/Kingo.Acl/` on `main-archive` / `dictionary-encoding`) before or during design — it is incomplete, and its lessons are already encoded below as requirements (Mark, 2026-07-15).

## The work item

Two evaluators over the `SubjectSetRewrite` algebra, pure core code in `Kingo`:

- **Contains** — the membership predicate behind the Check API: subject ∈ subjectset? A short-circuiting boolean walk; the hot path.
- **Expand** — full materialization of a subjectset's membership tree, nothing skipped; the audit/debug/UI view.

One algebra, two interpreters. This is the first joint consumer of both aggregates — rules from `Schema`, facts from the graph — so expect it to pressure-test the algebra's semantics the way SDL pressure-tested the names ([[the-first-consumer-forges-the-domain]]).

## Naming

- The domain predicate is **`Contains`** — set-first: `subjectSet.Contains(subject)` matches the ∋ reading ([[domain-language]]) and carries .NET's strongest prior (`set.Contains(member)`). Not `IsMember`/`IsAMemberOf`, which read member-first and re-invert that reading.
- **`Check`** stays the service/API-level name — Zanzibar's reach word ([[four-service-split-by-load-profile]]).

## Requirements

1. The result is a **`DecisionRecord`** (`Kingo.Decisions`, stub exists), not a bare bool: verdict, subject, subjectset, the snapshot evaluated at, schema version, timestamp — the audit event is this value serialized ([[authz-event-logging]]). Expand needs a sibling result type carrying the tree.
2. Direct membership (`this`) includes **both** direct subject facts **and** the members of subjectset-valued facts stored under the same (resource, relationship) — Zanzibar's userset-expansion clause. A grant to `group:eng#member` must make every member of that set a member here.
3. The walk is **cycle-safe and depth-bounded with a modeled outcome** (a `Result` error, never a crash). Cycles can exist in the schema (mutual computed subjectsets) and in the graph data (parent loops) — and graph data is user-writable, so an uncrashable hot path is a security property, not a nicety.
4. Evaluating against an **undefined namespace or relationship is a modeled error**, distinct from a denial. Fail-closed applies to facts, not to schema drift or caller defects.
5. The only I/O is **fact lookup, injected as a port** — the first genuine port family. The port is handed to the interpreter already **snapshot-pinned**: one Check evaluates against one consistent snapshot end to end (the zookie enters the signature as an opaque token; [[storage-versioning-design]]). Two implementations by design: Check's cached/hedged one, Read+Expand's plain one.
6. The access patterns this work discovers are the **input to the DynamoDB key design** ([[dynamodblite-substrate]]). Expected at minimum: point lookup (does this fact exist?) and range read (all facts under resource#relationship); record what Expand adds.
7. **Exclusion and intersection get explicit semantics decisions and tests.** Evaluation order within them is a cost choice; the semantics are fixed and must be pinned by tests (the SDL example's `banned` exclusion is the obvious fixture).
8. **Tupleset traversal** (`TupleToSubjectSet`): make an explicit, tested decision on how a parent fact whose subject carries a real relationship (rather than `...`) is treated.
9. **Audit emission is an effect at the host edge.** The interpreter returns the `DecisionRecord` and never sees a sink, a logger, or a clock it didn't receive.
