---
title: schema
summary: "The complete set of domains — the global space every name resolves in, and the reach of consistency."
tags: [glossary, schema]
created: 2026-07-18
status: evolving
has-a: "[[domain]]"
---

The complete set of domains — the global space every name resolves in, and the reach of consistency.

## Examples

- `sales/documents` and `support/documents` sit in one schema. Each domain scopes its local name; the schema is where the qualified names are unique.
- A rule in one domain references a relationship in another. The reference resolves against the schema, so consistency is checked across the whole set, not within the domain that authored it.

## Contrasts

- `domain` — one scoped set of namespaces within the schema; the schema is every domain together.
