---
title: Deriving the operation set
type: note
summary: "Kingo's operations derive from seven core ReBAC system use cases: decision production, decision event notification, theory and fact administration, administration event notification, "
tags: [architecture, services]
created: 2026-08-08
status: evolving
cites:
  - "[[why-kingo-must-exist]]"
  - "[[grouping-the-apis-into-services]]"
  - "[[authz-event-logging]]"
  - "[[rewrite-interpreters]]"
  - "[[storage-versioning-design]]"
  - "[[precomputed-closure-index-for-the-hot-path]]"
---

# The operation set derives from an informal use case analysis of a typical ReBAC system

Kingo's operations derive from ReBAC system use cases: decision production, decision event notification, theory and fact administration, administration event notification, 

## Needs

Stated by Mark, 2026-08-08. They build on each other.

1. Produce a decision — the reason the system exists.
2. Administer the theories and facts a decision evaluates.
3. Notify external systems when the catalog or the graph changes.
4. Let an administrator read an expansion, so they can reason about the rewrites they authored.
5. Troubleshoot a decision that changed against expectation.
6. Audit decisions and writes for legal purposes.
7. Trace decisions and replay them at their recorded kookie.

## Actors

The CISO organization, stated by Mark 2026-08-08, plus the two the needs already imply.

- **Caller** — the policy enforcement point. Asks Check and enforces the verdict. Sits outside Kingo, and is the highest-volume actor by orders of magnitude.
- **Administrator** — authors theories, asserts facts, reads expansions. Needs 2, 4, and 5.
- **SecOps** — detection and monitoring. Lives in the log stream, tunes SIEM rules, triages alerts. The closest analogue to an on-call SRE.
- **DFIR** — owns the investigation once an alert becomes an incident. Inside SecOps at smaller organizations, separate at large ones. The only actor who needs replay: reconstruction means re-running the decision at its recorded kookie, not reading its verdict.
- **GRC** — owns the control framework, retention policy, and evidence collection for compliance audits. Defines what must be logged; SecOps consumes it.
- **Internal Audit** — outside the CISO organization, reporting to the board's audit committee. Independence is the point: they audit whether security's own controls work. External auditors sit further out still.
- **Security Engineering** — builds the plumbing: the log archive, its retention locks, the guardrails.

GRC and Security Engineering constrain configuration rather than call an operation, which is why neither adds one.

## Operations

- **Check** ← need 1.
- **Write** ← need 2. Theory and facts.
- **Read** ← need 2. Inspection of facts; an administrator cannot edit a graph they cannot see.
- **Expand** ← needs 4 and 5. One service with two directions (Mark, 2026-08-08). Forward materializes a subjectset's rewrite tree. Reverse answers "which resources can this subject reach," the question behind every filtered list view — the same rewrite evaluation entered from the opposite node. Reverse is not Read, which returns stored facts with no rewrite evaluation and so cannot follow a subjectset. Provisional: revisit if a product consumer's measured load diverges from the administrative one.
- **Observability** ← needs 3, 6, and 7. Four services, derived below.

Replay is not an operation. Replaying a traced request is Check or Expand run at the Kookie the trace recorded. Point-in-time evaluation falls out of the settled storage shapes — interval-stamped fact rows and the supersession-closed theory changelog — so replay costs a retention window rather than an API. Its purpose is so an administrator can build tests for configuration mutations.

## Why the record exists

Six purposes, from enterprise security practice (Mark, 2026-08-08):

- **Forensics.** Reconstruct the sequence of actions that produced an outcome, after the point where anyone can still be asked.
- **Attribution and non-repudiation.** Shared accounts and assumed roles make "it wasn't me" cheap. An append-only record makes an action stick to an identity.
- **Detection.** Feed the stream to a SIEM and alert on patterns — credential use from a new geography, mass access to a sensitive prefix, the trail itself being disabled.
- **Compliance.** The auditor's question is not "are you secure" but "show me the evidence." The record is the evidence.
- **Deterrence.** People behave differently when their actions are attributable.
- **Operational forensics.** "Why did this change" — a deploy pipeline, a migration, a teammate in a console — beyond security.

