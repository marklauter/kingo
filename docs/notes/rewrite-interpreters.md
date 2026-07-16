---
type: todo
title: Rewrite interpreters — Contains and Expand
summary: "One algebra, two interpreters: Contains (Check's short-circuiting membership predicate) and Expand (full tree materialization) over SubjectSetRewrite — in Kingo.Acl, fact lookup as the first genuine port, Decision as the result. Requirements only; design clean-room."
tags: [note, todo, acl, interpreters]
created: 2026-07-15
status: open
priority: high
effort: high
---

# Rewrite interpreters — Contains and Expand

## Clean-room constraint

Design this in a fresh context from this note, the Zanzibar paper (`5068.pdf` on the archive branches), and the current domain code. **Do not read the archive branches' ACL implementation** (`src/Kingo.Acl/` on `main-archive` / `dictionary-encoding`) before or during design — it is incomplete, and its lessons are already encoded below as requirements (Mark, 2026-07-15).

## The work item

Two evaluators over the `SubjectSetRewrite` algebra, pure domain code in `Kingo.Acl` — the one project that references both `Kingo.Graphs` and `Kingo.Schemas`, and the home of the fact-lookup port its adapters implement later:

- **Contains** — the membership predicate behind the Check API: subject ∈ subjectset? A short-circuiting boolean walk; the hot path.
- **Expand** — full materialization of a subjectset's membership tree, nothing skipped; the audit/debug/UI view.

One algebra, two interpreters. This is the first joint consumer of both aggregates — rules from `Schema`, facts from the graph — so expect it to pressure-test the algebra's semantics the way SDL pressure-tested the names ([[the-first-consumer-forges-the-domain]]).

## Naming

- The domain predicate is **`Contains`** — set-first: `subjectSet.Contains(subject)` matches the ∋ reading ([[domain-language]]) and carries .NET's strongest prior (`set.Contains(member)`). Not `IsMember`/`IsAMemberOf`, which read member-first and re-invert that reading.
- **`Check`** stays the service/API-level name — Zanzibar's reach word ([[four-service-split-by-load-profile]]).
- **`Kookie`** is the Kingo name for the zookie (decided 2026-07-16): an opaque kernel value naming a point in the store's timeline. Request-side it's a *floor* ("no earlier than this snapshot"); Decision-side it's the *pin* ("evaluated at this snapshot"). "Zookie" survives only as the paper-facing alias ([[storage-versioning-design]]).

## Settled — domain-modeling interview, 2026-07-16

Kernels pinned with Mark before the clean-room session; these are constraints, not suggestions.

