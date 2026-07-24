---
title: Rewrite equality recurses unbounded
summary: "Equality and hashing over the rewrite algebra recurse per nesting level, so a tree tens of thousands of levels deep would overflow the stack on Equals or GetHashCode. Closed: depth is tracked at construction and the operator factories refuse trees past SubjectSetRewrite.MaxDepth, so a tree deep enough to matter is unrepresentable."
tags: [note, todo, schemas]
created: 2026-07-21
status: closed
priority: low
effort: medium
---

# Rewrite equality recurses unbounded

Raised by code review, 2026-07-21, after `Namespace.Create`'s traversals went iterative ([[namespace-create-validation]]). The validation gate no longer recurses, but structural equality still does: `SubjectSetRewrite.Union`/`SubjectSetRewrite.Intersection` `SequenceEqual` into children, `SubjectSetRewrite.Exclusion`'s synthesized record `Equals` into `Include`/`Exclude`, and `SequenceHash.Of` into each child — all in `src/Kingo.Schemas/SubjectSetRewrite.cs`, and reachable through `Namespace`/`Spec` equality.

The exposure was the trusted path: the SDL parser's guard bounds every tree untrusted text can produce, but a trusted caller could assemble an arbitrarily deep tree through the public factories.

## Resolution

Closed 2026-07-21, branch `namespace-parse-invariants`, by the fix shape this note named: depth tracked at construction and refused past a bound. `SubjectSetRewrite` carries `Depth` (structural height, 1 at a leaf) and `MaxDepth` (100); the operator factories compute each node's depth from its operands in O(1) and refuse past the bound with `rewrite.depth` — `SubjectSetRewrite.Exclusion.Create` became `Result`-returning to carry the refusal. No constructible tree can drive equality, hashing, or any other recursion over the algebra deep enough to exhaust a stack. The SDL parse edge carries two guards against the same constant: a parenthesis-nesting counter bounds the grammar's own recursion before it runs (parens are its only recursion — operator chains fold iteratively), and an iterative height gate on the parsed tree refuses too-deep shapes with the same `rewrite.depth` error before the transform recurses; breadth (wide flat operator chains) passes freely. Pinned by `SubjectSetRewriteTests` (depth-bound section) and `RewriteExpressionParserTests` (paren-scan and tree-height boundary tests).
