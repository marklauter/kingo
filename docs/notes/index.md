---
type: index
title: Kingo notes
summary: "Index of docs/notes/ — current-state notes, decisions, and todos for the Kingo reboot."
tags: [note, index]
created: 2026-07-14
---

# Kingo notes

Repo memory that outlives the context window. A **note** is a mutable snapshot of present belief — rewrite it freely as understanding moves. A **decision** is an ADR-equivalent: hard to reverse, surprising without context, a real trade-off. A **todo** is a note whose lifecycle lives in frontmatter properties (`status`, `priority`, `effort`, `blocked_by`), never in tags.

Format: Hoplite frontmatter standard — flat Obsidian Properties, wikilinks as edges (`hoplite` repo: `docs/hoplite/frontmatter.md`, `docs/hoplite/expressing-edges.md`). No `updated` key; git history is the modification record.

## The system

- [[architecture]] — hexagonal with a DDD core: `Kingo` holds domain types, `Kingo.Serialization` defines ports, `Kingo.Serialization.{Json,Yaml,Pdl}` are adapters.
- [[domain-language]] — the ubiquitous language: the relation-tuple grammar in Kingo vocabulary and the mapping from each production to its C# type. The Parse boundary rule lives here.
- [[pdl-yaml]] — the Policy Definition Language: YAML outer structure, embedded rewrite-expression language. Deliberately quarry-era pending the adapter work.

## Decisions

- [[four-service-split-by-load-profile]] — five Zanzibar APIs across four hosts grouped by load profile; ACL Check is the hot path.
- [[immutablearray-for-domain-collections]] — domain values carry `ImmutableArray<T>`; custom structural equality is mandatory.
- [[dynamodblite-substrate]] — code against `AWSSDK.DynamoDBv2`, run DynamoDbLite locally; no storage port for the local/prod switch. Settled: production-ready, key/value store style (no ORM).

## Todos

Queue order: PDL adapter → converters → (then, any order) rewrite interpreters, DynamoDbLite spike, zookie/snapshot design — the Write host waits on all three. The core test pass closed 2026-07-14 (ten test files; gate green, Kingo at 98% line / 100% branch; note deleted per its disposition).

- [[dissolve-kingo-pdl-under-hexagonal-layout]] — **open**; the next work item. Rewrite parser/serializer as `Kingo.Serialization.Pdl`, delete the quarry project.
- [[move-jsonconverter-off-identifier-types-into-the-json-adapter]] — **open**, blocked by the PDL adapter; `IParse`-keyed converters registered in the adapters.
- [[storage-versioning-design]] — **open**; design the versioning system (zookies, write CAS, Watch cursors — one scheme, three consumers) when storage work begins.
- [[ivalue-tself-tvalue-absorbs-all-value-type-wrappers]] — **closed** 2026-07-14 by fresh construction of the core.

## Reference

- [[sources]] — the quarry branches (`main-archive`, `dictionary-encoding`): what they hold and how to lift files from them.
