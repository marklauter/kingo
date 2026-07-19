---
title: closure
summary: "The set of facts derivable from the stored facts under the schema's rewrite rules — the set closed under derivation."
tags: [glossary, acl]
created: 2026-07-18
status: locked
---

The set of facts derivable from the stored facts under the schema's rewrite rules — the set closed under derivation.

## Examples

- `Contains(doc:x#viewer@user:anne)` asks whether the closure contains that fact — stored directly, or derived through userset expansion, computed subject sets, or tupleset traversal.
- `Kingo.Closures` is the interpreter project: both evaluators answer questions about the closure. "Authorized" is the Check host's reading of the verdict.

## Contrasts

- `reachability` — rejected as the name for this set: `!` and `&` make the derived set non-monotone, so a verdict is a function over several reachability questions, not a reachability property itself.
- `transitive closure` — graph theory's nearest term and the reason the word fits; Kingo's closure is closed under the rewrite rules' derivation, not only under edge composition.
