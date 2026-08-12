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
- Actor against stakeholder is a boundary test: an actor interacts with Kingo, a stakeholder holds an interest served through it.
- Derivation runs stakeholder interest → what must hold for it to be served → who must act for it to hold → that actor's drive → use cases → the system that fits. Deriving actors from the feature set is the wrong direction.
- Unruled: whether a nefarious actor must act on Kingo to be catalogued here. It decides how many nefarious actors there are.

</design notes>

# Actor catalog

## Primary actors

## Supporting actors

## Stakeholders

## Nefarious actors

## Tensions

## Open

- The System Owner as primary actor is proposed, not ruled: the party that operates a system holding resources it does not own, whose conditional goal is that every access to those resources is decided by the resource owner's intent. Sub-question: one primary actor with the enterprise that owns its resources outright as a specialization, or two.
- The actor list in the operation-set note predates this one and should point here instead.
