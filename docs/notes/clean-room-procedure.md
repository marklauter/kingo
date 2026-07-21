---
title: Clean-room procedure
summary: "How a work item reaches a clean-room design session: a self-sufficient handoff note, an input closure by settled kind, a dry-run critique, and a findings ledger driven empty before the session runs."
tags: [note, process, design]
created: 2026-07-20
status: evolving
---

# Clean-room procedure

Distilled from the first run: [[rewrite-interpreters]] and its dry-run, [[rewrite-interpreters-findings]] (2026-07-15 through 2026-07-20).

## What the room is clean of

Prior implementations of the same feature — the designer's own earlier attempts above all. The first run's exclusion was `src/Kingo.Acl/` on the archive branches (`main-archive`, `dictionary-encoding`), Kingo's incomplete first ACL implementation. Its lessons arrive encoded as requirements in the handoff, never by reading the code: an unfinished design read directly exerts pressure the requirements were written to filter, and the room exists so the designer inherits the lessons without inheriting the design. External sources are the lesser exclusion — inspiration, not inputs (see the closure).

## The handoff note

One note carries the work item: requirements and settled rulings, never design. Rulings are dated and attributed; a superseded ruling names what replaced it. The note is self-sufficient — every ruling's substance *and its reasoning* lives in the note. Reasoning stranded in another doc is invisible to the designer, and a constraint without its why invites being improved away.

## The input closure

Inputs are the handoff note and the current domain code, plus — one hop from the note — the wikilinked docs of settled kind: glossary terms, decision records, spec-tagged notes. Kind, not status flag: most of the glossary sits at `evolving`, and a locked-only rule would drop the vocabulary the note is written in. Open todos are out of bounds; a wikilink to one is a jurisdiction marker — the question is assigned there — never a reading assignment. External sources are inspiration, not inputs: inline what the design needs where it is used, cite sections as provenance. No transitive expansion. Where an input disagrees with the note, the note wins; the designer reports the conflict as a finding.

## The dry-run

Before the session runs, simulate it: read the note against its declared inputs and record every place the designer fails. Severity names the failure mode — **blocking** (cannot proceed without an answer), **ambiguous** (two readings produce different systems), **inconsistency** (inputs disagree; the note must say which wins), **minor**. Findings live in their own critique note, which sits outside the closure like any other unsettled doc.

## The ledger

Every finding is driven to a terminal state: **ruled** (a decision, dated and attributed), **resolved** (the inputs repaired), **dissolved** (the question made unrepresentable), or **assigned** (explicitly delegated to the session — the note says the choice is the designer's). Each records its residue: where the fix landed in the handoff or the code. When two readings fight, first ask whether the question deserves to exist — twice the best answer was narrowing a type so the question could not be asked, and a second finding closed for free (F7, F9).

The session runs only when the ledger is empty and an independent pass confirms the handoff holds everything load-bearing. The findings note then locks as history.
