---
title: Grouping the APIs into services
type: decision
summary: "Kingo exposes the five Zanzibar APIs (Read, Write, Watch, Check, Expand) across four separate ASP.NET Core hosts, grouped by load profile rather than one host per API: Write, Read+Expand, Watch, and ACL Check as the hot path."
tags: [architecture, services]
status: evolving
---

# Grouping the APIs into services

## Observation

The Zanzibar paper (§2.4) defines five client APIs, but their load requirements differ by orders of magnitude. Checks and reads dominate Zanzibar's 10M+ QPS peak. The paper devotes its distributed-systems work to Check's latency budget: distributed caching with consistent hashing, request hedging, hot-spot mitigation, and the Leopard index. Mutations are rare. Read and Expand are query-shaped and tolerant of latency (UI serving). Watch is streaming with a different connection lifecycle entirely.

CQRS systems split the same way. Bounded contexts share domain vocabulary and rarely share projection, because a write-side order looks nothing like a read-side order. Deployment boundaries follow load profiles.

## Interpretation

Four hosts:

1. **Write** — mutations are rare; can run on a very slow system. Sole writer of the fact store; appends the changelog, which is the [[kookie]] source. Carries the drift invariants (2026-07-20, dry-run finding F8): fact writes validate against the current theory, and a theory write that would abandon live facts is refused — a reverse existence query at theory-write time, making removal a two-step migration.
2. **Read + Expand** — co-hosted serving tier; query-shaped, tolerant of latency.
3. **Watch** — changelog streaming; long-lived connections, cursors via heartbeat kookies.
4. **ACL Check** — the hot path. Multi-region, multi-node, parallel, auto-scaling; caching, hedging, and hot-spot handling land here and only here.

Supporting decisions:

- **Check and Expand share an engine, not a service.** The subjectset rewrite AST and its evaluation semantics are pure library code in Kingo core (facts + theory in, result out). One AST, two interpreters: Check's boolean short-circuiting walk, Expand's full tree materialization. Fact lookup is a port — Check implements it with a cached, hedged, snapshot-pinned lookup; Read+Expand with a plain one. (Pre-reboot main had this split wrong: the AST lived in `Kingo.Namespaces` but the evaluator in `Kingo.Acl` interleaved storage I/O with the rewrite walk.)
- **Kookie-bounded snapshot semantics are what make the hot path cacheable.** A check at a fixed snapshot is immutable, so `(resource#relation, subject, snapshot)` is a stable cache key, safe to replicate everywhere. Kookies go in every API contract from day one; retrofitting a consistency token into a shipped contract is a breaking change.
- **Hosts share only Kingo core and storage.** Core carries the vocabulary (the fact grammar as `IValueType` wrappers) and the rewrite engine; the storage layer carries facts + changelog + theories with snapshot reads. Per-service request/response projections stay private to each host — no shared canonical DTOs cross a service boundary.
- **Vocabulary is Kingo's**: Subject, SubjectSet, Resource, Relation, chosen over Zanzibar's user/userset/object/relation. A subject need not be human (client-credentials clients, agents), "user" is overloaded in systems design, and the JWT carries the principal as `sub`. The fact grammar reads `⟨resource⟩#⟨relation⟩@⟨subject⟩`, where a subject is an identity, a subjectset, or a resource bound to a subjectset.

The split is deliberately more service-oriented than Zanzibar itself, which co-hosts Read/Write/Check/Expand on one aclserver fleet with only Watch separate. Load profile justifies the split; fidelity to the paper does not.

## Next

- When scaffolding hosts, use this four-host layout (e.g. under `src/services/`), each referencing core and storage, never each other.
- First design decision the split forces: the kookie/snapshot model — timestamp source and what "snapshot no earlier than kookie" means against the storage target.
- Coordinated with [[dissolve-kingo-pdl-under-hexagonal-layout]]: the fact grammar and rewrite AST land in `Kingo` core before the hosts exist.
