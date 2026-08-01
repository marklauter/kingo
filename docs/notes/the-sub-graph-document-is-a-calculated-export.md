---
title: The sub-graph document is a calculated export
type: note
summary: "A third fact-side artifact: a document holding a selected portion of the graph as state, calculated by asking a subjectset who it contains. It denotes a subgraph, which is what GraphPrinter never did."
tags: [graphs, documents]
created: 2026-07-31
status: evolving
cites:
  - "[[graph]]"
  - "[[subjectset]]"
  - "[[expand]]"
  - "[[kookie]]"
  - "[[graph-document-is-bulk-dml]]"
---

# The sub-graph document is a calculated export

The whole graph is never a document. A production catalog carries millions of subjects, resources, and subjectsets, so the exportable artifact is a portion of it — a sub-graph. It is selected by calculation: name a subjectset, ask who it contains, and take the facts that answer.

It is a state, not a changeset. Every entry is a fact that is present, so the document denotes an actual subgraph of the graph. That is the property `GraphPrinter` lacked when it was deleted (2026-07-15, recorded in [[graph-document-is-bulk-dml]]): it was the inverse of a parser that read a mutation document, and there is no `parse ∘ print = id` law between a state and a changeset. The deletion argument was about printing a changeset back out. It does not reach an artifact that was a state to begin with, so the sub-graph document reopens the printer question on different ground.

What makes it a different artifact from a stored-fact dump is that the selection is derived. Reading the facts stored under a subjectset is a storage operation; asking which subjects that subjectset contains runs the rewrites, so the answer includes memberships no stored fact records. The export is therefore pinned like any other evaluation — the kookie names the fact state it was calculated at, and the same selection at a different pin is a different document.

## To settle when this becomes a spec

- What the selection is stated as. One subjectset, a list, a filter over resources or relations.
- Whether the result is the derived closure or the stored facts that support it. The first answers "who can read this", the second round-trips into a mutation document and moves data between installations. These are different products and may be different artifacts.
- Whether it round-trips at all, which is the printer question in its new form.
- Whether the sub-graph document and the mutation document share a grammar. They share the fact text form the core owns; the envelopes need not match.
