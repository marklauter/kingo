---
title: Actor catalog
type: note
summary: "Three primary actors — resource owner, requester, regulator — five adversaries who exist on their own, and nine supporting actors, each with a drive and a genealogy tracing back to a primary actor's value condition."
tags: [architecture, actors]
created: 2026-08-08
status: evolving
cites:
  - "[[why-kingo-must-exist]]"
---

# Every actor traces to a primary actor's value or to an adversary

An actor pursues a goal and decides in service of it. Kingo is the system under design, not an actor. A resource is a name, not an actor.

A primary actor has a conditional goal: a desired state plus the values they hold about being in it. A supporting actor has a drive, and the drive is derived from a tension on some primary actor's value. An adversary has neither — they exist on their own, and their existence is what creates the tensions that produce the security surface.

## Primary actors

**Resource owner.** *My resources are reached only by the actors I intend — without me present, effective immediately, and provably.*

- **Without me present** — the owner is asleep, offline, or gone when the access happens, and it still has to be judged.
- **Effective immediately** — a revocation must not be outrun by a stale read.
- **Provably** — the owner can show afterward that the intent held.

**Requester.** *I have what I came for — immediately, without asking a human, and without refusal the owner did not intend.*

The requester's goal alone produces no system; an unlocked door satisfies it. It matters because a wrong refusal is a failure, not a safe default. Without this actor in the model, refusing everything would be a correct design.

**Regulator.** *Access to regulated resources is controlled and demonstrable, for every holder in the jurisdiction whether or not they would choose it.* Provisional — not yet ruled.

The regulator's goal is not derived from the owner's. It binds owners who would not have chosen to be bound, which is what makes it primary rather than a restatement of the owner's provability value.

## Adversaries

They are not spawned by anyone's values. Each one is why a piece of the system exists.

- **Intruder** — reaches resources they were never intended to reach. The base case, and the reason restriction exists at all.
- **Insider** — is intended to reach some resources and uses that reach for another purpose. Restriction cannot refuse them, because their access is legitimate at the moment they use it. Only the record catches them, which is why the record is not optional.
- **Saboteur** — attacks the availability of the judgment rather than the resource. A decision that cannot be reached is a decision, once fail-closed policy is set.
- **Tamperer** — alters the record after the fact to hide what they did. The reason the record is append-only and the reason expiry authority sits outside the audited services.
- **Incompetent** — not malicious, same effect. Grants too broadly, removes a namespace still in use, misstates the intent. Counts with the others because an unenforced intent fails the same way whatever the motive.

## Supporting actors

Each has a drive and a genealogy back to a primary actor's value condition.

**System owner.** Drive: fiduciary responsibility. Holds the resources without owning them, so the owner's *provably* and *only by whom I intend* values fall on a party who is not the owner. Genealogy root: the resource owner's protection values. Not primary — fiduciary responsibility is a drive, and it is derived.

**Administrator.** Drive: express the owner's intent in terms the system can enforce. Spawned by *without me present*: an intent that has to survive the owner's absence has to be written down, and someone writes it. On a small deployment the resource owner and the administrator are the same person, which hides the split without removing it. Genealogy root: without me present.

**Enforcement point.** Drive: act on the verdict. Spawned by the same value from the other side — something has to ask before the access and refuse after the answer. Sits outside Kingo, and asks more often than every other actor combined. Genealogy root: without me present.

**Monitor.** Drive: detect misuse while it is still happening. Spawned by *effective immediately* meeting the Intruder and the Insider: a record read next quarter does not stop anything. Consumes the live stream. Genealogy root: effective immediately.

**Incident responder.** Drive: reconstruct what happened. Spawned by the adversaries plus *provably*. The only actor who needs replay, because reconstruction means re-running the decision at its recorded Kookie rather than reading its verdict. Genealogy root: provably.

**Internal auditor.** Drive: verify independently that the controls worked. Spawned by *provably* colliding with the system owner's fiduciary drive — the party holding the record cannot be the party attesting to it. Independence is the whole content of the role. Genealogy root: provably, regulator's demonstrability.

**External auditor.** Drive: attest to a third party. Same genealogy as the internal auditor, further out, and answerable to someone outside the organization entirely. Genealogy root: regulator's demonstrability.

**Compliance officer.** Drive: define what must be recorded and for how long. Translates the regulator's goal into a configuration the deployment holds. Constrains configuration and calls no operation. Genealogy root: regulator's demonstrability.

**Platform engineer.** Drive: build and configure the enforcement and recording plumbing. Spawned by the system owner's fiduciary drive needing an operational form. Constrains configuration and calls no operation. Genealogy root: the resource owner's protection values, through the system owner.

## Tensions

Each one either spawned an actor above or shapes the design directly.

- **Protect against share.** The owner's protection values against the requester's goal. Every restriction refuses someone legitimate when it is wrong, and refusing everything is not a safe default.
- **Immediacy against cost.** Judging each access takes work, and the requester waits through it. Produces the hot path and everything spent on it.
- **Provability against volume.** A recorded decision is a write for every read. Produces the emission design and the load asymmetry between the two event classes.
- **Independence against custody.** The system owner holds the record and is also audited by it. Produces append-only storage and expiry authority held outside the audited services.
- **Incompetence against expressiveness.** A language rich enough to state what the owner means is rich enough to state something they did not. Produces construction-time validation and the write-side drift guard.

## Open

- The regulator is unruled as a primary actor. If it is not primary, the internal auditor, external auditor, and compliance officer lose their genealogy root and have to trace to the owner's *provably* value alone.
- The requester's conditional goal is proposed, not ruled.
- The actor list in the operation-set note predates this one and should point here instead.
