---
title: fact-to-subject-set
type: definition
summary: "A rewrite that reads a factset, then evaluates a computed subject set on each resource the factset names."
tags: [glossary, rewrite]
created: 2026-07-18
status: evolving
is-a: "[[subject-set-rewrite]]"
---

A rewrite that reads a factset, then evaluates a computed subject set on each resource the factset names. Its effective subject set is the union of the sets computed on those resources. This is the mechanism for inherited permissions.
