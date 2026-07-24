---
title: theory
summary: "A scoped set of namespaces — the unit of ownership, authoring, and atomic change."
tags: [glossary, schema]
created: 2026-07-22
status: evolving
contrast:
  - "[[namespace]]"
---

A scoped set of namespaces — the unit of ownership, authoring, and atomic change.

## Examples

- Two teams each define a `documents` namespace. `sales/documents` and `support/documents` are distinct, because the theory scopes the local name.
- A theory publishes an interface other theories build on. Its rules resolve globally, so a reference may cross into another theory.

## Contrasts

- `namespace` — the stored unit within a theory; a theory is the set that owns and names them.
