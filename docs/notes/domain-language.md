---
type: note
title: Domain language — the tuple grammar and its types
summary: "The ubiquitous language of the whole system: the relation-tuple grammar in Kingo vocabulary (Subject, SubjectSet, Resource, Relationship) and the mapping from each production to its C# type — the contract Kingo (the domain core) implements."
tags: [note, spec, ddd, vocabulary]
created: 2026-07-14
status: evolving
---

# Domain language — the tuple grammar and its types

## Vocabulary

Kingo deliberately renames Zanzibar's terms. A subject need not be human — it can be a client (client-credentials grant), an agent, etc.; "user" is overloaded, and the JWT carries the principal as `sub`. "Subject" is the access-control-theory word (Lampson, XACML, NIST); it says what it actually is.

| Zanzibar (paper) | Kingo        |
|------------------|--------------|
| user             | Subject      |
| userset          | SubjectSet   |
| object           | Resource     |
| relation         | Relationship |
| namespace        | Namespace    |

A note on principals: a subject is a *set of principals*. Many trusted IDPs each hold their own principal record for the same person; all map to one subject. Principals (and their claims) are authn-side and never appear in this grammar — tuples store the unified subject.

## Tuple grammar

Same hybrid-BNF style as [[pdl-yaml]] — legibility over ceremony.

```bnf
<tuple>       ::= <resource> '#' <relationship> '@' <subject>
<resource>    ::= <namespace> ':' <resource-id>
<subject>     ::= <subject-id> | <subjectset>
<subjectset>  ::= <resource> '#' <relationship>
```

Examples (canonical text form):

```
doc:readme#viewer@user:anne             tuple: anne is a viewer of doc:readme
doc:readme#viewer@group:eng#member      tuple: members of group:eng are viewers
doc:readme#viewer                       subjectset: the viewers of doc:readme
```

Note `<tuple>` is structurally `<subjectset> '@' <subject>` — a tuple asserts membership of a subject in a subject set.

### Terminals

Each terminal owns its character rules — there is deliberately no general-purpose `Identifier` type (every aggregate gets its own ID type; a shared identifier was a pre-reboot mistake).

| Terminal              | Rule                                  | Status |
|-----------------------|---------------------------------------|--------|
| `<namespace>`         | `[A-Za-z_][A-Za-z0-9_]*`; case-insensitive — `Parse` normalizes to lowercase (canonical form); unique per aggregate-key rules (enforced by Write against the store) | settled |
| `<relationship>`      | `[A-Za-z_][A-Za-z0-9_]*` or `...`; case-insensitive, normalized like `<namespace>` | settled; `...` is the "unspecified relationship" sentinel |
| `<resource-id>`       | `[A-Za-z0-9_][A-Za-z0-9_.-]*` (dots for `readme.md`); case-**sensitive** — resource ids are client-minted references, not authored vocabulary | provisional |
| `<subject-id>`        | `[A-Za-z0-9_][A-Za-z0-9_.:-]*` (`:` allowed so `user:anne` parses; `#`/`@` reserved); case-sensitive | provisional |

The `...` sentinel is a tuple-grammar concept (Zanzibar §2.1), not a PDL-ism — `RelationshipIdentifier.Nothing`.

## Production → type mapping

The taxonomy, settled 2026-07-14. Only identifiers are `IValue`s; the line is single-primitive-with-a-grammar.

| Production        | C# type                    | Kind |
|-------------------|----------------------------|------|
| `<namespace>`     | `NamespaceIdentifier`      | `IValue<TSelf, string>` — owns its regex, `Parse` returns `Result` |
| `<relationship>`  | `RelationshipIdentifier`   | `IValue<TSelf, string>` |
| `<resource-id>`   | `ResourceIdentifier`       | `IValue<TSelf, string>` (to be written) |
| `<subject-id>`    | `SubjectIdentifier`        | `IValue<TSelf, string>` (to be written) |
| `<resource>`      | `Resource`                 | value record (multi-field, so not `IValue`); implements `IParse<Resource>` — the canonical-text parse contract that `IValue` also inherits. The BCL `TryParse` shape is a separate opt-in (`ITryParse<TSelf>`) reserved for types that cross the ASP.NET binding boundary; nothing in core carries it |
| `<subjectset>`    | `SubjectSet`               | value record; also an inhabitant of the `Subject` DU |
| `<subject>`       | `Subject`                  | DU: `DirectSubject(SubjectIdentifier) \| SubjectSet` |
| `<tuple>`         | `Statement`                | the stored fact — an RDF-style subject–predicate–object triple `(Resource, Relationship, Subject)` with a computed `SubjectSet` projection. Named for what it is (an assertion); `Grant` was rejected (structural edges like `folder:a#parent@folder:b` grant nothing) and `Entitlement` names the *derived* output of Expand, not the stored datum |

Canonical string forms belong to the core: `IValue.Parse` owns each terminal's text form, and the composite `Parse` factories own the `ns:id#rel@subject` notation. Serialization adapters handle *document* formats around them; they never define the grammar.

