---
title: Spec as owned qualifier, not a wall
summary: "Reframing from the rewrite-interpreters clean room: a spec (an owned set of namespaces) is the qualifier segment of a namespace identity (spec/namespace), enforced at the write edge, never a consistency wall. Rules stay globally consistent (strict, a published contract); facts roam across specs and may dangle. Triggered by finding facts were schema-agnostic — the keystone the schema dissolution rested on."
tags: [note, schemas, evaluation, write]
created: 2026-07-22
status: evolving
revisits: "[[schema-dissolves-into-administration]]"
---

# Spec as owned qualifier, not a wall

State of an in-flight redesign, opened while designing the rewrite interpreter ([[rewrite-interpreters]]) and finding that facts were schema-agnostic. The owned grouping of namespaces is now the [[spec]]; this note uses it throughout, and keeps "schema" only for the dissolved domain type and the [[schema-dissolves-into-administration]] decision that retired it.

## The keystone

[[schema-dissolves-into-administration]] retired `Schema` as a domain type *because* facts were schema-agnostic — that premise forces globally-unique bare namespace names, which leaves the grouping owning nothing structural, which is why it "earns no aggregate status." Every link in that chain hangs off the one premise.

Making the namespace identity `<spec>/<namespace>` puts the spec into every fact's namespace, so facts are no longer agnostic to it. The keystone is gone, and the grouping returns as a spec, in a shape the dissolution never weighed: **wall the rules, free the facts.** The boundary it rejected walled facts too; this one does not.

## Settled this session

- A namespace body *defines* a namespace (not declares); definition is total. The write is always an upsert, create-or-replace of the whole namespace. Omitting a relationship removes it.
- Two envelopes, one shape: a single body (name out of band, on the endpoint) and a block (`namespaces:` map). A block is one atomic batch, order-free — names are forward-declared and resolved against the batch plus the store, unresolved is a parse error.
- A **spec** is an owned set of namespaces, and its name is the qualifier segment of the identity: `<spec>/<namespace>`. Spec names are globally unique; namespace names are unique only within their spec; `spec/namespace` is globally unique. Two teams can each own a `documents` namespace. A namespace cannot be in two specs — the qualifier is part of identity, so `a/documents` and `b/documents` are simply distinct.
- The qualifier is enforced, not convention: the write edge refuses a mutation under a spec the caller does not own. Ownership is expressible in Kingo itself — a claim on the spec is a fact; the write edge does a [[check]] before admitting the mutation. No new domain type.
- References and consistency stay **global**. A spec is naming and ownership only, never a reference wall. A rule in `a/` may name `b/…`; a fact may cross specs (resource in one, subject in another), which is how a shared group works: `org/group:eng#member` is defined once and referenced everywhere as a subject, no per-spec copy.
- **Facts may dangle freely.** No cascade, no tombstone-gating on delete. Kingo does not "deny" — it computes containership as set algebra; a deleted relation removes a traversal path, so that path contributes the empty set and a stranded fact is on no satisfiable path. An orphan-finder tool is a possible later convenience, not a requirement.
- Deletion is gated on the **rule** consistency of *other* specs, not on facts. Strict, because a published spec is a public contract others build on and cannot be quietly broken.

## Terminology

- `spec` — the owned set of namespaces. A DDD *module* (a grouping that scopes and organizes without isolating), not a bounded context, which would wall the model. It clumps namespaces by domain and scopes their local names; it does not isolate references.
- `rules` — the definitional content a spec holds: the relationships and their rewrites, the Datalog pairing against facts. Working replacement for `config`, which named nothing precise. Not yet locked.
- Open: whether a spec is a stored aggregate (its row holds the namespace collection) or the namespace is the independent stored root.

## The open fork — a contract needs something static to bind to

Strict cross-spec consistency can only bite on references that are **static**, and the current rewrite grammar has none crossing a spec. A [[computed-subject-set]] is a bare identifier, same namespace. A [[fact-to-subject-set]]'s first identifier is same namespace. Its second identifier is the only cross-spec reach, and it resolves at evaluation in whatever spec the walked-to facts land in — dynamic, not a static reference to a named spec.

So "strict across the spec" currently has nothing cross-spec to enforce; it collapses to intra-spec integrity, which the atomic push already gives. Every real cross-spec dependency rides on facts, which dangle freely. The fork:

1. **Add static qualified references** to the rule grammar: a term like `drive/documents#viewer`, an explicit import. The dependency becomes visible, checked at publish (target must exist), protected on delete (RESTRICT). The contract gets teeth, but a spec no longer validates in isolation — it needs the specs it imports.
2. **Keep cross-spec coupling all-dynamic** — no qualified references; cross-spec reach stays fact-driven and evaluation-resolved. Specs validate independently, but the "unbreakable contract" is soft: a deleted relation silently empties a consumer's paths. "Strict" then honestly means intra-spec only.

Mark leans 1. Unresolved.

## Corpus ripples to reconcile (flagged, not swept)

- [[namespace]] — the locked entry says a namespace name is "globally unique within an installation." Now it is unique only within its spec; `spec/namespace` is the globally-unique identity.
- [[schema-dissolves-into-administration]] — its "hierarchical organization is naming convention" clause becomes "an enforced spec qualifier"; its retirement of the grouping as a domain type is reopened by the keystone above. Its `[[schema]]` links now dangle; the term is [[spec]].
- [[namespace-documents]] — its "removal is not expressible" and "cannot delete a namespace a fact uses" are reversed here: facts dangle freely. Its framing as a *namespace* document is in question now the spec is the owned unit.
- [[dissolve-schema-into-administration]] and [[evaluation-context]] — still reference `[[schema]]`; the spec rename has not reached them.
