---
title: catalog
summary: "The complete set of theories: the containment that holds the model and the space every name resolves in."
tags: [spec, theory]
created: 2026-07-24
status: evolving
cites:
  - "[[catalog]]"
  - "[[theory]]"
  - "[[namespace]]"
  - "[[relationship]]"
  - "[[subject-set-rewrite]]"
  - "[[fact]]"
  - "[[identifiers]]"
---

# Catalog

A catalog is the complete set of theories, a containment four levels deep:

> catalog ⊃ theory ⊃ namespace ⊃ relationship

A catalog holds theories, a theory holds namespaces, a namespace holds relationships, and each relationship carries a [[subject-set-rewrite]]. Each level scopes the names beneath it: a relationship name is unique within its namespace, a namespace name within its theory, a theory name across the catalog. Composing scoped segments makes every qualified name unique across the whole ([[identifiers]]).

The catalog is the space every name resolves in. A rewrite in one theory may reference a relationship in another, and that reference resolves against the catalog, not the theory that wrote it. A [[fact]] may span theories the same way, each side qualified independently ([[facts]]).

The catalog is also the reach of consistency: the boundary within which references resolve and consistency holds. A [[theory]] is the unit of atomic change and carries its own version; the catalog is the whole those versioned [[theories]] compose.
