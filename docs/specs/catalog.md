---
title: catalog
type: specification
summary: "The complete set of theories: the containment that holds the model and the space every name resolves in."
tags: [theory]
created: 2026-07-24
status: evolving
cites:
  - "[[glossary/catalog]]"
  - "[[theory]]"
  - "[[namespace]]"
  - "[[relation]]"
  - "[[rewrite]]"
  - "[[fact]]"
  - "[[identifiers]]"
---

# Catalog

A catalog is the complete set of theories, a containment four levels deep:

> catalog ⊃ theory ⊃ namespace ⊃ relation

A catalog holds theories, a theory holds namespaces, a namespace holds relations, and each relation is a named [[rewrite]]. Each level scopes the names beneath it: a relation name is unique within its namespace, a namespace name within its theory, a theory name across the catalog. Composing scoped segments makes every qualified name unique across the whole ([[identifiers]]).

The catalog is the space every name resolves in. Relation identifiers declared with rewrites don't require a namespace. They are late bound. A [[fact]] carries the qualification instead, each side named in full and qualified independently ([[facts]]).

The catalog is also the reach of consistency: the extent one pin covers. A [[theory]] is the unit of atomic change and carries its own version; the catalog is the whole those versioned [[theories]] compose.
