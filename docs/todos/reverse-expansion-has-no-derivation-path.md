---
title: Reverse expansion has no derivation path
type: todo
summary: "Expand is carried as one operation with two directions, but only the forward direction derives from anything. Settle whether reverse expansion is Kingo's at all, and if it is, what produces it and where it deploys."
tags: [architecture, services, closures]
status: open
priority: medium
effort: medium
blocked-by: "[[precomputed-closure-index-for-the-hot-path]]"
cites:
  - "[[rewrite-interpreters]]"
  - "[[actor-catalog]]"
---

# Reverse expansion has no derivation path

[[the-operation-set]] lists Expand as one operation with two directions and marks the pairing provisional. Forward materializes a subjectset's rewrite tree. Reverse answers "which resources can this subject reach," the question behind every filtered list view.

The rewrite interpreters do not supply reverse and are not meant to: Contains is a membership predicate, and Expand materializes one relation's rewrite tree downward. Requirement 6 of [[rewrite-interpreters]] — the evaluator needs no reverse lookup — is unaffected. Finding every subjectset whose closure contains an identity is [[precomputed-closure-index-for-the-hot-path]]'s problem, which is why this cannot be settled before that index exists.

Two questions, in order.

1. Whose is it? The note calls reverse an administration capability, but a filtered list view is product traffic. Under [[actor-catalog]] the consumer is the accessor's interest — reaching the resources they are entitled to — which reaches Kingo only through the relying party, never directly. An administration capability and a product capability have different load profiles, and load profile is the deployment boundary.
2. If it is Kingo's, is it Expand? Pairing it with forward Expand assumes the administrative profile. If the consumer is product traffic, the pairing fails the same rule that split Read from Expand.

Done when the note carries a direction for reverse — its own operation, forward Expand's second direction, or outside Kingo — with the load profile that decides it named.
