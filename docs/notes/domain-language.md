---
title: Domain language — the fact grammar and its types
summary: "The ubiquitous language of the whole system: the fact grammar in Kingo vocabulary (Subject, SubjectSet, Resource, Relationship) and the mapping from each production to its C# type — the contract Kingo (the domain core) implements."
tags: [note, spec, ddd, vocabulary]
created: 2026-07-14
status: evolving
---

# Domain language — the fact grammar and its types

## Vocabulary

Kingo deliberately renames Zanzibar's terms. A subject need not be human — it can be a client (client-credentials grant), an agent, etc.; "user" is overloaded, and the JWT carries the principal as `sub`. "Subject" is the access-control-theory word (Lampson, XACML, NIST); it says what it actually is.

| Zanzibar (paper)        | Kingo        |
|-------------------------|--------------|
| user                    | Subject      |
| userset                 | SubjectSet   |
| object                  | Resource     |
| relation                | Relationship |
| namespace               | Namespace    |
| relation tuple          | Fact         |
| namespace configuration | Schema (the curated set of namespaces an SDL document defines) |

**Naming decisions (2026-07-15, the reach test):** the right name is the one an unprimed reader's strongest prior already matches. `Fact` replaced `Statement` — "statement" is overloaded with parser and programming-language senses in a codebase full of parsing, while "fact" has one sense and completes the Datalog frame: the **schema** carries the rules, the **facts** are the ground data, Check is derivation over both (rules + facts ⊢ entitlements — the derived output; facts are the premises, and a fact's granting power is on loan from the schema: the same edge is a grant under `viewer: this`, structural under `parent`, inert if the rewrite omits `this`). `Schema` replaced `Policy` — "policy" has a split prior (IAM half reads *grants*, OPA half reads *rules*), so it points at both aggregate roots at once, while three frames converge on schema: DDL defines a schema, SpiceDB calls exactly this artifact the schema, and RDF's name for "the definitions that give statements their vocabulary and rules" is RDF Schema. "Policy" exits the vocabulary unspent, available if a genuine policy concept (caveats, conditions) arrives later. A collection of facts is **the graph** (2026-07-15): each fact is an edge, a set of RDF statements is officially a graph, and Check is a reachability query over it. The slate coheres as one sentence: *the schema defines what edges may exist, the graph is the edges that do exist, the rewrite rules derive edges from other edges, and Check walks the graph.* Guardrail: `Graph` names a concept, not a core type — no invariant spans the fact collection (each fact is its own consistency boundary), so the graph is the store-side/query-side word; if a `Graph` type ever appears it is a read-model inside the Check host, never a domain value.

