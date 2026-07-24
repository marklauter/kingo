---
title: domain
summary: "A scoped set of namespaces — the unit of ownership, authoring, and atomic change. A naming and ownership scope, never a consistency boundary."
tags: [glossary, schema]
created: 2026-07-22
status: evolving
contrast:
  - "[[namespace]]"
---

A scoped set of namespaces — the unit of ownership, authoring, and atomic change. A naming and ownership scope, never a consistency boundary.

## Examples

- Two teams each define a `documents` namespace. `sales/documents` and `support/documents` are distinct, because the domain scopes the local name.
- A domain publishes an interface other domains build on. Its rules resolve globally, so a reference may cross into another domain.

## Contrasts

- `namespace` — the stored unit within a domain; a domain is the set that owns and names them.
