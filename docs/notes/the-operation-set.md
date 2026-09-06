---
title: The operation set
type: note
summary: "Six operations — Check, Write, Read, Expand, Watch, Audit — deployed as eight services, because Watch and Audit each split by event class."
tags: [architecture, services]
created: 2026-08-08
status: evolving
cites:
  - "[[actor-catalog]]"
  - "[[grouping-the-apis-into-services]]"
  - "[[authz-event-logging]]"
  - "[[rewrite-interpreters]]"
  - "[[storage-versioning-design]]"
  - "[[precomputed-closure-index-for-the-hot-path]]"
---

# The operation set

## Operations

- **Check** — produces a decision.
- **Write** — asserts theories and facts.
- **Read** — returns stored theories and facts, with no rewrite evaluation. Reading theories serves the theory author's predictability condition ([[actor-catalog]]), which Expand assumes and cannot supply.
- **Expand** — materializes one relation's rewrite tree, single-level with respect to indirection ([[rewrite-interpreters]]). Carried as one operation with two directions (Mark, 2026-08-08); the reverse direction is unsettled ([[reverse-expansion-has-no-derivation-path]]).
- **Watch** — streams events from a cursor.
- **Audit** — returns retained events.

Replay is not an operation. Replaying a traced request is Check or Expand run at the Kookie the trace recorded. Point-in-time evaluation falls out of the settled storage shapes — interval-stamped fact rows and the supersession-closed theory changelog — so replay costs a retention window rather than an API. It serves two callers: an author building tests for configuration mutations, and an investigator answering "why was this allowed" by re-running the decision rather than reading its verdict.

## Why the record exists

Six purposes, from enterprise security practice (Mark, 2026-08-08):

- **Forensics.** Reconstruct the sequence of actions that produced an outcome, after the point where anyone can still be asked.
- **Attribution and non-repudiation.** Shared accounts and assumed roles make "it wasn't me" cheap. An append-only record makes an action stick to an identity.
- **Detection.** Feed the stream to a SIEM and alert on patterns — credential use from a new geography, mass access to a sensitive prefix, the trail itself being disabled.
- **Compliance.** The auditor's question is not "are you secure" but "show me the evidence." The record is the evidence.
- **Deterrence.** People behave differently when their actions are attributable.
- **Operational forensics.** Answering "why did this change" when the cause is a deploy pipeline, a migration, or a teammate in a console rather than an attack.

Four design constraints follow. The record is tamper-evident, and the audited party cannot edit it. It is stored outside the blast radius of the systems it covers. It is retained past the detection gap, since a breach can go months undiscovered. Its timestamps are synchronized, so events from different systems correlate into one timeline.

None of the four is satisfied yet. `Decision` holds a wall timestamp distinct from its Kookie, which is the hook correlation needs — an investigator finds decisions in an incident window by wall time, then replays each at its own pin — but a timestamp is not a clock protocol, and the protocol is unruled ([[storage-versioning-design]]). Retention is unaddressed entirely: nothing states the window or where expiry authority sits.

Only Kingo can re-derive a decision, because replaying a verdict needs the graph and the catalog at that Kookie. That establishes Kingo must expose replay, which it does. It does not establish that Kingo stores the record: a Decision fetched from any sink replays the same. Whether Kingo owns the store is open ([[the-record-lives-outside-kingos-blast-radius]]).

Two of the four constraints shape how the record is stored. Tamper-evidence means append-only, with expiry authority held outside the services being audited. Blast radius means a store the audited service cannot reach, which a store separate from the fact store meets only if it is also separately deployed ([[the-record-lives-outside-kingos-blast-radius]]).

The auditor's integrity condition adds a requirement no other actor's does: verifying the trail was never disabled. Kingo's own audit configuration — retention, sink target, emission on or off — becomes an input event in its own right. A gap in the record must be detectable rather than silent.

## Event classes

Two, split by direction (Mark, 2026-08-08): input events are writes, output events are checks.

- **Input events** — theory changes and fact writes. Answers "why did Jim lose access" and "why was the attacker able to breach." Author and investigator audience, low volume. The changelog already is this record, so it exists whether or not observability consumes it.
- **Output events** — Check verdicts. Answers "who was accessing system X when event Y occurred." System operator and security operator audience, feeding alerting sinks. High volume, scaling with the hot path, so this store can grow larger than the fact store.

Each class is read two ways — query the retained log, or listen to it live from a cursor — giving four independently deployable services: Input-Audit, Input-Watch, Output-Audit, Output-Watch (Mark, 2026-08-08). Input-watch is Watch as it stands today.

The load asymmetry between the classes forces the split. Output events are produced once per Check and input events once per write, so the two differ by orders of magnitude at every point: emission rate, store size, and query cost. Co-hosting them provisions the low-load pair for the high-load pair's peak. That argument splits the classes and says nothing about splitting audit from watch inside one. Connection lifecycle does: [[grouping-the-apis-into-services]] separates Watch because streaming holds long-lived cursor-bearing connections, where a query over the retained log does not.

Both watch services are emission rather than reads back out. [[authz-event-logging]] has Check buffering a Decision for a background shipper, and has Watch tailing the changelog as management-event emission. The shipper pushes to a configured sink and Watch is pulled from a caller-held cursor, so these are two transports over one emission port, not one path. Whether Kingo offers push, pull, or both is open ([[watch-is-emission-not-a-read]]). That note also splits the classes as management and data events, which needs reconciling with the input/output split.

## Services

Eight, provisional on record ownership. Check, Write, Read, and Expand are one service each; Watch and Audit are two each, split by event class. If Kingo does not own the record, Input-Audit and Output-Audit query someone else's sink and are not Kingo services at all, which drops the count to six ([[the-record-lives-outside-kingos-blast-radius]]).

Load profile is the deployment boundary, except where an invariant or a connection lifecycle forces one.

- **Check** — reaches the fact store, the catalog, and the rewrite engine. The hot path, and the only place caching and hedging land.
- **Write** — reaches both stores and appends the changelog. Low load.
- **Read** — reaches the fact store and the catalog. I/O-bound.
- **Expand** — reaches the fact store, the catalog, and the rewrite engine. CPU-bound on tree materialization.
- **Input-Audit** — reaches the retained input record. Low load, query-shaped.
- **Input-Watch** — reaches the changelog. Low load, long-lived cursor-bearing streams.
- **Output-Audit** — reaches the retained decision record. High load, scaling with Check, and the largest store.
- **Output-Watch** — reaches the decision stream. High load, scaling with Check, long-lived cursor-bearing streams.

Write is one service by invariant rather than by profile: the drift guard needs a sole writer of theories and facts, so splitting them would put the invariant across a service boundary.

Audit and Watch split within a class by connection lifecycle rather than load, since a stream holding a caller's cursor and a query over the retained record share a profile.

Read and Expand do not co-host. Their dependency sets differ, so a co-hosted Read would redeploy on every rewrite-engine change. Their resource profiles differ too. This supersedes [[grouping-the-apis-into-services]], which co-hosts them across four hosts.
