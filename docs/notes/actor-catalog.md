---
title: Actor catalog
type: note
summary: "Three primary actors — resource owner, requester, regulator — five adversaries with goals of their own, and nine supporting actors, each with a drive and a genealogy tracing back to a primary actor's value condition."
tags: [architecture, actors]
created: 2026-08-08
status: evolving
cites:
  - "[[why-kingo-must-exist]]"
---

# Actors: primary, supporting, adversarial

Primary actors have goals.
Supporting actors have drives.

A primary actor has a conditional goal: a desired state paired with values. A supporting actor has a drive, derived from a tension related to a primary actor's value. An adversary opposes a primary actor's goals or values or a secondary actor's drives. An adversary is an actor the system exists to defeat.

## Primary actors

### Resource Owner

The party whose resources the system exists to protect.

- Goal — *my resources are reached only by the actors I intend — effective immediately, and provably.*
- Conditions
  - Effective immediately — a revocation must not be outrun by a stale read. Eventual consistency is a legitimate design, so immediacy is a choice.
  - Provably — the owner can show afterward that the intent held. Deciding and forgetting is a legitimate design, so provability is a choice.
- Naming — "owner" alone collides with the party who owns the system rather than the material in it. "Publisher" and "author" name how the resource arrived, one path among several.

Specializations share the desired end state and differ in who states the intent, or in which condition dominates.

#### Delegating Owner

Hands the day-to-day intent to someone else.

- The split that makes the Administrator a separate party rather than the owner wearing a second hat.

#### Individual Owner

Holds their own material on someone else's platform.

- Will not write a theory. Clicks a share control.
- Tolerance for administrative work is near zero.

#### Regulated Owner

Holds material a regulator binds.

- Provability dominates far enough that they would rather refuse a legitimate access than lose the record of it.
- Where fail-closed stops being a default and becomes a requirement.

### Requester

The party who wants to reach a resource they do not own.

- Goal — *I have what I came for — immediately, and without refusal the owner did not intend.*
- Standing — the goal alone produces no system; an unlocked door satisfies it. It is in the model because without it, refusing everything is a correct design.
- Naming — "subject" is locked in the glossary as a modeled value, so it names data rather than a party. "User" is overloaded in systems design and excludes clients and agents that are not human. "Caller" names the machinery that asks on the requester's behalf, a supporting actor.

Specializations differ on who they act for, and on how many resources one goal covers.

#### Agent Requester

Acts on a party's behalf under delegated authority.

- Adds a condition no other requester has: *no more than the party who delegated to me.*
- Pressures the model hardest, because the requester's identity and the authority's identity are different values in the same question.

#### Bulk Requester

Covers many resources with one goal, to render a list or a search result.

- The desired end state is unchanged and the shape of the question inverts, from "may this party reach this resource" to "which resources may this party reach."
- Reverse expansion exists for this specialization and no other.

#### Human Requester

A person at an interface. Standing unruled.

- Feels immediacy as latency and a wrong refusal as a support ticket.
- Can be told to ask their administrator.

#### Service Requester

A program acting on its own behalf. Standing unruled.

- No patience, and no way to interpret a refusal. Retries or fails.

### Regulator

The party who requires that access to certain resources be controlled and demonstrable, whether or not the owner would have chosen it. Provisional — not yet ruled as primary.

- Goal — *access to regulated resources is controlled and demonstrable, for every holder in the jurisdiction.*
- Standing — not derived from the owner's goal. It binds owners who would not have chosen to be bound, so it is primary rather than a restatement of the owner's provability value.
- Naming — "the State" covers too little; a payment-card council and a customer's contract bind an owner the same way a statute does. "Auditor" names a party who verifies, a supporting role derived from this goal.

Specializations differ on where the obligation comes from. The demand does not vary, and the consequence of failing it does. All three impose identical requirements on Kingo and differ only in who is owed the evidence, so a design satisfying the statutory case satisfies all three.

#### Statutory Regulator

Binds by law within a jurisdiction. The owner had no say.

#### Contractual Regulator

Binds by an agreement the owner signed — a customer's terms, an industry council's standard.

#### Internal Policy Regulator

The organization binding itself. Weakest consequence, same shape.

## Adversaries

Their goals are their own and oppose the resource owner's and the system owner's. Each one is why some part of the system exists.

### Intruder

Reaches resources they were never intended to reach.

- Goal — *I have what I was not meant to have — without being noticed.*
- The base case, and the reason restriction exists at all.
- Restriction answers the desired state. The second condition is what produces the record and the Monitor.

### Insider

Is intended to reach some resources and uses that reach for another purpose.

- Goal — *I use my legitimate reach for my own purpose — without it looking different from my ordinary work.*
- Restriction cannot refuse them; their access is legitimate at the moment they use it.
- Only the record catches them, which is why the record exists.

