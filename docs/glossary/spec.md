---
title: spec
summary: "An owned set of namespaces, grouped by domain and scoping their local names — the unit of ownership, authoring, and atomic change. A naming and ownership scope, never a consistency boundary."
tags: [glossary, schema]
created: 2026-07-22
status: evolving
contrast:
  - "[[namespace]]"
---

An owned set of namespaces, grouped by domain and scoping their local names — the unit of ownership, authoring, and atomic change. A naming and ownership scope, never a consistency boundary.

## Examples

- Two teams each define a `documents` namespace. `sales/documents` and `support/documents` are distinct, because the spec scopes the local name.
- A spec publishes an interface other specs build on. Its rules resolve globally, so a reference may cross into another spec.

## Contrasts

- `namespace` — the stored unit within a spec; a spec is the set that owns and names them.
