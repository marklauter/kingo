---
title: Watch emits events
type: todo
summary: "Decide whether Kingo delivers events by push to a configured sink, pull from a caller-held cursor, or both, then redraw the emission line so Input-Watch and Output-Watch sit on the side they fall on."
tags: [audit, architecture, services]
status: open
priority: medium
effort: low
cites:
  - "[[the-operation-set]]"
  - "[[authz-event-logging]]"
---

# Watch emits events

[[the-operation-set]] draws a line: [[authz-event-logging]] covers emission into a sink and stops, and the four observability services read the record back out. Two of the four sit on the wrong side of it.

[[authz-event-logging]] has Check buffering a `Decision` for a background shipper that delivers batches, which is what Output-Watch carries. It also has Watch tailing the changelog, and calls that management-event emission, which is what Input-Watch carries. So the claim is false for both watch services, not one.

The transports differ. The shipper pushes to a sink named in Kingo's own configuration, and Watch is pulled from a cursor the caller holds. Two transports over one emission port, so this is a delivery choice rather than two designs.

Pull has a property push does not: a caller-held cursor makes a gap in the stream detectable by the caller, which the auditor's integrity condition needs. Push has a property pull does not: the sink can sit outside Kingo's blast radius, which [[the-record-lives-outside-kingos-blast-radius]] turns on.

Done when the operation set states which transports Kingo offers, and the emission line is redrawn where it actually falls.