### Saboteur

Attacks the availability of the judgment rather than the resource.

- Goal — *the judgment cannot be reached.*
- Under fail-closed policy an unreachable judgment becomes a denial, so stopping the system denies everyone.

### Tamperer

Alters the record after the fact to hide what they did.

- Goal — *the record does not show what I did.*
- The reason the record is append-only, and the reason expiry authority sits outside the audited services.

### Incompetent

Grants too broadly, removes a namespace still in use, or misstates the intent. Not malicious, same effect.

- Fails the adversary test: the goal is the Administrator's, and they fail at it. A failure mode of a supporting actor, not an actor with an opposed goal.
- The design pressure is real either way. It produces construction-time validation and the write-side drift guard.
- Kept pending a ruling on where they belong.

## Supporting actors

Each holds a drive and a genealogy back to a primary actor's value condition.

### System Owner

Holds the resources without owning them.

- Drive — fiduciary responsibility.
- Genealogy — the resource owner's protection values.
- A Resource Owner specialization and a supporting actor at once. Apply the owner's conditional goal to the system's own material — the theories, the graph, the decision record — and it reads unchanged, which is the test for a specialization.
- On a self-hosted deployment the two roles collapse into one party.

### Administrator

States the owner's intent in terms the system can evaluate.

- Drive — express the owner's intent enforceably.
- Genealogy — reached only by the actors I intend.
- An intent the system evaluates has to be written down in a form it can read, and someone writes it.
- On a small deployment the resource owner and the administrator are the same party, which hides the split without removing it.

### Enforcement Point

Asks before the access and acts on the answer.

- Drive — act on the verdict.
- Genealogy — reached only by the actors I intend, from the other side.
- Sits outside Kingo. Asks more often than every other actor combined.

### Monitor

Detects misuse while it is still happening.

- Drive — detect misuse in progress.
- Genealogy — effective immediately.
- A record read next quarter stops nothing, so immediacy applies to the detection as much as to the revocation.
- Consumes the live stream.

### Incident Responder

Reconstructs what happened once a detection becomes an incident.

- Drive — explain the incident.
- Genealogy — provably.
- The only actor who needs replay: reconstruction means re-running the decision at its recorded Kookie rather than reading its verdict.

### Internal Auditor

Verifies independently that the controls worked.

- Drive — independent verification.
- Genealogy — provably, and the regulator's demonstrability.
- The party holding the record cannot be the party attesting to it, which is the whole of the role.
- Answers to a body inside the organization but outside the chain they audit.

### External Auditor

Attests to a third party.

- Drive — attestation.
- Genealogy — the regulator's demonstrability.
- Same genealogy as the Internal Auditor, answerable outside the organization.

### Compliance Officer

Decides what must be recorded and for how long.

- Drive — define the control framework.
- Genealogy — the regulator's demonstrability.
- Translates the regulator's goal into a configuration the deployment holds.
- Constrains configuration and calls no operation.

### Platform Engineer

Builds and configures the enforcement and recording plumbing.

- Drive — build the plumbing.
- Genealogy — the resource owner's protection values, through the System Owner.
- Constrains configuration and calls no operation.

## Tensions

Each one either produced an actor above or shapes the design directly.

- Protect against share — the owner's protection values against the requester's goal. Every restriction refuses someone legitimate when it is wrong, and refusing everything is not a safe default.
- Immediacy against cost — judging each access takes work, and the requester waits through it. Produces the hot path.
- Provability against volume — a recorded decision is a write for every read. Produces the emission design and the load asymmetry between the two event classes.
- Independence against custody — the System Owner holds the record and is also audited by it. Produces append-only storage and expiry authority held outside the audited services.
- Incompetence against expressiveness — a language rich enough to state what the owner means is rich enough to state something they did not. Produces construction-time validation and the write-side drift guard.

## Open

- The Regulator is unruled as a primary actor. If it is not primary, the Internal Auditor, External Auditor, and Compliance Officer lose their genealogy root and have to trace to the owner's *provably* value alone.
- The Requester's conditional goal is proposed, not ruled.
- The Human Requester and Service Requester differ in how they receive a refusal, and nothing in Kingo is yet known to change because of it. Cut or keep.
- The Agent Requester's condition — *no more than the party who delegated to me* — has two readings and neither is ruled. Either the Enforcement Point substitutes the delegator's identity and Kingo never sees the agent, which is consistent with caller identity living in the envelope; or Kingo models the delegation itself and the answer depends on who is asking, which that ruling forbids.
- The Incompetent is not an adversary by the test above, and has no home yet.
- The System Owner as a Resource Owner specialization makes the theory, the graph, and the record resources in their own right, so administrative access to Kingo becomes an authorization question Kingo could answer about itself. That is a scoping decision, not a free consequence.
- The actor list in the operation-set note predates this one and should point here instead.
