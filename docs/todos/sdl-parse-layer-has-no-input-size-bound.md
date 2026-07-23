---
title: SDL parse layer has no input-size bound
summary: "Removing the rewrite-expression token budget left depth exactly bounded but total size unbounded: tokenization, AST allocation, the height walk, and Transform are linear in input length, and error messages echo the full expression. Decide where the size bound lives before SDL accepts untrusted input at a service edge."
tags: [note, todo, sdl]
created: 2026-07-21
status: open
priority: medium
effort: low
---

# SDL parse layer has no input-size bound

Raised by the adversarial review of the depth-guard redesign, 2026-07-21. The old 200-token budget in `RewriteExpressionParser` doubled as the layer's only total-size bound; the exact guards that replaced it (paren scan, tree-height gate) bound depth and nothing else. A multi-megabyte flat expression now costs proportional CPU and memory across tokenization, AST allocation, the height walk, and `Transform`'s `Sequence` over a wide union — linear, not explosive, but unbounded. The validation error messages also interpolate the full expression, so the echo into error values (and any log that carries them) is unbounded too.

The exposure needs an untrusted caller reaching SDL parse, which today means nothing — no service edge exists. It becomes real when the Write host (or any endpoint) accepts schema documents.

The candidate homes for the bound:

- The host edge's request body-size limit — no parser change; every SDL consumer must remember to configure it.
- A character-length cap on the expression in `RewriteExpressionParser` — an O(1) check, generous (tens of KB) so it bounds size without becoming a depth proxy again; plus truncating the expression echo in error messages.
- Both — the parser cap as defense in depth behind the edge limit.

Decide with the first service edge; the parser cap is a one-line change whenever ruled.
