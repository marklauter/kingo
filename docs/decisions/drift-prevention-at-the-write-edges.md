---
title: Drift prevention at the write edges
summary: "Fact/schema drift is prevented at the Write service, not tolerated at the evaluator: fact writes validate against the current schema, spec writes that would abandon live facts are refused, and evaluation always reads a coherent snapshot pair. Removal becomes a two-step migration."
tags: [decision, write, schemas, storage]
created: 2026-07-20
status: locked
---

# Drift prevention at the write edges

Facts and specs are separately writable artifacts that reference each other. Drift — a stored fact naming a namespace or relationship the schema no longer defines — has exactly two producers, and the Write service, as sole writer of both artifacts ([[four-service-split-by-load-profile]]), closes both:

1. **Facts can't lead the schema.** Every fact write is validated against the current schema: the namespace is defined, the relationship is defined, and factset-consumed members have the exactly-specified shape.
2. **Specs can't abandon facts.** A spec write that removes a namespace or relationship is refused while live facts reference it. The check is a reverse existence query — do any live facts reference this name — at spec-write time, cold path. Removal becomes a two-step migration: migrate the facts, then land the removal. Whole-namespace and whole-spec deletion are the limiting case of the same ceremony.

A third element makes the invariants hold at read time as well as write time: evaluation and replay always read a coherent ([[kookie]], schema version) pair, both artifacts versioned on the one store timeline ([[storage-versioning-design]]). The two invariants make every moment coherent; the coupled read makes every evaluation see one moment.

Consequences: the evaluator's undefined-namespace-or-relationship error ([[rewrite-interpreters]] condition 4) is a never-in-practice backstop, reachable only through a mismatched pair or a coupling bug; and facts carry no schema version, because valid-at-write-time now implies valid-under-every-schema-since.

## Alternatives

- **Zanzibar's paper position** — validate tuple writes against the namespace config, leave deletion discipline unspecified and handle stranded configs operationally. Lost: it leaves the dangling-reference state constructible and hands the cost to the evaluator and to operations. Google absorbs that cost with people; Kingo closes it in code, which is SpiceDB's position and the one adopted.
- **Allow abandonment, keep condition 4 as an expected path** — spec agility over fact integrity: destructive spec pushes land in one step and evaluations meeting stranded facts fail as modeled errors. Lost: an authorization system values fact integrity more, and an expected-path error on the hot walk is an outage that looks like policy.
- **Schema version stored on every fact, evaluated under its own schema** — makes stranded facts self-describing. Lost: it forces multi-version evaluation semantics (two facts in one walk judged under two laws), and under the adopted invariants the stored version is dead weight.
- **Deprecate-don't-remove policy** — never delete, only mark unused. Lost: vocabulary accumulates forever and the guard it replaces is cheap.

## Why

The cost is operational and permanent: no one-shot destructive spec change, ever. Every removal is the ceremony — add the new name, migrate the facts, delete the old — and secops administrators live with it, as SpiceDB operators do. The write path also gains a reverse existence query, a new access pattern the storage design must serve ([[dynamodblite-substrate]]).

It buys referential integrity as an invariant rather than a hope: no evaluation meets a dangling reference through normal operation, the evaluator's drift error demotes to a backstop, replay needs only the recorded pair, and the fact store's rows never need to say which schema blessed them.
