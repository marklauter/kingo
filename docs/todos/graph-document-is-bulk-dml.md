---
title: The graph document is bulk DML, not a state dump
summary: "Proposal: FML, the Fact Mutation Language — the fact-side document is a list of create/touch/delete operations in YAML section blocks parsing to a GraphOperation DU, which lives between the edges, not in the domain, since every rule it carries is storage semantics; the Graph/GraphParser/GraphPrinter stubs were deleted and it waits on the first ports project."
tags: [note, todo, sdl, fml, agl, graphs, dml, hexagonal]
created: 2026-07-15
status: open
priority: medium
effort: medium
blocked-by: "[[storage-versioning-design]]"
---

# The graph document is bulk DML, not a state dump

## Observation

`GraphParser` / `GraphPrinter` were stubbed 2026-07-15 as the fact-side mirror of the spec pair ([[specs]]): `Parse(text) → Result<Graph>` plus a `graph.Print()` extension, with the document format left open. Mark's aspiration for that format (2026-07-15) is **bulk DML** — a loader/mutator, not a serialized state. You can add, delete, and (in SQL's frame) patch; patch is empty for a fact, because a fact's domain key is the whole triple and there is no non-key field left to change — a "patched" fact is a different fact. That leaves **create, touch, delete**, which is exactly Zanzibar's `RelationTupleUpdate` set (§2.4) and the set SpiceDB's `RelationshipUpdate` kept unchanged. Two independent designs landing on the same three is evidence the space is that shape:

- `create` — assert, conflict if the fact already exists.
- `touch` — assert, succeed either way (upsert). Exists because re-running a generated document should be a no-op, not a pile of conflicts.
- `delete` — retract. In storage a delete is a tombstone stamp closing the fact's interval, not a row removal (dry-run finding F8); the operation vocabulary is unchanged by that.

The DDL/DML frame is what names the split cleanly ([[specs]] is the DDL half): the schema carries the rules, the facts are the ground data, and this document mutates the data. The language this document is written in is **FML, the Fact Mutation Language** — the DML half of the Authorization Graph Language, as SDL is the DDL half ([[domain-language]] names AGL and its two sublanguages). "Mutation" over "manipulation": SQL's M is a 1970s word for the same slot, and mutation says what the three operations do to the graph. The analogy is not exact — SQL's DML is a language of statements against a live store, while a graph document is a batch handed to Write — but "bulk DML" is the right neighborhood, and it is decisively *not* `pg_dump`'s data section.

## Proposed format

YAML again, with **section blocks keyed by operation** — Mark's shape:

```yaml
create:
  - doc:readme#viewer@user:anne
  - doc:readme#owner@user:bob

delete:
  - doc:readme#viewer@user:carol

touch:
  - group:eng#member@user:dave
```

Each entry is a fact in the canonical text form the core already owns — `Fact.Parse` ([[domain-language]]: `<resource>#<relationship>@<subject>`). The adapter owns only the envelope, exactly as with SDL: the grammar stays in core, the *document* is adapter territory. That keeps the Parse boundary rule intact and means this format needs no new terminal rules.

Sections are the natural fit for a bulk loader — the common document is "here are 400 facts to create" and a per-entry operation tag would be noise on every line. The cost is that operation order becomes *implicit in section order*, which is a real constraint (see open questions).

## `GraphOperation` — and why it is not a domain type

The document parses to a closed DU over the three cases. Sketch:

```csharp
public abstract record GraphOperation(Fact Fact);

public sealed record CreateOperation(Fact Fact) : GraphOperation(Fact);
public sealed record TouchOperation(Fact Fact) : GraphOperation(Fact);
public sealed record DeleteOperation(Fact Fact) : GraphOperation(Fact);
```

`Graph` beats `Fact` as the qualifier (Mark, 2026-07-15). `FactOperation(Fact Fact)` promises every operation names exactly one fact — true today, false the moment delete-by-filter lands, since "drop every viewer of `doc:readme`" acts on the graph and names no single fact. `GraphOperation` makes no such promise, so the DU can grow a filter case without the base lying. (Closing the DU is an implementation detail left open: `SubjectSetRewrite` uses a bare `private protected` ctor, which works because it declares no primary constructor. A base that takes one cannot cleanly also expose a parameterless ctor, so either the base drops the primary constructor and each case declares its own `Fact`, or the DU is closed by convention.)

