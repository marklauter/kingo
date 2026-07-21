---
title: First clean-room dry-run — twenty findings, ledger driven empty
summary: "The [[rewrite-interpreters]] handoff was dry-run against its declared inputs before the design session; the critique produced twenty findings over three days (2026-07-18 to 2026-07-20), every one driven to a terminal state, and the method survived as [[clean-room-procedure]]."
tags: [journal, interpreters, process]
created: 2026-07-20
---

# First clean-room dry-run — twenty findings, ledger driven empty

Context going in: [[rewrite-interpreters]] had been the selected work item since 2026-07-15, written requirements-only with the design reserved for a clean-room session — a fresh context reading the note, the Zanzibar paper, and the domain code. Before spending that session, we simulated it: read the note exactly as the designer would, against exactly the declared inputs, and record every stumble. Expectation: a handful of wording fixes. Result: twenty findings ([[rewrite-interpreters-findings]]), six of them blocking.

The blockers were structural. `Decision`'s declared home created a project cycle (F1 — resolved by minting `Kingo.Closures`). "Schema version" and `Kookie` were named nowhere in the type system (F2, F6 — opaque stubs minted). Depth accounting was undefined while a safety claim leaned on it (F3 — depth counts fact-driven re-entries only; schema cycles became unrepresentable at `Namespace.Create` instead). Empty operator nodes had the universal-set reading waiting to become an authorization hole (F4 — refused at construction). Guards the note leaned on didn't exist anywhere (F5 — assigned owners).

Two findings dissolved rather than resolved: F7's "does subjectset A contain subjectset B" fight ended by narrowing `Contains` to `(SubjectSet, Subject)` so the question can't be asked, and F9 (is the subject side schema-validated?) closed for free the same moment. The lesson we keep: when two readings fight, first ask whether the question deserves to exist.

The week's biggest ruling came out of F8: drift prevention moved to the write edges — facts can't lead the schema, the schema can't abandon facts ([[drift-prevention-at-the-write-edges]]) — demoting condition 4 to a never-in-practice backstop. F11 fixed the execution shape (async `ValueTask` port, token as plumbing, parallelism out of scope). F14 defined the input closure and then, in a same-day amendment, dropped the paper from the input set entirely — the note cited `docs/5068.pdf`, no PDF existed in the repo, and the fix was stance, not a file: Zanzibar is inspiration, Kingo is its own system, and what the design needs is inlined where it's used.

Two independent review passes closed the loop. The first caught F20 half-open (the port's operation count — explicitly assigned to the design session rather than ruled). The second checked the handoff's independence from the findings doc and caught the last two leaks: the findings doc excluded from the closure only by silence, and F9's dissolution living nowhere but the findings file. Both fixed the same day. A wrong turn worth recording: rationale kept drifting into the findings entries instead of the handoff bullets, and it took a deliberate back-fill pass (commit 7ed7075) to restore the invariant that the handoff carries its own reasoning — the findings doc is history, and history is outside the closure.

Learned: the dry-run pays for itself — six blockers would otherwise have surfaced mid-session, each one a context-burning round trip. The method survived the week as [[clean-room-procedure]]; the ledger is empty, and the session itself is next.