**The format (2026-07-15, renamed 2026-07-22):** the umbrella over Kingo's authored formats is the **Kingo Wire Format (KWF)** ([[kingo-wire-format]]), with two document kinds — the **namespace document**, which declares what edges may exist ([[kingo-wire-format]]), and the **fact document**, which mutates the edges that do ([[graph-document-is-bulk-dml]]) — splitting the way DDL and DML split SQL. The precedent is Zanzibar's own: its namespace configs are protobuf text format, a serialization with a schema behind it, never a branded language. KWF names the serialization the same way — the envelope family the endpoints accept — and the one genuine language inside it is the **rewrite language**, the expression grammar embedded in namespace documents. It spends nothing the guardrail forbids: KWF names a wire format, not a type, and `Graph` stays a concept plus the `Kingo.Graphs` assembly, never a domain value. It is sized for what may come — Check is a reachability query over the same graph, so a query document kind would fit without a rename. Retired: **AGL, the Authorization Graph Language**, and its sublanguage names **SDL** and **FML** (named 2026-07-15, retired 2026-07-22) — they claimed languages about the graph, but the documents define namespaces and mutate facts; the graph is the runtime object, not the documents' subject, and neither document kind is a language. AGL's brandless-on-principle argument (SQL's "Structured" is a property, not a product) died with the language framing: a wire format is a product artifact, so the product's name belongs on it. `Kingo.Sdl` and the planned `Kingo.Fml` keep their assembly names until the [[dissolve-schema-into-administration]] mechanical work decides renames. Rejected earlier and still dead: `KQL` (Kusto and Kibana are both live query languages), and naming the umbrella nothing at all (defensible — the two document kinds share no grammar, only a frame — but the DDL/DML frame is load-bearing enough in these notes to deserve a word).

A note on principals: a subject is a *set of principals*. Many trusted IDPs each hold their own principal record for the same person; all map to one subject. Principals (and their claims) are authn-side and never appear in this grammar — facts store the unified subject.

## Fact grammar

Same hybrid-BNF style as [[schema-definition-language]] — legibility over ceremony.

```bnf
<fact>        ::= <resource> '#' <relationship> '@' <subject>
<resource>    ::= <namespace> ':' <resource-id>
<subject>     ::= <subject-id> | <subjectset> | <resource> '#...'
<subjectset>  ::= <resource> '#' <relationship>
```

Examples (canonical text form):

```
doc:readme#viewer@user:anne             fact: anne is a viewer of doc:readme
doc:readme#viewer@group:eng#member      fact: members of group:eng are viewers
folder:x#parent@folder:y#...            fact: folder:y (the resource itself) is a parent of folder:x
doc:readme#viewer                       subjectset: the viewers of doc:readme
```

Note `<fact>` is structurally `<subjectset> '@' <subject>` — a fact asserts membership of a subject in a subject set. Read denotationally (2026-07-21; [[resource-fact-case]]), every occupant of the `<subject>` seat denotes a *set of subjects*, in one of three encodings: a subject-id denotes its singleton set; a subjectset denotes the derived set its relationship defines; a resource with `#...` is a walkable pointer, denoting sets only through whatever relationship a consumer pairs it with. **A fact asserts inclusion: set L (the left subjectset) includes set R (the seat's denotation).** `doc:readme#viewer@user:anne` is the singleton case — {anne} ⊆ viewers; x ∈ S is {x} ⊆ S, so membership is inclusion's degenerate form. Storage semantics only: questions stay membership-shaped, with no subset reasoning in `Contains` ([[rewrite-interpreters]]). The object-object facts are the graph the evaluators walk; the other two encodings are set algebra ([[the-walk-gathers-constraints-set-theory-decides]]). The seat keeps the name `<subject>` (resolved 2026-07-21; [[resource-fact-case]]): the paper's ⟨user⟩-word problem lived in the types, not the grammar — the party is `<subject-id>`/`SubjectIdentifier`, and "subject" names the seat, a term deriving a set of subjects.

### Terminals

Each terminal owns its character rules — there is deliberately no general-purpose `Identifier` type (every aggregate gets its own ID type; a shared identifier was a pre-reboot mistake).

| Terminal              | Rule                                  | Status |
|-----------------------|---------------------------------------|--------|
| `<namespace>`         | `[A-Za-z_][A-Za-z0-9_]*`; case-insensitive — `Parse` normalizes to lowercase (canonical form); unique per aggregate-key rules (enforced by Write against the store) | settled |
| `<relationship>`      | `[A-Za-z_][A-Za-z0-9_]*`; case-insensitive, normalized like `<namespace>` | settled; `...` removed 2026-07-21 — it is the resource-member marker, not a relationship ([[resource-fact-case]]) |
| `<resource-id>`       | `[A-Za-z0-9_][A-Za-z0-9_.-]*` (dots for `readme.md`); case-**sensitive** — resource ids are client-minted references, not authored vocabulary | provisional |
| `<subject-id>`        | `[A-Za-z0-9_][A-Za-z0-9_.:-]*` (`:` allowed so `user:anne` parses; `#`/`@` reserved); case-sensitive | provisional |

The `#...` marker is fact-grammar punctuation (Zanzibar §2.1), not an SDL-ism and not a relationship: it closes the resource-member production, so `folder:y#...` is the resource itself as a member — a `Fact.ResourceFact` (ruled 2026-07-21; [[resource-fact-case]]).

## Production → type mapping

The taxonomy, settled 2026-07-14. Only identifiers are `IValue`s; the line is single-primitive-with-a-grammar.

| Production        | C# type                    | Kind |
|-------------------|----------------------------|------|
| `<namespace>`     | `NamespaceIdentifier`      | `IValue<TSelf, string>` — owns its regex, `Parse` returns `Result` |
| `<relationship>`  | `RelationshipIdentifier`   | `IValue<TSelf, string>` |
| `<resource-id>`   | `ResourceIdentifier`       | `IValue<TSelf, string>` |
| `<subject-id>`    | `SubjectIdentifier`        | `IValue<TSelf, string>` |
| `<resource>`      | `Resource`                 | value record (multi-field, so not `IValue`); implements `IParse<Resource>` — the canonical-text parse contract that `IValue` also inherits. The BCL `TryParse` shape is a separate opt-in (`ITryParse<TSelf>`) reserved for types that cross the ASP.NET binding boundary; nothing in core carries it |
| `<subjectset>`    | `SubjectSet`               | value record; the left-hand side of every fact, and the member seat of a `Fact.SubjectSetFact` |
| `<subject>`       | the `Subject` seat of `Fact`'s cases | the seat production — a term deriving a set of subjects, the paper's ⟨user⟩. No wrapper type: `<subject-id>` seats `SubjectIdentifier` directly (the `Subject` record dissolved 2026-07-21 — subjects exist only as identifiers inside facts; [[resource-fact-case]]), `<subjectset>` seats `SubjectSet`, `<resource> '#...'` seats `Resource`. The shape choice lives on `Fact`, not on a member type (refactored 2026-07-19: the `Subject` DU dissolved into `Fact`'s cases) |
| `<fact>`         | `Fact`                | the stored fact — a set-membership assertion, an RDF triple read set-first (settled 2026-07-15): the RDF-subject is the `SubjectSet`, the predicate is membership itself (∋), the RDF-object is the member asserted into the set — so Zanzibar's tuple order isn't backwards, and the param order mirrors the text form. A closed union over the member's shape (refactored 2026-07-19): `Fact.SubjectFact(SubjectSet, SubjectIdentifier)` \| `Fact.SubjectSetFact(SubjectSet, SubjectSet)` \| `Fact.ResourceFact(SubjectSet, Resource)` (third case ruled 2026-07-21 — the `#...` member is the resource itself; [[resource-fact-case]]) — each case `(SubjectSet, member)` **structurally**, not `(Resource, Relationship, Subject)` (reshaped 2026-07-16): the grammar note above already read `<fact>` as `<subjectset> '@' <subject>`, and the set-first triple reading names `SubjectSet` as one thing — the flattened three-part shape contradicted both, and made `SubjectSet` a derived property reconstructing on every access. The question `Contains` asks is a putative `SubjectFact` — the case held as a hypothesis rather than a stored assertion; no separate question type (unified 2026-07-19; [[rewrite-interpreters]]). Named for what it is (an assertion); `Grant` was rejected (structural edges like `folder:a#parent@folder:b` grant nothing) and `Entitlement` names the *derived* output of Expand, not the stored datum. `Subject` over `principal`: subject is the standard term for the party an assertion is about (JWT `sub`, XACML); principals are authn-side and map onto a subject |

Canonical string forms belong to the core: `IValue.Parse` owns each terminal's text form, and the composite `Parse` factories own the `ns:id#rel@subject` notation. Serialization adapters handle *document* formats around them; they never define the grammar.

**The Parse boundary rule (settled 2026-07-14):** a type gets a core `Parse` iff its text form is defined by the fact grammar itself — single line, delimiters reserved by the terminal rules, trivially invertible from `ToString`, parseable with a few `IndexOf` calls and zero dependencies. Parsing is *invoked* at edges but the rules are *owned* by the type (a pure `string → Result<T>` function violates nothing). The schema model (`Namespace`, `Relationship`, `SubjectSetRewrite`) gets no core `Parse`: it has no grammar-defined text form — its textual existence is only ever inside a document (SDL YAML, JSON), and a third-party parser in the signature (YamlDotNet, Superpower) is the tell that it's adapter territory. The rewrite expression language (`(this | editor) ! banned`) is already a format — precedence, parens, comments — and was born in the adapter. **Graduation criterion:** if the canonical notation ever needs escaping, quoting, encoding variants, or versioning, it has become a wire format and the whole pair (`Parse` + `ToString`) moves to a serialization adapter; core keeps structured construction only. Corollary: value types carry `Parse` (and, opt-in, `ITryParse`) because they must be *capable* of living on the wire — but the wire format itself is defined by the converters in the serialization adapters (JSON string token vs structured object, casing, envelope). Converters invoke `Parse` at the trust boundary; nothing about JSON or YAML leaks inward. The sharpest form of the test is **value vs language**: a type may parse a notation that *represents it* — fixed composition of terminals, no recursion, byte-stable `ToString` inverse, like an ISO 8601 date — but a recursive grammar (operators, precedence, parentheses; SDL's rewrite expressions) is a language, and languages get parsers that live in adapters.

### Schema model (not part of the fact grammar, same core)

Parse-agnostic and storable — produced equally by the SDL parser, a JSON adapter, or the Write API. Not an AST.

- `Namespace(Name, Relationships)` — a namespace definition **as a value** (structural equality, immutable snapshot). `Create` is the only construction path (ctor is private) — the house Cons: a `Result`-returning validating factory with staged checks — duplicate relationship names (`namespace.duplicate_relationship`, one error per name), then dangling intra-namespace rewrite references (`namespace.dangling_reference`), then cycles in the computed-subjectset reference graph (`namespace.rewrite_cycle`, each error carrying the full cycle path) — per [[namespace-create-validation]]. A `Namespace` that exists satisfies its invariants; there is no trusted-assignment bypass (settled 2026-07-14 — unlike `IValue.Unchecked`, which wraps an already-validated primitive, a composite's invariants are relational and must hold at every construction). Entity-ness — versioning, lifecycle, OCC, authorship — is the Write context's wrapper and never lives in core. Rule: if a core type grows a version field, a timestamp, or a mutation method, it has crossed the line and belongs to a service.
- `Schema(Namespaces)` — the complete schema as a value: what a schema document (SDL, JSON) defines. **The config-side aggregate root** (2026-07-14; see aggregate table): a set of namespaces curated together, the unit of atomic config change. Never empty (`schema.empty`: the absence of namespaces is the absence of a schema — model that as not having one) and names are unique within it (`schema.duplicate_namespace`); both enforced by `Create`, the only construction path (ctor private, same rule as `Namespace`). No core `Parse` — adapters parse documents (`SdlDocument.Parse`) and project a `Schema` from the result. Store-wide checks stay with Write.
- `Relationship(Name, Rewrite)` — a named relationship and its rewrite, inside a `Namespace`. The name is free for the schema record because the stored fact is `Fact` — the relationship is the *predicate* of the triple, not the triple itself. Pre-reboot main called this `RelationshipSpec`; Pdl called it `Relation`.
- `SubjectSetRewrite` — the rewrite **algebra**, a closed DU: `This | ComputedSubjectSet | FactToSubjectSet | Union | Intersection | Exclusion` (the C# type names carry a `Rewrite` suffix: `ThisRewrite`, `UnionRewrite`, ...). Authoring syntax and precedence: [[schema-definition-language]]. Evaluation: one algebra, two interpreters (Check's short-circuiting boolean walk, Expand's full tree materialization) per [[four-service-split-by-load-profile]].

## Aggregate roots (two — settled 2026-07-15)

Two aggregate roots, one per side of the system: config and data. Aggregates are consistency boundaries around **stored state** — the earlier four-root table (Namespace/Resource/Subject/Fact, then Schema/Resource/Subject/Fact) was grammar-driven ("every grammar element carries an aggregate boundary") and collapsed once the state test was applied: only `Schema` and `Fact` are ever stored. Each root has a unique, immutable **domain key** (never the storage hash key or a PK surrogate; dictionary-encoded ints are a storage-adapter concern that never leaks into the domain):

| Aggregate root | C# namespace          | Domain key |
|----------------|-----------------------|------------|
| `Schema`       | `Kingo.Schemas`      | `SchemaIdentifier` — the name, name-as-identity (2026-07-15, provisional: see open items). Same grammar as `NamespaceIdentifier` (`^[A-Za-z_][A-Za-z0-9_]*$`, lowercase-normalized) because it is the same kind of thing — authored vocabulary, unlike a client-minted `<resource-id>`. Replaced `Namespace` as the config-side root 2026-07-14. `Namespace` is now an entity within `Schema`, identity local (schema + `NamespaceIdentifier`); the name-is-the-identity reasoning still holds one level down: no rename, only a new namespace. (A separate ULID-style key for namespaces was considered and rejected: it buys admin identity but not rename-freedom, at the cost of Terraform-style illegible facts.) |
| `Fact`    | `Kingo.Graphs`    | the whole triple — a create/delete-only fact, never mutated |

`Resource` and `SubjectSet` are **value objects of the fact context** (folded into the fact context 2026-07-15 (C# namespace `Kingo.Graphs`)): no stored state, no lifecycle, no consistency boundary. A resource anchors facts and carries `NamespaceIdentifier` as an FK-style reference-by-identity (`Namespace` does not own resources; referential integrity — namespace exists, relationship defined — is Write's cross-aggregate check at mutation time). A subject is the unified identity a set of authn-side principals maps to — Zanzibar-style, there is **no user store**: subjects exist only as identifiers inside facts; identity lives with authn. `SubjectSet` doubles as Check/Expand request vocabulary, so hosts import `Kingo.Graphs` for request shapes — accepted.

Layout rule: **root `Kingo` holds the cross-cutting vocabulary — the identifier IValues** (aggregates reference other aggregates by identity, so identifiers are the shared currency); each plural namespace holds one aggregate's types, named for the *context*, not necessarily the root's plural — `Schemas` holds the `Schema` root, `Graphs` holds the `Fact` root (a graph is what a collection of facts *is*; `Kingo.Graphs.Fact` reads as "a fact in the graph"). Plural names avoid class-name collisions (CA1724 compares exact names). Dependency flow is acyclic and flat: `Schemas` → root; `Graphs` → root.

**Each of those is its own project** (2026-07-15): `Kingo.Graphs` and `Kingo.Schemas` are assemblies, not just namespaces inside `Kingo`. The namespaces and their dependency flow are unchanged — the split makes the flow structural rather than conventional, so the compiler refuses what the layout rule only asked for. `Kingo` is now the shared kernel and holds nothing but the identifiers; its architecture suite pins the namespace flat (`^Kingo$`), so a sub-namespace reappearing there means a model was parked in the kernel instead of given its own project. `Kingo.Graphs.Tests` and `Kingo.Schemas.Tests` each carry a `[Fact]` banning an assembly reference to the other half — the two models meet in the rewrite interpreter ([[rewrite-interpreters]]), never in each other. Consumers narrow accordingly: `Kingo.Sdl` references `Kingo.Schemas` alone. `Decision` and `Expansion` live in `Kingo.Closures` (moved 2026-07-18), the interpreter project — the one that references both `Kingo.Graphs` and `Kingo.Schemas` ([[four-service-split-by-load-profile]], [[rewrite-interpreters]]).

## Open items

- `<resource-id>` and `<subject-id>` character rules are provisional (see terminals table); confirm before the Write API freezes them.
- **`Schema` aggregate follow-ups** (root swap executed 2026-07-14: `Kingo.Schemas` C# namespace, table above updated; `Schema` is a set of namespaces *curated together* — grouped by human intent, not required to reference each other; no referential-closure invariant, though a rewrite reference to a namespace outside the schema is dangling and checkable at `Create`). The domain key is **settled provisionally 2026-07-15**: `SchemaIdentifier`, name-as-identity — so no rename, only a new schema; `Schema.Create(name, namespaces)` carries it and SDL's `schema:` key spells it ([[schema-definition-language]]). Worth revisiting once the reference question below lands: the ULID-style key was rejected for *namespaces* because a surrogate would make every fact illegible, but that argument only bites if the identifier appears in the fact grammar. If schema scope is resolved out-of-band, a surrogate key plus a mutable display name becomes available again — and admin rename-freedom matters more for a schema than for a namespace. Still open: how schema scope appears in references — **Schema picks up addressability** within an installation (two schemas may both define `doc`), so either the fact grammar grows a schema segment or facts/resources carry `SchemaIdentifier` outside the text form. Schema versioning feeds [[storage-versioning-design]]. **Accounts stay out of the domain**: an account holds many schemas administratively, but Kingo's multi-tenant mode is independent installations (a container or isolated AWS account per customer) — tenancy is deployment, never a domain key.

## Related

- [[architecture]] — where the layers live; this note is the language the core layer speaks.
- [[dissolve-kingo-pdl-under-hexagonal-layout]] — the migration that implements this spec.
- [[schema-definition-language]] — the authoring format for the schema model.
- [[four-service-split-by-load-profile]] — how the services consume the language.
- [[immutablearray-for-domain-collections]] — why domain values carry `ImmutableArray<T>` and the equality/default caveats that ride along.
