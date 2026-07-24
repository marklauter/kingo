---
title: rename domain to theory across corpus and code
summary: "Migrate the collection-of-namespaces sense of domain to theory everywhere but the two locked files, leaving the DDD/domain-model sense alone."
tags: [todo, schema]
created: 2026-07-24
priority: medium
effort: high
status: open
---

The [[theory]] glossary term and the [[theories]] spec are renamed from `domain`/`domains`. The rename ripples, and the ripple needs judgment: only the **collection-of-namespaces** sense becomes `theory`. The **DDD/domain-model** sense (`domain code`, `domain collections`, `domain library`) stays `domain` — that overload is the reason for the rename, so collapsing both senses would defeat it.

## Where it ripples

Docs still naming the renamed sense:

- `docs/specs/identifiers.md` — the `⟨domain name⟩` EBNF production and its prose; becomes `⟨theory name⟩`.
- `docs/specs/facts.md` — references to the domain a fact belongs to.
- `docs/glossary/namespace.md`, `docs/glossary/schema.md`, `docs/glossary/this.md` — `[[domain]]` links.
- `docs/notes/ubiquitous-language.md`, `docs/notes/quaries.md`, `docs/notes/architecture.md`, `docs/notes/index.md` — collection sense mixed with DDD sense; triage each hit.
- `docs/todos/*` — several open todos name the collection sense.

Grep seeds (each hit needs the sense judged, not blind-replaced):

- `\[\[domains?\]\]` — every wikilink to the renamed pages.
- `⟨domain name⟩`, `domain:` — grammar production and YAML key.
- `domain\.rewrite` — the parenthesis-depth refusal code.

## Code

The domain type, its projection, the `domain:` YAML key on the wire, and the `domain.rewrite` refusal code all carry the renamed sense. Clean rename per the no-aliases rule: no back-compat for the old key or symbol name. Sweep after the doc pass so the two stay consistent.

## Done when

Every collection-of-namespaces reference reads `theory`; every DDD reference still reads `domain`; the two are no longer ambiguous in any file.
