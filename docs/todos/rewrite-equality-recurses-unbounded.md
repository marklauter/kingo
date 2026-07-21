---
title: Rewrite equality recurses unbounded
summary: "Equality and hashing over the rewrite algebra recurse per nesting level (operator SequenceEqual into children, synthesized record Equals, RewriteHash.OfSequence), so a tree tens of thousands of levels deep overflows the stack on Equals or GetHashCode. Deferred: the SDL token budget keeps untrusted text from minting such a tree, so the exposure is a trusted caller building one deliberately."
tags: [note, todo, schemas]
created: 2026-07-21
status: open
priority: low
effort: medium
---

# Rewrite equality recurses unbounded

Raised by code review, 2026-07-21, after `Namespace.Create`'s traversals went iterative ([[namespace-create-validation]]). The validation gate no longer recurses, but structural equality still does: `UnionRewrite`/`IntersectionRewrite` `SequenceEqual` into children, `ExclusionRewrite`'s synthesized record `Equals` into `Include`/`Exclude`, and `RewriteHash.OfSequence` into each child — all in `src/Kingo.Schemas/SubjectSetRewrites.cs`, and reachable through `Namespace`/`Schema` equality.

Deferred rather than fixed because the untrusted path is closed: `RewriteExpressionParser`'s token budget (200 tokens per expression) bounds every tree SDL can produce, so an overflow needs a trusted caller iteratively assembling a tree tens of thousands of levels deep through the public factories — its own defect today.

Becomes real work if a service edge ever compares or hashes rewrite trees it did not construct (cache keys, config diffing on the Write path). The fix shapes: iterative structural equality with an explicit stack, or a depth tracked at construction and refused past a bound.
