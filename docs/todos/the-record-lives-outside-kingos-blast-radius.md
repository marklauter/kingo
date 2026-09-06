---
title: The record lives outside Kingo's blast radius
type: todo
summary: "Decide whether Kingo stores the authorization record or only produces it. The blast-radius constraint says the record sits outside the audited system, and re-derivation does not argue for storing it. The answer decides whether Input-Audit and Output-Audit are Kingo services at all."
tags: [audit, architecture, services]
status: open
priority: high
effort: medium
cites:
  - "[[the-operation-set]]"
  - "[[authz-event-logging]]"
  - "[[actor-catalog]]"
---

# The record lives outside Kingo's blast radius

[[the-operation-set]] names four design constraints on the record. The second says it is stored outside the blast radius of the systems it covers. The note then has Kingo own the store, and calls that store "separate from the fact store," which does not meet the constraint if the two share a deployment, an account, or an operator. CloudTrail ships to a different account for this reason.

The reproducibility argument does not close the gap. Only Kingo can re-derive a decision, because replaying a verdict needs the graph and the catalog at that Kookie. That establishes Kingo must expose replay. It does not establish that Kingo holds the record: a `Decision` fetched from any sink replays the same.

The auditor's integrity condition ([[actor-catalog]]) cuts the same way. A record the audited party can silently disable is not tamper-evident, and Kingo is the audited party.

## What the answer decides

- **Audit's owner.** If Kingo does not store the record, Input-Audit and Output-Audit are queries against someone else's sink and are not Kingo services. The service count in [[the-operation-set]] drops from eight to six, and "Audit returns retained events" needs a different owner.
- **Where expiry authority sits.** Tamper-evidence requires it outside the services being audited, which is a constraint on the store's deployment rather than on its schema.
- **What Kingo must keep regardless.** Replay needs the graph and the catalog readable at any Kookie inside the retention window, whoever holds the events ([[storage-versioning-design]]).

Done when the note states whether Kingo stores the record, produces it for an external sink, or both, with the blast-radius constraint answered rather than restated.
