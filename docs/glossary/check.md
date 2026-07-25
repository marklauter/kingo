---
title: check
type: definition
summary: "The authorization query a caller makes at the host edge. Does a subject set contain a subject?"
tags: [glossary, evaluation]
created: 2026-07-18
status: evolving
contrast:
  - "[[contains]]"
---

The authorization query a caller makes at the host edge. Does a subject set contain a subject? A check carries the caller's context: the snapshot floor, the caller identity, and the fail-closed policy applied to the answer.

## Contrasts

- `contains` — a check is the request, with everything the host attaches to it. Contains is the evaluation that settles the question.
