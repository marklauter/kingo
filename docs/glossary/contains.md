---
title: contains
summary: "The membership predicate. It applies a relationship's rewrite as set algebra over the facts to decide whether the closure holds a putative fact."
tags: [glossary, evaluation]
created: 2026-07-18
status: locked
contrast:
  - "[[check]]"
applies: "[[subject-set-rewrite]]"
decides: "[[putative-fact]]"
---

The membership predicate. It applies a relationship's rewrite as set algebra over the facts to decide whether the closure holds a putative fact. The result is a decision. The hot path.

## Contrasts

- `check` — contains settles the question, and knows nothing beyond the facts and the theory. A check is the host-edge request that asks for it and carries the caller's context.