Four design constraints follow. The record is tamper-evident, and the audited party cannot edit it. It is stored outside the blast radius of the systems it covers. It is retained past the detection gap, since a breach can go months undiscovered. Its timestamps are synchronized, so events from different systems correlate into one timeline.

Kingo already satisfies the last: `Decision` carries a wall timestamp distinct from its kookie, so an investigator finds decisions in an incident window by wall time, then replays each at its own pin.

Reproducibility is the argument for Kingo owning the decision record rather than shipping it to an outside sink. An outside sink stores and searches decision records; only Kingo can re-derive one, because replaying a verdict needs the graph and the catalog at that kookie. Compliance asks for evidence, and a record that can be re-derived is stronger evidence than one that can only be read. Forensics has the same shape: "why was this allowed" is answered by re-running the decision, not by reading its verdict.

The two constraints Kingo does not yet satisfy are constraints on how it owns the record rather than arguments against owning it. Tamper-evidence means append-only, with expiry authority held outside the services being audited. Blast radius means a store separate from the fact store.

Internal Audit adds a requirement no other actor does: verifying the trail was never disabled. That makes Kingo's own audit configuration — retention, sink target, emission on or off — an input event in its own right, and it makes a gap in the record something that must be detectable rather than silent.

## Event classes

Two, split by direction (Mark, 2026-08-08): input events are writes, output events are checks.

- **Input events** — theory changes and fact writes. Answers "why did Jim lose access" and "why was the attacker able to breach." Administrator audience, low volume. The changelog already is this record, so it exists whether or not observability consumes it.
- **Output events** — Check verdicts. Answers "who was accessing system X when event Y occurred." SRE and security audience, feeding alerting sinks. High volume, scaling with the hot path, so this store can grow larger than the fact store.

Each class is read two ways — query the retained log, or listen to it live from a cursor — giving four independently deployable services: input-query, input-listen, output-query, output-listen (Mark, 2026-08-08). Input-listen is Watch as it stands today.

The load asymmetry between the classes is what forces the split rather than merely describing it. Output events are produced once per Check and input events once per write, so the two differ by orders of magnitude at every point: emission rate, store size, and query cost. Co-hosting them provisions the low-load pair for the high-load pair's peak.

Kingo owns its decision history rather than handing retention and query to an outside system (Mark, 2026-08-08).

The authorization event logging note covers emission into a sink and stops; all four services read back out. That note also splits the classes as management and data events, which needs reconciling with the input/output split.

## Families

Decision (Check), Administration (Write, Read, Expand), Observability (input-query, input-listen, output-query, output-listen). Check is the decision point; enforcement is the caller's and sits outside Kingo (Mark, 2026-08-08). A family groups services; it is not itself a deployable unit, and load profile rather than family membership is what decides a deployment boundary — so placing an operation in a family carries no deployment consequence. The four observability services are independently deployable (Mark, 2026-08-08).

## Services

Eight, one per operation. Load profile is the deployment boundary, except where an invariant forces one.

- **Check** — Decision. The hot path.
- **Write** — Administration. One service by invariant rather than by profile: the drift guard needs a sole writer of both theories and facts, so splitting them would put the invariant across a service boundary.
- **Read** — Administration. Reaches the fact store alone, no catalog and no interpreter. I/O-bound.
- **Expand** — Administration. Reaches the fact store, the catalog, and the rewrite engine. CPU-bound on tree materialization.
- **input-query**, **input-listen** — Observability. Low load.
- **output-query**, **output-listen** — Observability. High load, scaling with Check.

Read and Expand do not co-host (Mark, 2026-08-08). Their dependency sets differ, so a co-hosted Read would redeploy on every rewrite-engine change, and their resource profiles differ.

## Open

- Reverse expansion has no derivation path. The rewrite interpreters do not supply one and are not meant to: Contains is a membership predicate, and Expand materializes one relation's rewrite tree downward. Requirement 6 of the rewrite-interpreters todo — the evaluator needs no reverse lookup — is unaffected, because reverse is an administration capability rather than an evaluator one. Finding every subjectset whose closure contains an identity is the precomputed closure index's problem.
