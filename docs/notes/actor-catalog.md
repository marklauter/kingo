---
title: Actor catalog
type: note
summary: The four party types, and the actors and stakeholders derived under them
tags: [architecture, actors]
status: evolving
---

<design notes>

Four peer types, sorted by what the system does with what the party holds.

- primary actor : at the boundary, holds a conditional goal the system serves. Yields use cases and constraints.
- supporting actor : at the boundary, holds a drive — a standing pressure to act. Yields use cases.
- stakeholder : holds an interest the system serves, never acts on the system. Yields constraints.
- nefarious actor : at the boundary, holds a conditional goal the system negates. Yields constraints.

- Primary and nefarious share the conditional-goal shape and differ on served against negated. That difference is the type, not a specialization.
- Adversarial is a mode any actor can occupy, which is why no actor is trusted beyond what its use cases need. The nefarious actor is an entry in the catalog, not a class over the other three.
- A nefarious actor's constraint can force a use case owned by another actor — detection belongs to the Monitor. The genealogy holds without the nefarious actor owning the use case.
- Only humans are actors, because only humans hold goals. A system is a system, however sophisticated: an AI agent is a non-deterministic system, not an actor. Software at the boundary carries the goal of the party that operates it.
- Actor against stakeholder is a boundary test: an actor interacts with Kingo, a stakeholder holds an interest served through it.
- Derivation runs stakeholder interest → what must hold for it to be served → who must act for it to hold → that actor's drive → use cases → the system that fits. Deriving actors from the feature set is the wrong direction.
- Unruled: whether a nefarious actor must act on Kingo to be catalogued here. It decides how many nefarious actors there are.

</design notes>

# Actor catalog

## Candidates

- steward - an agent of the resource owner
- content creator


## Primary actors

- resource owner
  - goal: to deter, and limit exposure to, theft, damage, and disclosure of resources.
  - condition: TBD
- tenant → resource owner
  - goal: to limit the scope of theft, damage, or disclosure of resources through compartmentalization.
  - condition: TBD
- author
  - goal: to express the resource owner's intent.
  - condition: can verify the intent holds as expressed.
- theory author → author
  - goal: to express the resource owner's intent without enumerating instances.
  - condition: TBD
- fact author → author
  - goal: to express the resource owner's intent by enumerating instances, and keep them current.
  - condition: TBD
- auditor
  - goal: to know whether decisions matched expressed intent, and prove it.
  - condition: the record is complete, tamper-evident, and cannot be silently disabled.
- secops
  - goal: to detect exposure early enough that it can still be limited.
  - condition: TBD
- operator (SRE)
  - goal: to keep every Kingo service available.
  - condition: service level indicators are measurable against defined objectives — latency, traffic, errors, saturation.
- investigator (DFIR)
  - goal: to reconstruct a specific incident.
  - condition: any past decision can be re-derived at its recorded kookie.
- TBD - states policy in plain language, cannot compose rewrites
  - goal: to communicate effectively with authors
  - condition: TBD

## Supporting actors

## Stakeholders

## Nefarious actors

## Tensions

- The record can prove the expression was followed, never that the expression was right. Intent never enters Kingo, so conformance to expression is mechanically checkable and conformance to intent is not.

## Open

- The System Owner as primary actor is proposed, not ruled: the party that operates a system holding resources it does not own, whose conditional goal is that every access to those resources is decided by the resource owner's intent. Sub-question: one primary actor with the enterprise that owns its resources outright as a specialization, or two.
- The actor list in the operation-set note predates this one and should point here instead.
