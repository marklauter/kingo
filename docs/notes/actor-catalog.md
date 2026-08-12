---
title: Actor catalog
type: note
summary: The four party types, and the actors, stakeholders, and nefarious actors derived under them
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
- A nefarious actor's constraint can force a use case owned by another actor — detection belongs to the security operator. The genealogy holds without the nefarious actor owning the use case.
- Only humans are actors, because only humans hold goals. No system is an actor, however sophisticated, so an AI agent is a non-deterministic system rather than an actor. Software at the boundary carries the goal of the party that operates it.
- Actor against stakeholder is a boundary test: an actor interacts with Kingo, a stakeholder holds an interest served through it.
- Derivation runs primary actor → conditional goal → tensions → supporting actors, each with a drive → use cases → the system that fits. A conditional goal is a desired state plus the value conditions the actor holds about being in it, and a tension is one of those conditions meeting reality. Deriving actors from the feature set is the wrong direction.
- A primary actor exists whether or not the system does. A supporting actor is created by the design, to make sure the system delivers a primary's conditional goal. An elevator inspector has no reason to exist in a world without elevators.
- A goal states a desired end state, never the path to it. The gift test: if a genie granted the end state outright and the actor is satisfied, it is a goal; if the shortcut skips something the actor wanted, it is a task.
- Name an actor from the world the system does not exist in. Passenger presupposes the elevator; traveler does not.
- A nefarious actor is catalogued here only if it acts on Kingo. An intruder at the relying party's front door, or an insider misusing access Kingo correctly granted, is offstage: Kingo answers right and the harm happens anyway, so neither yields a constraint Kingo can carry.

</design notes>

# Actor catalog

## Primary actors

- resource owner
  - goal: to limit exposure to theft of, damage to, and disclosure of resources.
  - condition (attribution): every access is attributable to someone.
  - condition (fidelity): no access is granted that their expressed intent does not allow.
  - condition (immediacy): a change of intent takes effect before the next access.
  - condition (availability): resources are reachable by those they are meant for.
- tenant → resource owner
  - goal: to be unreachable and unaffected by any other tenant.
  - condition (blast radius): one tenant's load or outage cannot degrade another's.

## Supporting actors

- author
  - drive (translation): to express the resource owner's intent.
  - condition (verifiability): can verify the intent holds as expressed.
  - condition (immediacy): what they write is in force for the next judgment.
- theory author → author
  - drive (generalization): to express the resource owner's intent without enumerating instances.
  - condition (expressiveness): the language can state the intent without falling back to enumeration.
  - condition (predictability): the effect of a definition can be seen before it takes effect — its blast radius is unbounded.
- fact author → author
  - drive (currency): to express the resource owner's intent by enumerating instances, and keep them current.
  - condition (rate): writes keep pace with how fast relationships change in the world.
  - condition (reconciliation): the enumeration can be checked against the world it mirrors, so drift is detectable.
- auditor
  - drive (verification): to know whether decisions matched expressed intent, and prove it.
  - condition (integrity): the record is complete, tamper-evident, and cannot be silently disabled.
- security operator (SecOps)
  - drive (detection): to detect exposure early enough that it can still be limited.
  - condition (latency): decisions and writes reach the stream fast enough to act on.
  - condition (correlation): each event carries enough context to correlate against other systems.
- system operator (SRE)
  - drive (continuity): to keep every Kingo service available.
  - condition (observability): service level indicators are measurable against defined objectives — latency, traffic, errors, saturation.
- investigator (DFIR)
  - drive (explanation): to reconstruct a specific incident.
  - condition (reproducibility): any past decision can be re-derived at its recorded kookie.

## Stakeholders

- content creator - owns resources held by a relying party; never interacts with Kingo directly
  - interest: that what they upload reaches only the audience they choose.
  - constraint (translation): they express intent through a relying party's interface, never to Kingo, so their intent reaches the system only as someone else's expression of it.
- accessor - the person whose request is judged; never interacts with Kingo directly
  - interest: to reach the resources they are entitled to.
  - constraint (silent overgrant): an accessor reports a wrong deny and never a wrong allow, so no feedback channel exists for over-permissiveness.
- specifier - states policy in plain language, cannot compose rewrites
  - interest: that what the authors express is what they meant.
  - constraint (examples): intent can be checked against concrete cases, without reading the theory.

## Nefarious actors

- forger
  - goal: to hold a fact that grants them access they were never given.
- quiet widener
  - goal: to broaden what an existing relation means, so facts already written grant more than they did.
- log tamperer
  - goal: to leave no usable record of what they did.
- race runner
  - goal: to act inside the window between a revocation and its effect.
- tenant escaper
  - goal: to reach resources belonging to a tenant that is not theirs.
- cartographer
  - goal: to learn who relates to whom, without needing access to anything.
- exhauster
  - goal: to make the decision path unavailable, so the enforcer fails open.
- credential thief
  - goal: to ask questions as an enforcer they are not.

Offstage, acting on the relying party rather than on Kingo: the intruder at its front door, and the insider misusing access Kingo correctly granted.

## Tensions

- Wrong denies are reported, wrong allows are not. The only actor positioned to notice a decision is the accessor, and they have no incentive to report an allow they shouldn't have had. Over-permissiveness is therefore invisible in production and must be caught by the record or by examples, never by users.
- The record can prove the expression was followed, never that the expression was right. Intent never enters Kingo, so conformance to expression is mechanically checkable and conformance to intent is not.

## Open

- Is tenant isolation structural or decided? Tenant is a PAP concept and must also be a PDP one, or a caller scoped to one tenant could ask about another's resources. Recommendation: structural — the account scopes the request before the engine runs, so no theory a tenant writes can reach past it. Deciding isolation with the same engine it compartmentalizes makes a theory bug a cross-tenant breach, which contradicts the tenant's own goal.
- `nefarious` names intent, but the same behaviors occur unwittingly — an exhauster can be a runaway retry loop, and a quiet widener is usually someone who misread their own rewrite. The constraints are identical either way. Either the type name is wrong, or the entries need to be stated as behaviors rather than as people.
- A content creator is a resource owner who never touches Kingo, yet resource owner is catalogued as a primary actor. Either resource owner is an abstract root whose subtypes split across actor and stakeholder, or the primary entry needs narrowing to the owners who reach the boundary.
- The actor list in the operation-set note predates this one and should point here instead.
