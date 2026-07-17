---
type: todo
title: Caller identity
summary: "Open question: what 'caller identity' means at the Check host edge — network context, principal claims, and on-behalf-of chains — and the three distinct authorization decisions an OBO call implies, including whether the caller may call Kingo at all."
tags: [note, todo, hosts, identity, audit]
created: 2026-07-17
status: open
priority: medium
effort: medium
---

# Caller identity

## Observation

The interpreter ruling in [[rewrite-interpreters]] (2026-07-17) put caller identity in the host's request envelope, not on the `Decision`. That ruling settles *where* identity lives; it leaves open *what* identity is. "Identity of caller" covers at least:

- **Network context** — IP, user agent, and the rest of the transport-level envelope; audit material ([[authz-event-logging]]).
- **The principal** — literally the authenticated subject: claims, token, the authn-side identity that maps to a `SubjectIdentifier`.
- **The on-behalf-of chain** — most real-world calls are service-to-service OBO: service A calls service B passing a user.

## Interpretation

An OBO call implies three distinct authorization decisions, not one:

1. **Does service A have OBO rights to the resource/action?** This is just another ACL check — the OBO grant is expressible as relationships in the graph, no new machinery.
2. **Does the user service A is passing have rights to the resource/action?** The ordinary check.
3. **Does the caller have rights to call Kingo's ACL at all?** Kingo authorizing access to itself — a check about the API surface, upstream of the question being asked.

Decisions 1 and 2 are two `Contains` questions in one request; decision 3 is edge policy that may itself be a `Contains` question against a Kingo-owned namespace, or something cheaper. All three sit in the hosts, outside the interpreters — the interpreter never sees who asked.

## Next

- Decide the envelope shape: which identity facets (network context, principal, OBO chain) the Check host records per request, feeding the audit event (envelope + serialized `Decision`, per [[rewrite-interpreters]]).
- Decide whether the OBO pattern is one API call carrying both questions or two calls, and whether Kingo models the OBO grant as ordinary relationships (Mark's read, 2026-07-17: yes, decision 1 is just another check).
- Decide how decision 3 is enforced — Kingo checking against its own namespace vs. host-level authn policy — and whether that self-check bootstraps cleanly.

## Related

- [[rewrite-interpreters]] — the envelope-not-Decision ruling this question grows out of
- [[authz-event-logging]] — the audit event the envelope feeds
- [[four-service-split-by-load-profile]] — the hosts where all of this lives
