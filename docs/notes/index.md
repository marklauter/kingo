---
title: Kingo notes
summary: "Index of the docs corpus — current-state notes (docs/notes/), decisions (docs/decisions/), and todos (docs/todos/) for the Kingo reboot."
tags: [note, index]
created: 2026-07-14
---

# Kingo notes

Repo memory that outlives the context window. A **note** (`docs/notes/`) is a mutable snapshot of present belief; rewrite it freely as understanding moves. A **decision** (`docs/decisions/`) is an ADR-equivalent: hard to reverse, surprising without context, a real trade-off. A **todo** (`docs/todos/`) is a note tagged `todo` whose lifecycle lives in the frontmatter properties `status`, `priority`, `effort`, and `blocked-by`, never in tags.

Format and authoring rules live in the hoplite skills (see CLAUDE.md, "Docs and notes"): flat Obsidian Properties, wikilinks as edges. No `updated` key; git history is the modification record.

## The system

- [[architecture]] — hexagonal with a DDD core: `Kingo` holds domain types; `Kingo.Sdl` is the SDL document codec; `.Json`/`.Yaml` are value-type converter packs for future REST hosts. No ports project — the first genuine port family (storage, transport) gets its own.
- [[domain-language]] — the ubiquitous language: the fact grammar in Kingo vocabulary and the mapping from each production to its C# type. The Parse boundary rule lives here.
- [[schema-definition-language]] — the Schema Definition Language: the `spec:` name plus `namespaces:` map envelope, embedded rewrite-expression language. Implemented by the `Kingo.Sdl` adapter.
- [[the-first-consumer-forges-the-domain]] — building the SDL codec pressure-tested the core: the renames, the aggregate collapse, and the port dissolution all fell out of one real consumer; put one on a young domain early.
- [[authz-event-logging]] — CloudTrail-style audit: writes are management events (the changelog already is that record), Check decisions are data events shipped asynchronously as serialized `Decision`s; buffer-full policy open.
- [[clean-room-procedure]] — how a work item reaches a clean-room design session: self-sufficient handoff note, input closure by settled kind, dry-run critique, findings ledger driven empty before the session runs. Distilled from the rewrite-interpreters run.
- [[the-walk-gathers-constraints-set-theory-decides]] — Contains intermixes graph theory and set theory with separate guards: traversal over facts discovers which set expressions apply, Kleene evaluation of the assembled expression produces the verdict. The walk assembles the question; it never answers it.

## Decisions

- [[four-service-split-by-load-profile]] — five Zanzibar APIs across four hosts grouped by load profile; ACL Check is the hot path.
- [[immutablearray-for-domain-collections]] — domain values carry `ImmutableArray<T>`; custom structural equality is mandatory.
- [[dynamodblite-substrate]] — code against `AWSSDK.DynamoDBv2`, run DynamoDbLite locally; no storage port for the local/prod switch. Settled: production-ready, key/value store style (no ORM).

## Todos

Queue order: rewrite interpreters first (selected 2026-07-15 — the consumer that specifies the storage work), then converters, DynamoDbLite spike, zookie/snapshot design in any order — the Write host waits on all of them. The core test pass closed 2026-07-14 (ten test files; gate green, Kingo at 98% line / 100% branch; note deleted per its disposition).

- [[rewrite-interpreters]] — **open**, the selected next work item: `Contains` (Check's predicate) + `Expand` over `SubjectSetRewrite` in `Kingo.Closures`, fact lookup as the first genuine port, `Decision` result. Requirements-only note — **design clean-room in a fresh context; do not read the archive ACL code.**

- [[realign-serialization-projects-around-their-real-consumers]] — **closed** 2026-07-15: ports project dissolved, `Spec` landed, SDL surface reworked to `SpecParser.Parse(text) → Result<Spec>` + `spec.Print()`. The lost "public types implement a port" arch rule is replaced with nothing — no port exists to anchor it; it returns with the first genuine port family (storage).
- [[graph-document-is-bulk-dml]] — **open**, blocked on the first ports project; proposal 2026-07-15: the fact-side document is a list of `create`/`touch`/`delete` operations in YAML section blocks, parsing to a `GraphOperation` DU that lives *between the edges*, not in the domain — every rule it carries is storage semantics. The `Graph`/`GraphParser`/`GraphPrinter` stubs were deleted; the `Graph`-is-not-a-type guardrail held.
- [[move-jsonconverter-off-identifier-types-into-the-json-adapter]] — **deferred** until the first wire consumer (the REST hosts); retitled "IParse-keyed converters for the JSON and YAML adapters" — the leak half is dead, the constructive half waits.
- [[storage-versioning-design]] — **open**; design the versioning system (zookies, write CAS, Watch cursors — one scheme, three consumers) when storage work begins.
- [[caller-identity]] — **open**; what "caller identity" means at the Check host edge (network context, principal, OBO chain) and the three authorization decisions an OBO call implies, including Kingo authorizing calls to itself.
- [[pin-sdk-10.0.204]] — **closed** 2026-07-21; `global.json` pins 10.0.204 with `rollForward: disable` — CI hermetic and fail-loud.
- [[ivalue-tself-tvalue-absorbs-all-value-type-wrappers]] — **closed** 2026-07-14 by fresh construction of the core.
- [[dissolve-kingo-pdl-under-hexagonal-layout]] — **closed** 2026-07-14: `Kingo.Serialization.Pdl` adapter landed (first port, adapter-layer ArchUnit rules), quarry deleted; the adapter is since renamed `Kingo.Sdl` and the port dissolved.

## Reference

- [[sources]] — the quarry branches (`main-archive`, `dictionary-encoding`): what they hold and how to lift files from them.
