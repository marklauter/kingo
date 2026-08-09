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

# Actors: primary, supporting, adversarial

Primary actors have goals.
Supporting actors have drives.

A primary actor has a conditional goal: a desired state paired with values. A supporting actor has a drive, derived from a tension related to a primary actor's value. An adversary opposes a primary actor's goals or values or a secondary actor's drives. An adversary is an actor the system exists to defeat.

A heading of the form `Child → Parent` marks a specialization. It shares the parent's goal and adds or intensifies conditions.

## Primary actors

### Resource Owner

#### Goal

Resources are reached by exactly the actors the owner intends.

#### Conditions

- Immediacy — a revocation must not be outraced by a stale read.
- Provability — the owner can show afterward that the intent held.

### Delegating Owner → Resource Owner

#### Conditions

- Delegation — the intent holds when another party states it on the owner's behalf.

### Creator → Delegating Owner

#### Conditions

- Simplicity — tolerance for administrative work is near zero. The intent is stated through mechanisms like a share control.

### Regulated Owner → Resource Owner

#### Conditions

- Provability dominates far enough that they would rather refuse a legitimate access than lose the record of it.

### Requester

#### Goal

The requester has what they came for.

#### Conditions

- Immediacy.
- No refusal the owner did not intend.

### Agent Requester → Requester

#### Conditions

- No more than the party who delegated to the agent.

### Bulk Requester → Requester

#### Conditions

- One goal covers many resources, to render a list or a search result.

### Human Requester → Requester

Standing unruled.

#### Conditions

- Feels immediacy as latency and a wrong refusal as a support ticket. Can be told to ask their administrator.

### Service Requester → Requester

Standing unruled.

#### Conditions

- No way to interpret a refusal. Retries or fails.

### Regulator

Provisional — not yet ruled as primary.

#### Goal

Access to regulated resources is controlled and demonstrable, for every holder in the jurisdiction.

#### Conditions

- Binding whether or not the holder would have chosen it.

### Statutory Regulator → Regulator

#### Conditions

- Binds by law within a jurisdiction. The owner had no say.

### Contractual Regulator → Regulator

#### Conditions

- Binds by an agreement the owner signed — a customer's terms, an industry council's standard.

### Internal Policy Regulator → Regulator

#### Conditions

- The organization binding itself. Weakest consequence, same shape.

## Adversaries

### Intruder

#### Goal

The intruder has what they were not meant to have.

#### Conditions

- Without being noticed.

### Insider

#### Goal

The insider's legitimate reach serves the insider's own purpose.

#### Conditions

- Without it looking different from the insider's ordinary work.

### Saboteur

#### Goal

The judgment cannot be reached.

#### Conditions

- The outage reads as a failure of the system.

### Tamperer

#### Goal

The record does not show what the tamperer did.

#### Conditions

- The record still reads as intact.

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
