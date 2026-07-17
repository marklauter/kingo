# Prompt: finish the rewrite-interpreters brief — Expand's open questions

Continue the domain-modeling interview for `docs/todos/rewrite-interpreters.md` (load `/hoplite-skills:domain-modeling`; one question at a time, recommendation with each; write each ruling into the note's Settled section as it lands). The clean-room constraint stands: do not read `src/Kingo.Acl/` on `main-archive` / `dictionary-encoding`.

Context: the brief is Contains-heavy. Expand's boundary is ruled (single-level wrt indirection, paper §2.4.5 verbatim, locked 2026-07-17), but three questions the interview answered for Contains were never asked for Expand. All three are answerable at the requirements level.

## 1. What is Expand's question?

Contains takes a putative `Fact`; Expand has no ruled signature.

**Recommendation:** `Expand(SubjectSet subjectSet) → Result<…>` — the resource#relationship pair names exactly one relationship's rewrite tree, which is what single-level materializes. No `Subject` in the question (Expand asks "who's in this set," not "is this one in it"), so the putative-Fact move doesn't transfer; `SubjectSet` is already the domain word for the thing expanded. Context (Schema, pinned port, TimeProvider, whatever bounds apply) stays constructor state, same as Contains.

## 2. Which of the eight error conditions apply to Expand?

The taxonomy (three families, eight conditions) was derived for Contains. Sharp sub-questions:

- Single-level means referenced subjectsets stay leaves — does Expand ever recurse at all? If not, condition 3 (depth exceeded) may not apply to it.
- Do tupleset conditions 5–6 apply? That depends on whether a tupleset arm resolves its facts during Expand or stays a leaf (the paper's Expand leaves are "user IDs or usersets referenced by indirect ACLs or object hierarchies").
- The Kleene rule (surface iff decisive) is about verdicts; Expand has no verdict. Presumably any error it meets is decisive — no absorption. Confirm.

## 3. What audit contract does the sibling result type carry?

Requirement 1 says "Expand needs a sibling result type carrying the tree" and stops. Does it carry the Kookie pin, schema version, and wall timestamp like `Decision` does? The replay-sufficiency criterion (Settled, 2026-07-17: a field earns its place iff replay requires it or it records the judgment's provenance) presumably transfers — confirm and name the type's fields at the same altitude as Decision's list.

## After ruling

Sweep Settled and Requirements for Contains-only phrasing that the Expand rulings now qualify, then delete this file — it's a hand-off, not a corpus note.
