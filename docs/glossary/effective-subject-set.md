---
title: effective-subject-set
summary: "The subjects a relationship's rewrite resolves to on a resource."
tags: [glossary, rewrite]
created: 2026-07-24
status: evolving
contrast:
  - "[[subject-set]]"
---

The subjects a relationship's rewrite resolves to on a resource.

Every rewrite operator is defined by the effective subject set it computes. Each operand contributes its own. That closes the algebra, since an operand's effective subject set is the input to the operator above it.

## Examples

- `viewer = this | editor` on `doc:readme`. Its effective subject set is the subjects written directly under `doc:readme#viewer`, together with the effective subject set of `doc:readme#editor`.
- A relationship defined with no rewrite has an effective subject set of its direct members.

## Contrasts

- `subject-set` — a subject set is the name `resource#relationship`. The effective subject set is what that name resolves to once the rewrite is evaluated.
