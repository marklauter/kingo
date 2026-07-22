---
title: fact-to-subject-set
summary: "A rewrite that walks the facts under a factset relationship, then evaluates a computed subject set on each resolved resource."
tags: [glossary, rewrite]
aliases: [tuple-to-subject-set]
created: 2026-07-18
status: locked
retired: [tuple-to-subject-set]
is-a: "[[subjectset-rewrite]]"
---

A rewrite that walks the facts under a factset relationship, then evaluates a computed subject set on each resolved resource — the mechanism for inherited permissions. Renamed from *tuple-to-subject-set* (2026-07-21): "tuple" is the paper's word for the thing Kingo names [[fact]]. The same day's drift sweep corrected the walk's resolved members from *subjects* to *resources* per [[resource-fact-case]] — a semantic fix distinct from the rename.