**The Parse boundary rule (settled 2026-07-14):** a type gets a core `Parse` iff its text form is defined by the tuple grammar itself — single line, delimiters reserved by the terminal rules, trivially invertible from `ToString`, parseable with a few `IndexOf` calls and zero dependencies. Parsing is *invoked* at edges but the rules are *owned* by the type (a pure `string → Result<T>` function violates nothing). The policy model (`Namespace`, `Relationship`, `SubjectSetRewrite`) gets no core `Parse`: it has no grammar-defined text form — its textual existence is only ever inside a document (PDL YAML, JSON), and a third-party parser in the signature (YamlDotNet, Superpower) is the tell that it's adapter territory. The rewrite expression language (`(this | editor) ! banned`) is already a format — precedence, parens, comments — and was born in the adapter. **Graduation criterion:** if the canonical notation ever needs escaping, quoting, encoding variants, or versioning, it has become a wire format and the whole pair (`Parse` + `ToString`) moves to a serialization adapter; core keeps structured construction only. Corollary: value types carry `Parse` (and, opt-in, `ITryParse`) because they must be *capable* of living on the wire — but the wire format itself is defined by the converters in the serialization adapters (JSON string token vs structured object, casing, envelope). Converters invoke `Parse` at the trust boundary; nothing about JSON or YAML leaks inward. The sharpest form of the test is **value vs language**: a type may parse a notation that *represents it* — fixed composition of terminals, no recursion, byte-stable `ToString` inverse, like an ISO 8601 date — but a recursive grammar (operators, precedence, parentheses; PDL's rewrite expressions) is a language, and languages get parsers that live in adapters.

### Policy model (not part of the tuple grammar, same core)

Parse-agnostic and storable — produced equally by the PDL parser, a JSON adapter, or the Write API. Not an AST.

- `Namespace(Name, Relationships)` — a policy definition **as a value** (structural equality, immutable snapshot). Construction mirrors the `Create`/`Parse` split on the value types: the constructor is pure assignment for trusted sources; `Define` is the `Result`-returning factory for untrusted input, rejecting duplicate relationship names (`namespace.duplicate_relationship`, one error per name). Entity-ness — versioning, lifecycle, OCC, authorship — is the Write/PAP context's wrapper and never lives in core. Rule: if a core type grows a version field, a timestamp, or a mutation method, it has crossed the line and belongs to a service.
- `Relationship(Name, Rewrite)` — a named relationship and its rewrite, inside a `Namespace`. The name is free for the policy record because the stored fact is `Statement` — the relationship is the *predicate* of the triple, not the triple itself. Pre-reboot main called this `RelationshipSpec`; Pdl called it `Relation`.
- `SubjectSetRewrite` — the rewrite **algebra**, a closed DU: `This | ComputedSubjectSet | TupleToSubjectSet | Union | Intersection | Exclusion` (the C# type names carry a `Rewrite` suffix: `ThisRewrite`, `UnionRewrite`, ...). Authoring syntax and precedence: [[pdl-yaml]]. Evaluation: one algebra, two interpreters (Check's short-circuiting boolean walk, Expand's full tree materialization) per [[four-service-split-by-load-profile]].

## Aggregate roots (settled 2026-07-14)

Four aggregate roots — every element of the tuple grammar carries an aggregate boundary, and the fact itself gets one too. Each has a unique, immutable **domain key** (never the storage hash key or a PK surrogate; dictionary-encoded ints are a storage-adapter concern that never leaks into the domain):

| Aggregate root | C# namespace          | Domain key |
|----------------|-----------------------|------------|
| `Namespace`    | `Kingo.Namespaces`    | `NamespaceIdentifier` — the *name is the identity*: the grammar quotes it in every fact and policy, so no rename, only a new namespace. (A separate ULID-style key was considered and rejected: it buys admin identity but not rename-freedom, at the cost of Terraform-style illegible tuples.) |
| `Resource`     | `Kingo.Resources`     | the composite value itself (`namespace:resource-id`); resources have no stored state — they anchor statements. Carries `NamespaceIdentifier` as an FK-style reference-by-identity: `Namespace` does not own resources (a namespace is purely what PDL defines), and referential integrity (namespace exists, relationship defined) is Write's cross-aggregate check at mutation time |
| `Subject`      | `Kingo.Subjects`      | `SubjectIdentifier` — the unified identity a set of authn-side principals maps to |
| `Statement`    | `Kingo.Statements`    | the whole triple — a create/delete-only fact, never mutated |

Layout rule: **root `Kingo` holds the cross-cutting vocabulary — the identifier IValues** (aggregates reference other aggregates by identity, so identifiers are the shared currency); each plural C# namespace holds one aggregate's types. Plural names avoid class-name collisions (CA1724 compares exact names). Dependency flow is acyclic: `Namespaces` → root; `Resources` → root; `Subjects` → `Resources` + root; `Statements` → `Resources` + `Subjects` + root.

## Open items

- `<resource-id>` and `<subject-id>` character rules are provisional (see terminals table); confirm before the Write API freezes them.

## Related

- [[architecture]] — where the layers live; this note is the language the core layer speaks.
- [[dissolve-kingo-pdl-under-hexagonal-layout]] — the migration that implements this spec.
- [[pdl-yaml]] — the authoring format for the policy model.
- [[four-service-split-by-load-profile]] — how the services consume the language.
- [[immutablearray-for-domain-collections]] — why domain values carry `ImmutableArray<T>` and the equality/default caveats that ride along.
