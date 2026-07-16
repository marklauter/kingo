---
type: todo
title: The graph document is bulk DML, not a state dump
summary: "Proposal: the fact-side document is a list of create/touch/delete operations in YAML section blocks parsing to a FactOperation DU, not a serialized graph — which conflicts with GraphParser as stubbed, empties GraphPrinter of meaning, and puts the Graph value type back in question."
tags: [note, todo, sdl, graphs, dml]
created: 2026-07-15
status: open
priority: medium
effort: medium
---

# The graph document is bulk DML, not a state dump

## Observation

`GraphParser` / `GraphPrinter` were stubbed 2026-07-15 as the fact-side mirror of the schema pair ([[schema-definition-language]]): `Parse(text) → Result<Graph>` plus a `graph.Print()` extension, with the document format left open. Mark's aspiration for that format (2026-07-15) is **bulk DML** — a loader/mutator, not a serialized state. You can add, delete, and (in SQL's frame) patch; patch is empty for a fact, because a fact's domain key is the whole triple and there is no non-key field left to change — a "patched" fact is a different fact. That leaves **create, touch, delete**, which is exactly Zanzibar's `RelationTupleUpdate` set (§2.4) and the set SpiceDB's `RelationshipUpdate` kept unchanged. Two independent designs landing on the same three is evidence the space is that shape:

- `create` — assert, conflict if the fact already exists.
- `touch` — assert, succeed either way (upsert). Exists because re-running a generated document should be a no-op, not a pile of conflicts.
- `delete` — retract.

The DDL/DML frame is what names the split cleanly ([[schema-definition-language]] is the DDL half): the schema carries the rules, the facts are the ground data, and this document mutates the data. The analogy is not exact — SQL's DML is a language of statements against a live store, while a graph document is a batch handed to Write — but "bulk DML" is the right neighborhood, and it is decisively *not* `pg_dump`'s data section.

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

Each entry is a tuple in the canonical text form the core already owns — `Fact.Parse` ([[domain-language]]: `<resource>#<relationship>@<subject>`). The adapter owns only the envelope, exactly as with SDL: the grammar stays in core, the *document* is adapter territory. That keeps the Parse boundary rule intact and means this format needs no new terminal rules.

Sections are the natural fit for a bulk loader — the common document is "here are 400 facts to create" and a per-entry operation tag would be noise on every line. The cost is that operation order becomes *implicit in section order*, which is a real constraint (see open questions).

## `FactOperation` — the type the document parses to

Settled 2026-07-15 (Mark): the operation is a domain type, a closed DU over the three cases, carrying the `Fact` it acts on. Case names follow the house pattern — DU cases suffix with the base's last word, as `ThisRewrite : SubjectSetRewrite` does:

```csharp
public abstract record FactOperation(Fact Fact)
{
    // closed: private protected keeps the case list to this file, as SubjectSetRewrite does
    private protected FactOperation() : this(null!) { }
}

public sealed record CreateOperation(Fact Fact) : FactOperation(Fact);
public sealed record TouchOperation(Fact Fact) : FactOperation(Fact);
public sealed record DeleteOperation(Fact Fact) : FactOperation(Fact);
```

(`SubjectSetRewrite` closes itself with a bare `private protected SubjectSetRewrite() { }` because it declares no primary constructor. A base that *does* take one cannot also expose a parameterless ctor cleanly — so either the base drops the primary constructor and each case declares its own `Fact`, or the DU is closed by convention rather than by the compiler. Decide at implementation time; the shape above is the sketch, not the settled code.)

It belongs in core (`Kingo.Graphs`), not the adapter: an operation against the graph is domain vocabulary that Write speaks regardless of how it arrived — a REST caller posting one operation and a document carrying four hundred are the same concept, and only one of them involves YAML. The DU is what makes the exhaustive `switch` at the apply site total, which is the whole point of modeling it as a type rather than an enum-plus-payload.

The base carrying `Fact` (rather than each case declaring it) says every operation names exactly one fact — the invariant that makes delete-by-filter *not* a member of this DU (see open questions). If filter-deletes ever join, that shape has to give.

