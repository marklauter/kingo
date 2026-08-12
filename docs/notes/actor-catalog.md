---
title: Actor catalog
type: note
summary: todo
tags: [architecture, actors]
created: 2026-08-08
status: evolving
cites:
  - "[[why-kingo-must-exist]]"
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
- Actor against stakeholder is a boundary test: an actor interacts with Kingo, a stakeholder holds an interest served through it. Resource Owner and its specializations, Requester, Regulator, and Organization fall on the stakeholder side.
- Derivation runs stakeholder interest → what must hold for it to be served → who must act for it to hold → that actor's drive → use cases → the system that fits. Deriving actors from the feature set is the wrong direction.
- Unruled: whether a nefarious actor must act on Kingo to be catalogued here. It decides whether Intruder and Insider stay, and how many nefarious actors there are.

# Actors: primary, supporting, adversarial

Primary actors have goals.
Supporting actors have drives.

A primary actor has a conditional goal: a desired state paired with values. A supporting actor has a drive, derived from a tension related to a primary actor's value. An adversary opposes a primary actor's goals or values or a secondary actor's drives. An adversary is an actor the system exists to defeat.

A heading of the form `Child → Parent` marks a specialization. It shares the parent's goal and adds or intensifies conditions.

## Primary actors

system owner
resource owner


### Resource Owner

#### Goal

<todo: all goals require "to ..." statement - a desired outcome>

To prevent access of owned resources by untrusted actors.

#### Conditions

- Immediacy — a revocation of trust must not be outraced by a stale read.
- Provability — the owner can show afterward that the trust-intent held.

### Delegating Owner → Resource Owner

#### Conditions

- Delegation — the intent holds when another party states it on the owner's behalf.

### Creator → Delegating Owner

#### Conditions

- Simplicity — tolerance for administrative work is near zero. The intent is stated through mechanisms like a share control.

### Regulated Owner → Resource Owner

#### Conditions

- Provability — dominates far enough that they would rather refuse a legitimate access than lose the record of it.

### Resource requester (this isn't a primary actor - it's supporting and has a drive, not a goal)

#### Goal

Owned resource are reached by exactly the actors the owner trusts.

Gains access to the resouce.

#### Conditions

- Immediacy.
- No refusal the owner did not intend.

### Regulator (this isn't a primary actor - it's supporting and has a drive, not a goal)

Provisional — not yet ruled as primary.

#### Goal

Access to regulated resources is controlled and demonstrable, for every holder in the jurisdiction.

#### Conditions

- Binding whether or not the holder would have chosen it.

### Statutory Regulator → Regulator (this isn't a primary actor - it's supporting and has a drive, not a goal)

#### Conditions

- Binds by law within a jurisdiction. The owner had no say.

### Contractual Regulator → Regulator (this isn't a primary actor - it's supporting and has a drive, not a goal)

#### Conditions

- Binds by an agreement the owner signed — a customer's terms, an industry council's standard.

### Internal Policy Regulator → Regulator (this isn't a primary actor - it's supporting and has a drive, not a goal)

#### Conditions

- The organization binding itself. Weakest consequence, same shape.

## Adversaries

### Intruder

#### Goal

To gain illicit access to resources to service is own purposes.

#### Conditions

- Without being detected.

### Insider

#### Goal

To misuse legitimate access to serve his own purpose.

#### Conditions

- Without it looking different from the insider's ordinary work.

## Supporting actors

### System Owner

#### Drives

- Fiduciary responsibility. Holds the resources without owning them.

#### Genealogy

- The resource owner's protection values.

### Administrator

#### Drives

- Express the owner's intent in terms the system can evaluate.

#### Genealogy

- Resources are reached only by actors the owner intends.

### Enforcement Point

#### Drives

- Ask before the access and act on the verdict.

#### Genealogy

- Resources are reached only by actors the owner intends, from the other side.

### Monitor

#### Drives

- Detect misuse in progress.

#### Genealogy

- Immediacy.

### Incident Responder

#### Drives

- Explain the incident once a detection becomes one.

#### Genealogy

- Provability.

### Internal Auditor

#### Drives

- Verify independently that the controls worked.

#### Genealogy

- Provability, and the regulator's demonstrability.

### External Auditor

#### Drives

- Attest to a third party.

#### Genealogy

- The regulator's demonstrability.

### Compliance Officer

#### Drives

- Define the control framework: what must be recorded, and for how long.

#### Genealogy

- The regulator's demonstrability.

### Platform Engineer

#### Drives

- Build and configure the enforcement and recording plumbing.

#### Genealogy

- The resource owner's protection values, through the System Owner.

## Tensions

- Protect against share — the owner's protection values against the requester's goal. Every restriction refuses someone legitimate when it is wrong, and refusing everything is not a safe default.
- Immediacy against cost — judging each access takes work, and the requester waits through it.
- Provability against volume — a recorded decision is a write for every read.
- Independence against custody — the System Owner holds the record and is also audited by it.
- Incompetence against expressiveness — a language rich enough to state what the owner means is rich enough to state something they did not.

## Open

- **Organization is a primary actor, agreed 2026-08-08, not yet written.** Proposed goal: the organization is not harmed by how its resources are reached — covering liability, penalty, and reputation, none of which any individual owner's goal reaches. **Enterprise → Organization** is its specialization, and the condition it adds is separation: the scale at which the Administrator, Compliance Officer, and Internal Auditor are three different people. Below that they are one person and those tensions collapse.
- **Internal Policy Regulator moves under Organization.** A Regulator is external to the organization by definition (ruled 2026-08-08), so an organization binding itself is the Organization acting, not a Regulator.
- The Compliance Officer is internal to the organization while its genealogy stays the Regulator's demand. That internal/external line is the same one that makes the Internal Auditor's independence meaningful, and it decides who may hold expiry authority over the record.
- Every actor in this catalog is external to Kingo. The primary actors reach it only through intermediaries — the third-party application as Enforcement Point, and someone as Administrator — so the actors whose goals justify the system are not the actors who call it. The API is shaped by the intermediaries and the intermediaries by the primary actors' goals; conflating the two layers is how an API ends up modeled on the caller's convenience instead of the owner's intent.
- Regulated Owner's condition is still off-pattern. Proposed: Provability — not the owner's trade to make. An unregulated owner can trade provability against cost and chooses not to; a regulated one has the trade removed, which is a lost degree of freedom rather than a stronger weighting.
- Produces sections were cut 2026-08-08. In a use case model what an actor produces is use cases, and the design consequences hang off those. The next step is the use cases.
- The Regulator is unruled as a primary actor. If it is not primary, the Internal Auditor, External Auditor, and Compliance Officer lose their genealogy root and have to trace to the owner's provability value alone.
- The Requester's conditional goal is proposed, not ruled.
- The Human Requester and Service Requester differ in how they receive a refusal, and nothing in Kingo is yet known to change because of it. Cut or keep.
- The Agent Requester's condition — no more than the party who delegated to the agent — has two readings and neither is ruled. Either the Enforcement Point substitutes the delegator's identity and Kingo never sees the agent, which is consistent with caller identity living in the envelope; or Kingo models the delegation itself and the answer depends on who is asking, which that ruling forbids.
- The System Owner as a Resource Owner specialization makes the theory, the graph, and the record resources in their own right, so administrative access to Kingo becomes an authorization question Kingo could answer about itself. That is a scoping decision, not a free consequence.
- The actor list in the operation-set note predates this one and should point here instead.
