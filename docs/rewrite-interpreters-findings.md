---
title: Rewrite interpreters — clean-room critique findings
summary: "Findings from a dry-run of the [[rewrite-interpreters]] clean-room handoff: gaps that block the design, semantics left ambiguous, and contradictions between the note and the other clean-room inputs (domain code, linked corpus docs)."
tags: [note, acl, interpreters, review]
created: 2026-07-18
status: evolving
critiques: "[[rewrite-interpreters]]"
---

# Rewrite interpreters — clean-room critique findings

A dry-run of the clean-room session: the note was read alongside its declared inputs (the Zanzibar paper, the current domain code on `reboot`) and the corpus docs it wikilinks. Each finding names where the clean-room designer would be blocked, forced to guess, or handed contradicting inputs. Severity: **blocking** (cannot proceed without an answer), **ambiguous** (can proceed, but two readings produce different systems), **inconsistency** (inputs disagree; the note should say which wins), **minor**.

## Blocking

### F1 — `Decision`'s declared home creates a dependency cycle — placement ruled, project name open

Requirement 1 said the result is "a `Decision` (`Kingo` kernel, stub exists)" while Settled says `Decision` carries the `Fact` judged. `Fact` lives in `Kingo.Graphs`, and `Kingo.Graphs` references `Kingo` (its identifier types), so a kernel `Decision` carrying a `Fact` means `Kingo` → `Kingo.Graphs` → `Kingo`, a project cycle. Ruled (Mark, 2026-07-18): `Decision` and `Expansion` live in the interpreter project — the one that references both aggregates; the `Decision` stub has moved to `src/Kingo.Acl/Decision.cs`. The project's name is also ruled (Mark, 2026-07-18): `Kingo.Acl` becomes **`Kingo.Closures`** (plural, per the namespace naming convention) — "Acl" names the data structure ReBAC generalized away, and the settled rulings say the interpreter answers membership in the derived closure, not authorization ("authorized" is the Check host's reading). "Closure" is the term in both fields the domain draws on: the set closed under the rewrite rules' derivation, graph theory's transitive closure. Reachability was considered and rejected: `!` and `&` make the derived set non-monotone, so the verdict is a function over several reachability questions, not a reachability property itself. Residue executed (2026-07-18): the rename is complete in code and docs (the handoff note's one remaining `src/Kingo.Acl/` mention is the archive-branch path, correct as written); requirement 1's parenthetical now reads `Kingo.Closures`; the `Expansion` stub exists at `src/Kingo.Closures/Expansion.cs`; the moved stub's doc comment matches the settled shape (F12); and `docs/glossary/closure.md` locks the derived-set reading. Nothing remains on this finding.

### F2 — "schema version" has no definition anywhere in the inputs

`Decision` and `Expansion` both carry "the schema version," but no input defines one. `Schema`'s identity is `SchemaIdentifier` (name-as-identity, explicitly provisional; `src/Kingo.Schemas/Schema.cs:15`) and the value has no version field. The note never says what fills the slot (the name? a content hash? a store-assigned revision?) or where the evaluator gets it, since it isn't on the injected `Schema` value. Ruled (Mark, 2026-07-18): the slot is an opaque `SchemaVersion` kernel value, minted now as a stub — the same move as `Kookie` — with its encoding left to [[storage-versioning-design]]. It arrives as evaluator constructor context beside the injected `Schema`.

### F3 — depth accounting is undefined, and the schema-cycle safety claim depends on it

The depth bound never defines its unit. Condition 3 says the recursion "is not bound by the schema … it is bound by the *facts*," which reads as: only fact-driven re-entries (tupleset hops, subjectset-valued members) consume depth. But Deliberately absent claims a schema cycle that escaped validation "manifests as (3)" — which is only true if schema-rule traversal (`ComputedSubjectSetRewrite` crossings) also consumes depth. If it doesn't, mutual computed subjectsets loop without ever touching the bound: no I/O, no fact hop, no depth increment — a hang, not a modeled error, on the path the note calls a security property. Either every recursive evaluation counts (say so, and the "cycle in data vs bound too low" audit story still works), or schema cycles need their own guard inside the evaluator. Ruled (Mark, 2026-07-18): neither — schema cycles are made unrepresentable at `Namespace.Create` ([[namespace-create-validation]]), the evaluator carries no schema-cycle guard, and depth counts only fact-driven re-entries (tupleset hops, subjectset-valued member expansions). "Manifests as (3)" is withdrawn; condition 3 now states the unit and Deliberately absent states the construction guard.

### F4 — empty operator nodes are legal values with undefined semantics

`UnionRewrite` and `IntersectionRewrite` take `ImmutableArray<SubjectSetRewrite>` and accept empty children (`src/Kingo.Schemas/SubjectSetRewrites.cs:42`); neither `Schema.Create` nor `Namespace.Create` rejects them, and the algebra is parse-agnostic — the SDL grammar can't produce an empty node, but the Write API path can. The conventional semantics of an empty intersection is the universal set: everyone is a member. The note was silent. Ruled (Mark, 2026-07-19): rejected at construction — the shape joins the [[namespace-create-validation]] pass, the evaluator never meets one, and the note's Settled section now says so.

### F5 — the upstream guards the note leans on don't exist, and their scope is unassigned

Two rulings delegate safety to guards that aren't in the tree and aren't in any listed work item:

- "Schema cycles … rejected at SDL parse; the evaluator never meets one." No cycle validation exists in `Kingo.Sdl`, and `Schema.Create` doesn't validate that `ComputedSubjectSetRewrite`/`TupleToSubjectSetRewrite` targets are defined relationships at all — a `Schema` value with dangling intra-schema references or cycles constructs successfully today.
- The Write-path refusal of wrong-shaped tupleset members (the guard that makes conditions 5–6 "a never-in-practice backstop") is future work in a service that doesn't exist.

That may be deliberate sequencing, but the clean-room can't tell whether building either guard is in scope for this work item, and (with F3) can't rely on "the evaluator never meets one." Also unanswered: is a *schema-internal* dangling reference (bad `Schema` value, no drift involved) condition 4, or a distinct condition? Condition 4 is framed entirely as fact/schema drift. Ruled (Mark, 2026-07-18): the schema-side guard is assigned — `Namespace.Create` rejects cycles and dangling intra-namespace references at construction ([[namespace-create-validation]]), so a schema-internal dangling reference is unrepresentable, not a condition; the tupleset second component stays eval-time condition 4. The Write-path refusal of wrong-shaped tupleset members remains the Write service's work item; the note now says conditions 5–6 are the only protection until it exists.

### F6 — `Kookie` exists only as a name

The note calls `Kookie` "an opaque kernel value," but no type existed and its encoding is explicitly undesigned ([[storage-versioning-design]] is an open todo). Ruled (Mark, 2026-07-19): mint the opaque stub now — `src/Kingo/Kookie.cs` exists — with the encoding left to the storage design, and the port's snapshot token is the `Kookie` type itself, copied into the result without interpretation; no distinct port token, no mapping layer. `SchemaVersion` (F2) got the same treatment in the same pass (`src/Kingo/SchemaVersion.cs`).

## Ambiguous semantics

### F7 — subjectset-valued questions: literal facts only, or the derived closure?

Two rulings pull against each other when the question's subject is itself a `SubjectSet`. "`Contains` answers: does the graph's *derived closure* contain this fact" says yes to derivation; "a subjectset-valued member means *literal element membership* … value equality at the leaves" can be read as stored-facts-only. The discriminating fixture: facts `doc:x#viewer@team:sales#member` and `team:sales#member@group:eng#member` — is `Contains(doc:x#viewer@group:eng#member)` true? Requirement 2's userset-expansion clause makes every member of `team:sales#member` a member of `doc:x#viewer`, and `group:eng#member` is literally an element of `team:sales#member` — so the closure reading says true. The value-equality-at-the-leaves reading says false (no stored fact matches). Pin it with exactly this test.

### F8 — undefined *namespace* mid-walk has no condition

Condition 4 covers a relationship removed by drift; a namespace can be removed the same way. Fact `doc:x#viewer@oldns:g#member` stored under schema v1; v2 drops namespace `oldns` entirely. The walk needs `oldns:g#member`'s rewrite and finds no namespace. Condition 1 is defined as a *caller* defect on the *question*, detectable pre-I/O — this is neither. Is it condition 4 broadened ("undefined namespace or relationship mid-walk"), or a ninth condition? Same question for tupleset-resolved subjects whose namespace is undefined.

### F9 — is the subject side of the putative Fact schema-validated at all?

Family 1 validates the question's resource namespace and relationship — the `SubjectSet` side of the `Fact`. Nothing says whether the *subject* side is checked when it's a `SubjectSet` (`Contains(doc:x#viewer@grup:eng#member)` — typo'd namespace on the subject). Under literal-element semantics the evaluation never needs the subject side's schema, so the check would come back false (no such element) rather than a modeled error. A subject-side typo would then silently behave differently from a resource-side typo — the outage-that-looks-like-policy family 1 exists to prevent. Deliberate or an oversight; state which.

### F10 — `ComputedSubjectSetRewrite` under Expand: leaf or inline?

Whether Expand emits `doc:x#editor` as an unexpanded leaf or inlines `editor`'s rewrite subtree is derivable only by inference from the condition-4 ruling ("Expand never crosses" into another relationship's definition ⇒ leaf). But the note's own §2.4.5 justification — Expand "follows indirect references expressed through userset rewrite rules," glossed as "the schema's rewrite rules, finite and known at parse" — reads as if schema-side references *are* followed, and a same-resource computed subjectset is exactly a schema-side reference. A clean-room could cite the same sentence for either design. One explicit sentence: a `ComputedSubjectSetRewrite` node materializes as the leaf `resource#relationship`.

### F11 — the port's execution shape is undecided but the semantics assume one

The Kleene ruling legitimizes "cancelling its in-flight parallel evaluation," which presupposes asynchronous, possibly concurrent port calls; Deliberately absent says timeout/cancellation is "host-edge concern." Between them the note never says: is the port async (`Task`/`ValueTask`-shaped)? Does `CancellationToken` flow through the domain signatures (`Contains`, `Expand`, the port), or is cancellation invisible below the edge? Is parallel operand evaluation in scope for this work or a later optimization the semantics merely permit? These change every signature in the deliverable.

## Inconsistencies across the input set

### F12 — the `Decision` stub contradicts the settled shape

`src/Kingo.Acl/Decision.cs:5` said the value is "expected to identify … the caller." The note settles caller identity as envelope-only (2026-07-17). The clean-room's instructions say to read the current domain code; the code disagreed with the note it's read beside. Resolved (2026-07-18): the stub — now `src/Kingo.Closures/Decision.cs` — states the settled shape (Fact judged, verdict, snapshot pin, schema version, wall timestamp; caller identity envelope-only).

### F13 — [[authz-event-logging]] puts caller identity and correlation id inside the serialized `Decision`

Requirement 1 cites the note; its payload list ("subject, subjectset, verdict, evaluation timestamp, the snapshot/zookie evaluated at, schema version, caller identity …, correlation id") was the pre-2026-07-17 shape. Resolved (2026-07-18): [[authz-event-logging]] now states the settled shape — envelope + serialized Decision, caller identity and correlation id envelope-only, failures audited as envelope + error. ([[caller-identity]] already recorded the ruling correctly.)

### F14 — the clean-room input set is undefined at its edges

The constraint names three inputs — this note, the paper, the current domain code — but the note wikilinks eight-plus corpus docs, and requirement 7 depends on "the SDL example's `banned` exclusion," which lives in [[schema-definition-language]] and is neither linked from the note nor reproduced in it. Are wikilinked docs (and their transitive links) inputs? If yes, F12/F13 contradictions land in-context and open todos leak design pressure; if no, requirement 7's fixture and the `Kookie` semantics ([[storage-versioning-design]]) dangle. State the closure rule, and either link or inline the SDL example.

### F15 — requirement 5's zookie wording contradicts the settled signature

Settled: "the interpreter never sees a zookie argument." Requirement 5 said: "the zookie enters the signature as an opaque token" — without naming whose signature, a literal reading puts the token back into the interpreter's. Resolved (2026-07-18): requirement 5 now names the host API's signature, consumed at the edge to pin the port.

## Minor

- **F16 — timestamp moment.** Whether the `TimeProvider` stamp is taken at evaluation start or completion is unstated; the audit field's meaning should be one sentence.
- **F17 — requirement 3 vs Deliberately absent.** "Cycles can exist in the schema" sat against "the evaluator never meets one." Resolved (2026-07-18) through F3/F5: requirement 3 and Deliberately absent now agree — schema cycles are unrepresentable at construction.
- **F18 — `...` as the question's relationship.** `Contains(folder:y#...@user:anne)` is grammatically constructible (`RelationshipIdentifier.Nothing` parses); no schema defines `...`, so it should fall out as condition 2 — worth a named test case rather than an accident.
- **F19 — "record what Expand adds" (requirement 6) names no destination.** Presumably a note under the dynamodblite decision's neighbourhood; say where.
- **F20 — port operation surface.** Requirement 6 predicts point lookup and range read; requirement 2 (userset expansion) and tupleset traversal both read *all* facts under a subjectset, and `Contains`'s direct-fact case wants existence. That's consistent with the prediction, but whether the port exposes two operations or one range-read the evaluator filters is design work the note could seed with a sentence on the reverse-index question (nothing here needs "which sets contain subject X"; confirm that's deliberate).