- <!-- locked --> **`Contains` answers one question: does the graph's derived closure contain this fact.** There is no "deny" concept in the algebra — `!` is set exclusion, just another operator. Membership at a snapshot, nothing else; "authorized" is the Check host's interpretation of the answer, not the interpreter's concern.
- **The question is a putative `Fact`; everything else is context.** `(SubjectSet, Subject)` is exactly a Fact's content, so the signature is `Contains(Fact fact) → Result<Decision>`: does this statement hold at this snapshot — stored directly or derived through the rewrite algebra (the paper's own modality: check takes a "putative" member). `Fact` now carries `(SubjectSet, Subject)` positionally (reshaped 2026-07-16). The evaluation context — the `Schema` value, the snapshot-pinned fact port, the `TimeProvider` — is construction state, not per-call args. The zookie is consumed at the host edge to pin the port; the interpreter never sees a zookie argument.
- **A subjectset-valued member means literal element membership, not subset reasoning.** The `Subject` DU admits `SubjectSet` (a stored `doc:x#viewer@group:eng#member` is legal), so `Contains(doc:x#viewer @ group:eng#member)` asks whether that subjectset appears as an element of the closure — value equality at the leaves, no second semantics under `!`. Matches Zanzibar: Expand's leaves are "user IDs or usersets."
- **`Decision` carries exactly one Fact — the one judged — and none of the facts consulted to judge it.** Decision = the Fact judged, the verdict, the snapshot pin (`Kookie`), the schema version, the wall timestamp. Reproducibility comes from the store's multi-version history, not the payload.
- **The depth bound is evaluator constructor config** (decided 2026-07-16) — injected context alongside the port and clock, not SDL and not per-call. `DepthExceeded` carries the bound it hit, so audit distinguishes "cycle in data" from "bound too low."
- <!-- locked --> **Operator error semantics are Kleene: the result is a function of operand values, never of evaluation order** (decided 2026-07-16). Error is the third value; an *absorbing* value dominates it: `false` absorbs in `∩` (false & anything = false), `true` absorbs in `∪` (true | anything = true), and for `a ! b` (= `a ∩ ¬b`) both `a = false` and `b = true` absorb to `false`. Short-circuit is legit exactly when the value in hand is absorbing — the unobserved operand's outcome (true, false, or error) cannot change the answer, so skipping it or cancelling its in-flight parallel evaluation is sound. An error surfaces as `Result.Failure` exactly when it is *decisive*: nothing absorbed it, so the answer genuinely depended on the broken operand. The special case already ruled — `a ! b` with `a` true and `b` errored → `Result.Failure`, no Decision — is this rule. Sequential, reversed, and parallel-with-cancellation evaluation all yield the identical verdict; strict error-poisoning was rejected because it is incoherent with short-circuiting (the verdict would depend on scheduling). The residual risk — a broken graph region hiding behind healthy absorbing branches, surfacing only intermittently — is assigned to the Write path and offline validation (schema cycles at SDL parse, data cycles at write or sweep), not to the hot path.
- Related instinct to honor in design: where a no-answer value is ever needed, prefer a domain `Decision.Empty` (collection-style singleton) over null — carry emptiness in the domain, not in the C# language.
- **The error taxonomy is settled** (decided 2026-07-16) — three families, six conditions; see [Error conditions](#error-conditions). Conditions 1–4 are distinct modeled errors owned by `Kingo.Acl`; 5–6 flow through as the port's own error values, propagated untranslated (a wrapper adds a layer without adding a fact). All six are `Result.Failure` per the settled channel; the Kleene rule decides whether a given evaluation surfaces or absorbs them.
- **The pinned port exposes its snapshot token.** An opaque value the interpreter copies into the `Decision` without interpreting, so the Decision is complete at birth — no host-side enrichment.
- **The result channel is `Result<Decision>`.** Undefined namespace/relationship and depth-exceeded are `Result` failures — where no membership answer exists there is no `Decision`, not a Decision wearing an error flag. Fail-closed (error → deny + alarm) is host policy, visible at the edge. Audit covers both channels: success serializes the Decision; failure serializes the error with the request envelope.
- **`Decision` carries two clocks, distinctly:** the snapshot token (logical time — *which* facts) and the wall timestamp from the injected `TimeProvider` (*when* evaluated).
- **`Decision` does not carry the proof path.** The witness doesn't survive `!`: a true verdict under `(this | (parent, viewer)) ! banned` embeds a non-membership claim for `banned`, and non-membership has no path — its proof is the exhausted search (a *false* verdict, conversely, can have a concrete path through `banned`). A path also costs hot-path allocation and leaks other subjects' relationships to any caller allowed to ask yes/no questions. "Why" is recomputed on demand: re-run `Contains` or run `Expand` at the Decision's recorded snapshot — reproducibility is a storage property (multi-version tuples), not a payload property ([[storage-versioning-design]]).

## Error conditions

Three families, six conditions. Every one is a `Result.Failure` — where no membership answer exists there is no `Decision` — and the Kleene rule decides whether a given evaluation surfaces or absorbs it.

### Family 1 — the question is ill-formed against the schema

Caller defects, detectable before any I/O: the `Schema` is an in-memory value, so both checks are pure lookups that fail before the port is ever touched.

1. **Undefined namespace.** The question's resource names a namespace the schema doesn't define — `Contains(document:readme#viewer@user:anne)` against a schema that defines `doc`, not `document`. There is no rewrite rule to interpret, so the question isn't answerable *false* — it isn't answerable at all: the set the question names doesn't exist in this schema's universe. A denial asserts "evaluated, and not a member" and is reproducible at the snapshot; this asserts "no such set." Mapping it to `false` would convert every caller typo and every caller/schema version skew into silent denial — an outage that looks like policy.
2. **Undefined relationship on the question.** The namespace exists but the relationship isn't defined on it — `Contains(folder:x#editor@user:anne)` when `folder` defines `owner`/`viewer`/`banned`/`parent` and no `editor`. Same character as (1), kept distinct because the repair is different (wrong namespace vs wrong relationship) and the drift source is different: relationship vocabularies evolve much faster than namespace sets, so the typical cause is a caller compiled against a newer schema than the evaluator holds. This is requirement 4's boundary: fail-closed applies to *facts*, never to schema drift or caller defects.

### Family 2 — the evaluation couldn't complete

Defects in the graph, or its interaction with the depth bound. These surface mid-walk, after I/O has begun.

3. **Depth exceeded.** The walk hit the constructor-configured bound. The bound cannot be computed at design time because the recursion is not bound by the schema — the schema's rewrite tree is finite and known at parse — it is bound by the *facts*: a tupleset like `(parent, viewer)` re-enters the walk once per stored parent fact, and subjectset-valued members re-enter it once per nesting level. Chain length is whatever users have written — `folder:a#parent@folder:b`, `folder:b#parent@folder:c`, … arbitrarily long, or `folder:a#parent@folder:b` plus `folder:b#parent@folder:a`, a genuine cycle. The bound is an operational guard against user-writable data; the error carries the bound it hit so audit can distinguish "cycle in data" from "bound set too low."
4. **Undefined relationship encountered mid-walk.** Same surface as (2), a different defect: schema/data drift. Facts and schema are separately versioned artifacts that drift independently. Under schema v1, `group:eng#member` is valid and `doc:x#viewer@group:eng#member` is written — validated, stored. Schema v2 removes (or renames) `member` on `group`; the fact is still in the store. An evaluation under v2 reaches that member, needs to expand `group:eng#member`, and finds no such relationship: the fact was valid when written, the schema moved. Write-time validation cannot prevent this — the mutation that broke the fact was a *schema* change, and guarding that side means proving no fact anywhere references the removed relationship, a full-graph scan as a precondition of schema DDL. Two honest layers: a *guard* (schema-change validation, or a deprecate-don't-remove policy) in the Write/schema service design, and this *modeled error* in the evaluator — which cannot assume the guard held for all history, especially when evaluating an old snapshot pin against a schema version from a different moment. The seductive alternative — treat the dangling reference as an empty set — was explicitly rejected: it silently changes authorization outcomes under `!`, where an undefined `banned` would mean *nobody is banned*.

### Family 3 — the context broke

Infrastructure failures, surfaced through the port and propagated untranslated — the interpreter does not wrap them, since a wrapper adds a layer without adding a fact.

5. **Fact lookup failed.** The pinned port's I/O failed outright.
6. **Snapshot unavailable.** The pin points beyond the retention window (GC took it) or cannot be served — the audit-replay horizon from [[storage-versioning-design]].

### Deliberately absent

- **Schema cycles** (mutual computed subjectsets) — rejected at SDL parse; the evaluator never meets one. If validation missed it, it manifests as (3).
- **Timeout / cancellation** — host-edge concern, not a domain error the algebra models.

## Requirements

1. The result is a **`Decision`** (`Kingo` kernel, stub exists), not a bare bool: the Fact judged, verdict, the snapshot evaluated at, schema version, timestamp — the audit event is this value serialized ([[authz-event-logging]]). The timestamp comes from an injected `System.TimeProvider` (requirement 9's "a clock it didn't receive" — this is the received one). Expand needs a sibling result type carrying the tree.
2. Direct membership (`this`) includes **both** direct subject facts **and** the members of subjectset-valued facts stored under the same `SubjectSet` — Zanzibar's userset-expansion clause. A grant to `group:eng#member` must make every member of that set a member here.
3. The walk is **cycle-safe and depth-bounded with a modeled outcome** (a `Result` error, never a crash). Cycles can exist in the schema (mutual computed subjectsets) and in the graph data (parent loops) — and graph data is user-writable, so an uncrashable hot path is a security property, not a nicety.
4. Evaluating against an **undefined namespace or relationship is a modeled error**, distinct from a denial. Fail-closed applies to facts, not to schema drift or caller defects.
5. The only I/O is **fact lookup, injected as a port** — the first genuine port family. The port is handed to the interpreter already **snapshot-pinned**: one Check evaluates against one consistent snapshot end to end (the zookie enters the signature as an opaque token; [[storage-versioning-design]]). Two implementations by design: Check's cached/hedged one, Read+Expand's plain one.
6. The access patterns this work discovers are the **input to the DynamoDB key design** ([[dynamodblite-substrate]]). Expected at minimum: point lookup (does this fact exist?) and range read (all facts under resource#relationship); record what Expand adds.
7. **Exclusion and intersection semantics are settled — Kleene absorption (see Settled) — and must be pinned by tests.** Evaluation order within them is a cost choice precisely because the semantics are order-free; the SDL example's `banned` exclusion is the obvious fixture, including its error cases (decisive vs absorbed).
8. **Tupleset traversal** (`TupleToSubjectSet`): make an explicit, tested decision on how a parent fact whose subject carries a real relationship (rather than `...`) is treated.
   - *Unverified hypothesis, worth testing during design (2026-07-16 — a naming exercise's side effect, not a decision):* the rename to `TupleToSubjectSetRewrite(TuplesetRelationship, ComputedSubjectSetRelationship)` asserts the second component is *the same construct* as `ComputedSubjectSetRewrite`, just rooted at each subject the tupleset walk resolves rather than at this resource — which is Zanzibar's own structure (`computed_userset` nested inside `tuple_to_userset`). If that holds, the `TupleToSubjectSet` arm delegates to the `ComputedSubjectSet` arm per resolved subject instead of reimplementing it, and this requirement's decision is forced rather than chosen: whatever `ComputedSubjectSet` does with a relationship is what the traversal does with it. The thing to check is whether the rooting difference is really the *only* difference — `...` is the obvious place it could break, since a `...` subject has no relationship to compute and may need a distinct arm rather than a delegated one.
9. **Audit emission is an effect at the host edge.** The interpreter returns the `Decision` and never sees a sink, a logger, or a clock it didn't receive (time is an injected `TimeProvider`, per requirement 1).
