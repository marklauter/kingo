---
title: fact
summary: "A stored set-membership assertion — the triple resource#relationship@member, read set-first: the subject set contains the member, a subject, another subject set, or a resource."
tags: [glossary, data]
created: 2026-07-18
status: locked
contrast:
  - "[[putative-fact]]"
  - "[[theory]]"
  - "[[entitlement]]"
---

A stored set-membership assertion — the triple resource#relationship@member, read set-first: the subject set contains the member, a subject, another subject set, or a resource (the thing itself, written `folder:y#...` — `...` is the production's marker, not a relationship). The member's shape belongs to the fact. The data-side aggregate root; created and deleted, never mutated. The extensional half of the model: the recorded memberships a [[theory]]'s definitions are read against.

## Contrasts

- `putative-fact` — the same shape held as a hypothesis rather than stored; the question Contains answers.
- `theory` — a theory defines how memberships derive from other memberships; a fact is a membership recorded outright. A theory ranges over facts: delete the theory and derivation stops, delete a fact and one membership is gone.
- `entitlement` — a fact is a premise, stored and deletable; an entitlement is a conclusion that holds in the closure only as long as the facts entailing it do.
