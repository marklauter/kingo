---
title: closure
summary: "The set of facts derivable from the stored facts under the catalog's rewrite rules. It is closed under derivation."
tags: [glossary, evaluation]
created: 2026-07-18
status: locked
---

The set of facts derivable from the stored facts under the catalog's rewrite rules. It is closed under derivation.

## Examples

- Contains asks whether the closure holds `doc:x#viewer@user:anne`. The fact may be stored directly, or derived through subject-set expansion, a computed subject set, or a fact-to-subject-set rewrite.
- Both interpreters, Contains and Expand, answer questions about the closure. "Authorized" is the Check host's reading of the verdict.

## Contrasts

- `transitive closure` — graph theory's nearest term, and the reason the word fits. Kingo's closure is closed under the rewrite rules' derivation, not only under edge composition.
