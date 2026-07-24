---
title: theory
summary: "A scoped set of namespaces whose rewrites define how memberships derive."
tags: [glossary, theory]
created: 2026-07-22
status: locked
has-a: "[[namespace]]"
contrast:
  - "[[fact]]"
---

A scoped set of namespaces whose rewrites define how memberships derive.

A theory is intensional: it defines subject sets from other subject sets, deriving memberships rather than recording them. Recorded memberships are [[fact]]s, the extensional half. A membership question is settled by reading the facts through the theory, and neither half answers alone.

It is also the unit of ownership, authoring, and atomic change.

## Examples

- Two teams each define a `documents` namespace. `sales/documents` and `support/documents` are distinct, because the theory scopes the local name.
- A theory publishes an interface other theories build on. Its rules resolve globally, so a reference may cross into another theory.

## Contrasts

- `fact` — a fact records a membership that holds; a theory defines how memberships are derived. Facts are what a theory is read against; the theory is the shape they are read through.
- `namespace` — the stored unit within a theory; a theory is the set that owns and names them.
