---
title: Subjects exist only as identifiers inside facts
type: decision
summary: "Kingo keeps no record of the party a subject id names. The identifier arrives already resolved from the caller's principals, so nothing syncs with an identity provider."
tags: [core]
created: 2026-07-25
status: locked
---

# Subjects exist only as identifiers inside facts

Kingo keeps no record of the party a `SubjectId` names. The identifier arrives inside a [[fact]] or a membership question, and it is compared, never interpreted. There is no party record, no claim, no attribute, and no link to the directory the identifier came from.

A `SubjectId` names the unified identity that a set of principals maps to. Several trusted identity providers may each hold their own principal record for one person, and all of them resolve to that one identity. The resolving belongs to the caller and happens before a fact is written or a question is asked. Kingo receives the result and nothing else.

The identifier is the caller's to shape, under the same ownership rule as a resource id: Kingo compares it and never reads meaning into it. A `SubjectId` that looks namespaced carries no namespace.

This is about the party, not about the [[subject]] seat. That seat holds a subject id, a [[subjectset]], or a resource member, and only the subject-id shape names a party at all.

## Alternatives

**Keep a subject store.** Kingo would hold subject records, validate that a subject exists when a fact is written, and reject unknown ones. It loses on synchronization. The store duplicates a directory Kingo does not own, so every create, delete, rename, and merge upstream has to arrive. Each one that doesn't is either an authorization outage for a real person or a grant surviving a departure. Kingo would also inherit identity resolution — deciding which principals are the same person — which is the identity provider's problem and is not made easier by moving it downstream.

**Store principals directly, treating each as its own subject.** No resolution needed, and no sync, since a principal is what the token already carries. It loses because grants stop transferring. One person authenticating through a second provider arrives as a different subject and holds none of their own access, so every grant has to be written once per provider and revoked the same way.

**Validate subject identifiers against theory vocabulary.** Rejected because there is nothing to validate against. A `SubjectId` carries no namespace and no relation, so the theory has no statement about it to check. This is why a membership question is validated on the set side only.

## Why

The system never synchronizes with an identity provider. There is no party lifecycle to model, no reconciliation job, no partial-failure state between a directory and an authorization store. Facts are the only place the identifier appears, so removing a person's access is a matter of deleting facts rather than deactivating a record every read path has to honor.

It also settles how a membership question is validated. The set side is checked against the theory: the namespace exists, the relation is defined. The subject side is not checked against the theory at all. The asymmetry follows from what each side names — the set side names theory constructs, and the subject side names something the theory does not define.

The cost is that Kingo cannot answer questions about parties. It cannot say whether one exists, cannot enumerate them, and cannot distinguish a typo from a real identifier — a misspelled `SubjectId` is a legal fact that never matches anything.

The last cost is invisible and lands on the caller. If two of a person's principals resolve to two different identifiers, that person receives different answers depending on which provider they authenticated through, and nothing in Kingo detects it. Both identifiers are well-formed, both have facts, and neither is wrong from the model's point of view. Resolving principals to one identifier is therefore a correctness requirement at the edge.
