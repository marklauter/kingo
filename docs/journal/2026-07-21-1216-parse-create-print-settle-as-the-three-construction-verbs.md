---
title: Parse, create, print settle as the three construction verbs
summary: "While reviewing the namespace-create-validation implementation, the test helpers' terse names (Ns, Rel) raised the question of hoisting them into production — and the answer settled the construction vocabulary instead: parse is the fallible lift from text, create the trusted construction, print the retraction, one verb per arrow."
tags: [journal, schemas, vocabulary]
created: 2026-07-21
---

Context: [[namespace-create-validation]] landed today — staged duplicate/dangling/cycle checks in the namespace factory, private-constructor-plus-static-`Create` across the rewrite algebra. Reviewing the diff, Mark liked the SDL test helpers' terse names (`Ns`, `Rel`, `Bare`, `FactTo`) against the deliberately descriptive domain names, and asked whether hoisting them into the production SDL project would buy readability.

Expected: a style trade-off — concision versus the descriptive-domain-names criterion.

Learned: it was a channel question, not a style one. The test helpers and the production factories are different arrows over the same embedding: `string → T`, total by test fiat, versus `string → Result<T>`, total by modeled failure. One name meaning both would hide which channel a caller is on. Working that boundary settled the vocabulary: **parse** is the fallible lift from text into the domain, **create** the trusted construction (fallible only where an invariant relates the parts), **print** the retraction, with the round-trip law binding the pair. One verb per arrow; every producer path is one of the three. The register already in the code confirmed it: arrows get one strong-prior word (`Parse`, `Create`, `Print`, `Contains`, `Expand`), nouns stay descriptive. The terse helpers stay test-side, where their unwrap semantics are honest. Locked as parse, create, print.
