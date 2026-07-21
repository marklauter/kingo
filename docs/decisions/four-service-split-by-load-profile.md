---
title: Four-service split by load profile
summary: "Kingo exposes the five Zanzibar APIs (Read, Write, Watch, Check, Expand) across four separate ASP.NET Core hosts, grouped by load profile rather than one host per API: Write, Read+Expand, Watch, and ACL Check as the hot path."
tags: [decision, architecture, services]
created: 2026-07-14
status: locked
---

# Four-service split by load profile

## Observation

The Zanzibar paper (§2.4) defines five client APIs, but their load requirements differ by orders of magnitude. Checks and reads dominate Zanzibar's 10M+ QPS peak, and the paper's entire distributed-systems arsenal — distributed caching with consistent hashing, request hedging, hot-spot mitigation, the Leopard index — exists to serve Check's latency budget. Mutations are rare. Read and Expand are query-shaped and latency-tolerant (UI serving). Watch is streaming with a different connection lifecycle entirely.

In our day-job CQRS systems the same lesson holds: bounded contexts share domain vocabulary but almost never share projection — a write-side order looks nothing like a read-side order. Deployment boundaries should follow load profiles, not domain seams.

## Interpretation

Four hosts:

1. **Write** — mutations are rare; can run on a very slow system. Sole writer of the fact store; appends the changelog, which is the zookie source. Carries the drift invariants (2026-07-20, dry-run finding F8): fact writes validate against the current schema, and a schema write that would abandon live facts is refused — a reverse existence query at schema-write time, making removal a two-step migration.
2. **Read + Expand** — co-hosted serving tier; query-shaped, latency-tolerant.
3. **Watch** — changelog streaming; long-lived connections, cursors via heartbeat zookies.
4. **ACL Check** — the hot path. Multi-region, multi-node, parallel, auto-scaling; all the high-performance distributed-systems work (caching, hedging, hot-spot handling) lands here and only here.

Supporting decisions:

- **Check and Expand share an engine, not a service.** The subject-set rewrite AST and its evaluation semantics are pure library code in Kingo core (facts + namespace config in, result out). One AST, two interpreters: Check's boolean short-circuiting walk, Expand's full tree materialization. Fact lookup is a port — Check implements it with a cached, hedged, snapshot-pinned lookup; Read+Expand with a plain one. (Pre-reboot main had this split wrong: the AST lived in `Kingo.Namespaces` but the evaluator in `Kingo.Acl` interleaved storage I/O with the rewrite walk.)
- **Zookie-bounded snapshot semantics are the performance enabler**, not consistency pedantry. A check at a fixed snapshot is immutable, so `(resource#relationship, subject, snapshot)` is a perfect cache key, safe to replicate everywhere. Zookies go in every API contract from day one; retrofitting consistency tokens into a shipped contract is miserable.
- **Hosts share only Kingo core and storage.** Core carries the vocabulary (the fact grammar as `IValue` types) and the rewrite engine; the storage layer carries facts + changelog + namespace configs with snapshot reads. Per-service request/response projections stay private to each host — no shared canonical DTOs cross a service boundary.
- **Vocabulary is Kingo's, not the paper's**: Subject, SubjectSet, Resource, Relationship — deliberate improvements over Zanzibar's user/userset/object/relation. A subject need not be human (client-credentials clients, agents), "user" is overloaded in systems design, and the JWT carries the principal as `sub`. The paper's tuple grammar reads `⟨resource⟩#⟨relationship⟩@⟨subject⟩` where a subject is `SubjectId | SubjectSet`.

Note this is deliberately more service-oriented than Zanzibar itself, which co-hosts Read/Write/Check/Expand on one aclserver fleet with only Watch separate. The split is justified by the load-profile argument, not by fidelity to the paper.

## Next

- When scaffolding hosts, use this four-host layout (e.g. under `src/services/`), each referencing core and storage, never each other.
- First design decision the split forces: the zookie/snapshot model — timestamp source and what "snapshot no earlier than zookie" means against the storage target.
- Coordinated with [[dissolve-kingo-pdl-under-hexagonal-layout]]: the fact grammar and rewrite AST land in `Kingo` core before the hosts exist.