**It does not live in the domain layer** (Mark, 2026-07-15 — correcting this note's first draft, which put it in `Kingo.Graphs`). An operation sits **between two edges**: born at user IO (a document, a REST call), dead at storage IO (a conditional write). The tell is that every rule it carries is storage semantics, not a domain invariant:

- `create` vs `touch` differ *only* in whether the write carries an if-absent condition — `attribute_not_exists`, nothing more.
- "Delete of an absent fact — no-op or failure?" is a conditional-write question.
- "Is the document a transaction?" is a `TransactWriteItems` question.

One guard sits upstream of all three and changes none of them: every fact write first passes the Write service's spec validation — facts can't lead the spec (2026-07-20, dry-run finding F8). That is the service's invariant at its edge, not a rule the operation type carries, so the storage-semantics argument stands.

A type whose entire rule set is storage semantics is not a domain type; it is the vocabulary of the thing that talks to storage. That it *mentions* `Fact` proves nothing — a SQL `INSERT` mentions a row without being part of the business model. The pure core never ranges over a verb: Check evaluates schema plus facts, Expand the same, and neither has any use for one. Zanzibar agrees, and its placement is the evidence: `RelationTupleUpdate` lives in the **Write API proto**, not in the tuple model — request vocabulary, exactly like SpiceDB's `RelationshipUpdate`.

**This is the port-family trigger.** [[architecture]] has been holding the interface rule for it: *"the interface rule returns when the first genuine port family (storage) arrives."* `IDocumentSerializer` was ceremony because it had one possible adapter forever; a write port has real ones — DynamoDbLite, DynamoDB, an in-memory fake — and `GraphOperation` is its language. So the type wants the ports/application project that does not exist yet: it cannot live in `Kingo.Sdl` (the Write host would depend on a YAML adapter to speak its own commands) and it cannot live in a host (adapters would then depend upward). Placement lands with the storage work — see [[storage-versioning-design]], [[dynamodblite-substrate]].

## `Kingo.Fml` — the adapter

The document's parser is its own project, **`Kingo.Fml`**, beside `Kingo.Sdl` — one adapter per sublanguage of AGL ([[domain-language]]). The pair mirrors the models: `Kingo.Sdl` → `Kingo.Schemas`, `Kingo.Fml` → `Kingo.Graphs`, so the assembly ban the two test suites already enforce between the models carries into the adapters for free. Nothing about FML wants to live in `Kingo.Sdl`: that project is named for the *Schema* Definition Language, and the two documents share no grammar — only a frame.

It is by far the thinner of the pair, and the asymmetry is the design, not an accident:

- **Parser only, no printer.** `parse ∘ print = id` pins the spec pair; there is no such law between a state and a changeset, which is why `GraphPrinter` is gone (below).
- **YamlDotNet, no Superpower.** SDL needs a parser combinator because rewrite expressions are a recursive language with precedence and parens. FML has no embedded language at all — every entry is a fact in the canonical text form core already owns (`Fact.Parse`), so the adapter owns nothing but the envelope and the section blocks.
- **It cannot be stood up yet.** Its parse target is `GraphOperation`, which has no home until the ports project exists — so `Kingo.Fml` references ports *and* `Kingo.Graphs`, and travels with the storage work rather than landing next.

## Consequences — the stubs are gone

All three fact-side stubs from 2026-07-15 were removed the same day rather than left to rot:

- **`GraphPrinter` — deleted.** It existed to be `GraphParser`'s inverse, and there is no `parse ∘ print = id` law between a state and a changeset; the round-trip tests that pin the spec pair have no analogue here. Printing a graph back out is a *dump* — a different artifact that merely shares a vocabulary. If a dump format is ever wanted it returns under its own name.
- **`GraphParser` — deleted.** `Parse(text) → Result<Graph>` denoted a state where a changeset is a sequence of operations, and there is no correct return type to restub it with until `GraphOperation` has a home. It comes back with the ports project, parsing text to operations. The adapter half of the division is unchanged when it does: the fact grammar stays core (`Fact.Parse`), and the adapter owns only the YAML envelope.
- **`Graph` and `GraphTests` — deleted.** Nothing produces a `Graph` on the changeset reading, and the type never had an invariant to be `Create`-only about — the duplicate-fact check was invented to fill the constructor, not asked for by the domain. **The guardrail in [[domain-language]] was right** ("`Graph` names a concept, not a core type — no invariant spans the fact collection"), so that note needs no revision. The word stays available to Check for a read-side compiled form, exactly as the guardrail's own carve-out says — a read-model in the host, never a domain value, the same shape as the `FrozenDictionary` projection in [[immutablearray-for-domain-collections]].

`Kingo.Graphs` is back to `Fact`, `Resource`, `SubjectSet` (the `Subject` wrapper dissolved 2026-07-21; [[resource-fact-case]]); `Kingo.Sdl` is back to the spec pair alone.

## Open questions

These are storage questions, which is why they travel with the ports project rather than blocking anything in core.

- **Delete of an absent fact — no-op or `NotFound`?** `touch` exists because that question has two answers on the create side; the symmetric question deserves an explicit answer rather than a default. Bulk callers usually want the no-op, which would make `delete` the mirror of `touch` and leave `create` the only strict operation. If a strict delete is also wanted, the set grows a fourth case and the symmetry argument gets stronger, not weaker.
- **Is a document a transaction?** All-or-nothing, applied in order? This decides whether the parse result is a bare sequence or a **batch type** carrying invariants — and a batch is the one place a collection of facts genuinely *has* an invariant, unlike stored state: if a document applies atomically, atomicity spans the collection and "no fact appears twice across sections" has a real victim. That is exactly what `Graph` lacked. If the answer is "transaction", a batch type earns its `Create`; if not, `ImmutableArray<GraphOperation>` is the whole story. Naming, if it comes: the changeset framing has stronger priors than `GraphOperations`, since `XOperations` reads as a static helper class in C#.
- **Section order.** With section blocks, a document cannot interleave: it cannot say "delete X, then create X". Either the sections have a fixed defined order (delete-then-create is the usual choice — it makes a document idempotent-ish and lets a section pair express replacement), or a document that both deletes and creates the same fact is rejected as ambiguous. Worth deciding before the format hardens, because it is unfixable afterward without a breaking change.
- **Same fact in two sections** — a defect at parse time, or resolved by section order? Follows directly from the question above.
- **Preconditions.** SpiceDB's `WriteRelationships` carries optional preconditions (must-match / must-not-match filters) so a batch can assert state before applying. Out of scope for a first document format, but worth knowing it is the next thing bulk callers ask for.
- **Delete by filter.** Zanzibar and SpiceDB both offer delete-by-filter (wildcards over resource/relationship/subject) separately from delete-by-tuple. A document listing every fact to delete cannot express "drop every viewer of `doc:readme`". Likely a Write API concern rather than a document one, but it is the other half of what "bulk mutator" usually means.

## Next

- ~~Delete the fact-side stubs~~ — done 2026-07-15: `GraphPrinter`, `GraphParser`, `Graph`, and `GraphTests` all removed. None could survive the changeset reading, and `GraphOperation` has no home until the ports project exists, so there was nothing to restub them *to*. This note is the design record until then.
- **Blocked on the ports/application project** — `GraphOperation` lands there, with the write port. Travels with the storage work: [[storage-versioning-design]], [[dynamodblite-substrate]].
- Settle the delete semantics and the transaction question — they decide whether a batch type exists and what `GraphParser` returns.
- Rebuild `GraphParser` against `GraphOperation` once it has a home, in `Kingo.Fml` (above) — the project the naming question in this note's first draft was reaching for.
- Write the format up properly once settled — likely its own note beside [[specs]], since an FML document is a different artifact from the SDL one.

## Related

- [[specs]] — the DDL half: the spec document, its `spec:`/`namespaces:` envelope, and the parser/printer pair this one deliberately does *not* mirror.
- [[domain-language]] — `Fact` and the fact grammar these documents carry; the `Graph`-is-not-a-type guardrail this proposal vindicates.
- [[four-service-split-by-load-profile]] — Write is the host that would consume these documents.