## Consequences for the stubs

- **`GraphParser` as written conflicts with this.** `Parse(text) → Result<Graph>` assumes the document denotes a *state*; a changeset is not a state, it is a sequence of operations. The return type wants to be `Result<ImmutableArray<FactOperation>>`.
- **`GraphPrinter` lost its meaning — deleted 2026-07-15.** It existed to be `GraphParser`'s inverse, and there is no `parse ∘ print = id` law between a state and a changeset — the round-trip tests that pin the schema pair have no analogue here. Printing a `Graph` back out is a *dump*, a different artifact from a changeset that happens to share a vocabulary. Removed immediately rather than left to rot, since nothing about the open questions could save it; if a dump format is ever wanted it comes back under its own name, as its own artifact.
- **The `Graph` value type goes back in question.** It was added 2026-07-15 at Mark's instruction, and flagged in its own doc comment as contradicting [[domain-language]]'s guardrail ("`Graph` names a concept, not a core type — no invariant spans the fact collection"). On the changeset reading, nothing produces a `Graph`: the parser yields operations, Write consumes operations, and the only thing that would ever want the type is a read-model inside the Check host — which is precisely what the guardrail already says. **The guardrail was right.** If this proposal lands, `Graph` and its tests come out and the note needs no revision after all.

## Open questions

- **Delete of an absent fact — no-op or `NotFound`?** `touch` exists because that question has two answers on the create side; the symmetric question deserves an explicit answer rather than a default. Bulk callers usually want the no-op, which would make `delete` the mirror of `touch` and leave `create` the only strict operation. If a strict delete is also wanted, the set grows a fourth case and the symmetry argument gets stronger, not weaker.
- **Is a document a transaction?** All-or-nothing, applied in order? This decides whether the parse result is an ordered sequence or an unordered set, and it is the difference between the adapter returning a list and returning something with duplicate detection at `Create` (the way `Graph.Create` rejects `graph.duplicate_fact` today).
- **Section order.** With section blocks, a document cannot interleave: it cannot say "delete X, then create X". Either the sections have a fixed defined order (delete-then-create is the usual choice — it makes a document idempotent-ish and lets a section pair express replacement), or a document that both deletes and creates the same fact is rejected as ambiguous. Worth deciding before the format hardens, because it is unfixable afterward without a breaking change.
- **Same fact in two sections** — a defect at parse time, or resolved by section order? Follows directly from the question above.
- **Preconditions.** SpiceDB's `WriteRelationships` carries optional preconditions (must-match / must-not-match filters) so a batch can assert state before applying. Out of scope for a first document format, but worth knowing it is the next thing bulk callers ask for.
- **Delete by filter.** Zanzibar and SpiceDB both offer delete-by-filter (wildcards over resource/relationship/subject) separately from delete-by-tuple. A document listing every tuple to delete cannot express "drop every viewer of `doc:readme`". Likely a Write API concern rather than a document one, but it is the other half of what "bulk mutator" usually means.

## Next

- ~~Delete `GraphPrinter`~~ — done 2026-07-15: it could not survive any answer to the questions below, so it went now rather than later.
- Settle the delete semantics and the transaction/ordering questions — they change the parse result's type, not just the docs.
- Restub `GraphParser` around `Result<ImmutableArray<FactOperation>>` (its stub carries a `known wrong` marker until then); delete `Graph` plus `GraphTests` pending the same.
- Write the format up properly once settled — likely its own note beside [[schema-definition-language]], since a DML document is a different artifact from the DDL one.

## Related

- [[schema-definition-language]] — the DDL half: the schema document, its `schema:`/`namespaces:` envelope, and the parser/printer pair this one deliberately does *not* mirror.
- [[domain-language]] — `Fact` and the tuple grammar these documents carry; the `Graph`-is-not-a-type guardrail this proposal vindicates.
- [[four-service-split-by-load-profile]] — Write is the host that would consume these documents.
