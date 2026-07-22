---
title: Kingo document formats
summary: "The documents Kingo's endpoints accept: single namespaces and namespace blocks declare what edges may exist, fact apply blocks mutate the edges that do — all YAML, all unordered atomic batches versioned by the store timeline."
aliases: [kingo-wire-format, kwf, agl, authorization-graph-language]
tags: [spec, documents]
created: 2026-07-22
status: evolving
cites:
  - "[[namespace]]"
  - "[[relationship]]"
  - "[[fact]]"
  - "[[subjectset-rewrite]]"
  - "[[union]]"
  - "[[intersection]]"
  - "[[exclusion]]"
  - "[[this]]"
  - "[[computed-subject-set]]"
  - "[[fact-to-subject-set]]"
  - "[[factset]]"
supersedes: "[[schema-definition-language]]"
---

# Kingo document formats

- [Kingo document formats](#kingo-document-formats)
  - [Namespace documents](#namespace-documents)
    - [The rewrite language](#the-rewrite-language)
  - [Fact documents](#fact-documents)
  - [Open](#open)
  - [Related](#related)

Kingo accepts three kinds of documents: single namespaces, namespace blocks, and fact apply blocks.
The kinds split the way DDL and DML split SQL — definitions on one side, data on the other.

The naming follows Zanzibar's precedent: its namespace configs are protobuf text format — a serialization, never a branded language. These are document formats the same way. No document kind is a language; the one language among them is the **rewrite language**, the [[subjectset-rewrite]] expression grammar embedded in namespace documents. (Renamed 2026-07-22 from AGL, the Authorization Graph Language — its SDL and FML sublanguages and the interim KWF name retired with it; [[domain-language]] records why.)

Both document kinds share one semantic frame:

- A document is an **unordered atomic batch** — one Write transaction, validated on its end state. Ordering exists only between documents.
- Documents are **idempotent**: applying one twice lands the same state. (On the fact side this hangs on drop-of-absent being a no-op — open in [[graph-document-is-bulk-dml]].) There is no script identity, run history, or version metadata — each push versions on the store timeline, and the [[kookie]] pins the config and fact state any evaluation reads ([[sdl-becomes-a-script-language]] records why the flyway-style alternative lost).

## Namespace documents

A namespace document declares namespaces. It is pure declaration: upsert only, create-or-replace per namespace, and absence means nothing — a namespace missing from a document is untouched, never deleted. Removal is not expressible in a namespace document; it is the Write API's explicit `DELETE /namespaces/{name}`, refused while live facts reference the namespace.

The unit of declaration is the **namespace body**: a YAML sequence of [[relationship]] definitions, each a bare name (direct membership) or a name with a rewrite expression ([[subjectset-rewrite]]).

```yaml
- owner
- parent
- viewer: (this | (parent, viewer)) ! banned
- banned
```

Two document forms carry it:

- **Single namespace** — the body alone is the document; the namespace name arrives out of band (`PUT /namespaces/folder`, or the filename in a config repo). The stored artifact and the wire body are the same bytes: a repo directory holds one `<namespace>.yaml` per namespace, and deploy is a loop of PUTs.
- **Block** — root key `namespaces:` holding a map of name to body, upserted as one atomic batch.

```yaml
namespaces:
  file:
    - owner
    - editor: this | owner
    - viewer: (this | editor) ! banned
    - banned
  folder:
    - owner
    - parent
    - viewer: (this | (parent, viewer)) ! banned
    - banned
```

A rewrite may reference a namespace that does not exist yet. Write tolerates the dangling reference; evaluation fails closed on it (error-taxonomy family 1, [[fail-closed]]).

### The rewrite language

The split between YAML and the rewrite language is deliberate: YAML carries the namespace map, comments, and editor tooling; the rewrite expression — `(this | editor | (parent, viewer)) ! banned` — would be awkward in pure YAML, so it lives in a string and is parsed separately (Superpower in `Kingo.Sdl`).

Operator precedence inside an expression:

| Operator | Meaning      | Precedence           | Associativity |
|----------|--------------|----------------------|---------------|
| `!`      | [[exclusion]]    | highest              | left          |
| `&`      | [[intersection]] | lower (same as `\|`) | left          |
| `\|`     | [[union]]        | lower (same as `&`)  | left          |

`!` binds tighter than `&` / `|`. `&` and `|` share precedence and read left to right; mix them with parentheses if grouping matters. Chained `!` associates left, matching set difference: `a ! b ! c` is `(a ! b) ! c`. A run of consecutive same-operator applications is one n-ary node (`a | b | c` is a single three-child union), and a parenthesized operand is opaque. Together these mean any rewrite tree round-trips to a structurally equal tree.

```bnf
<rewrite>             ::= <exclusion> [ ('&' | '|') <exclusion> ]*
<exclusion>           ::= <term> [ '!' <term> ]*
<term>                ::= 'this'
                        | <computed-subjectset>
                        | <fact-to-subjectset>
                        | '(' <rewrite> ')'

<computed-subjectset> ::= <identifier>
<fact-to-subjectset>  ::= '(' <identifier> ',' <identifier> ')'
<identifier>          ::= [a-zA-Z_][a-zA-Z0-9_]*
```

A [[computed-subject-set]] references another relationship in the same namespace. A [[fact-to-subject-set]] walks a [[factset]] relationship (first identifier), then evaluates a second relationship on the resulting resources — inherited permissions ("viewer on folder grants viewer on file via parent"). Computed-subject-set targets and factset first identifiers must name relationships defined in the namespace, and computed-subject-set references must not form a cycle. Both are `Namespace.Create` invariants ([[namespace-create-validation]]), surfacing from a parse as `namespace.dangling_reference` / `namespace.rewrite_cycle`. A factset's second identifier resolves in the namespaces of the resources the facts name, so it is not checked here.

Authoring rules:

- A bare relationship name is the only spelling of "no rewrite" (semantically [[this]]). A `<name>:` pair with a missing value is rejected rather than defaulted — it always reads as a forgotten expression.
- In expression position the rewrite language owns the scalar's raw text, not YAML's typing: a plain `null` is the identifier `null`, a legal relationship name.
- Reserved words, wire-format-level only: `this` and `...` cannot name a relationship in a namespace document. `this` always lexes as the direct-membership keyword; `...` is the fact grammar's resource-member marker and cannot lex in an expression. The core `RelationshipIdentifier` accepts both — the reservation belongs to this format's grammar, and a format with no embedded expression language has no such collision.
- Namespace identity is case-insensitive while YAML keys are not, so two block-document keys normalizing to the same namespace are rejected.

## Fact documents

A fact document is bulk DML over facts: `apply` and `drop` blocks, each holding [[fact]] triples in the canonical text form the core owns (`resource#relationship@member`). `apply` is upsert; `drop` retracts.

```yaml
facts:
  apply:
    - doc:readme#owner@10
    - doc:readme#viewer@group:eng#member
    - folder:a#parent@folder:root#...
  drop:
    - doc:readme#viewer@user:carol
```

The adapter owns only the envelope; the fact grammar stays in core ([[domain-language]]'s Parse boundary rule). Design record and open questions — strict create beside `apply`, drop of an absent fact, preconditions, delete-by-filter — live in [[graph-document-is-bulk-dml]].

## Open

- The block push's endpoint spelling.
- FML's envelope details: whether the `facts:` root key stays now that no document-kind discrimination is needed, and the open questions above.
- Whether a query document kind joins the family — Check is a reachability query over the same graph the other kinds feed.

## Related

- [[schema-definition-language]] — the superseded SDL note; its envelope predates [[schema-dissolves-into-administration]]. Kept for the prior-art record until the corpus reframe lands.
- [[graph-document-is-bulk-dml]] — FML's design record.
- [[dissolve-schema-into-administration]] — the remodel this spec's document forms come from.
- [[domain-language]] — the fact grammar and the naming record, including the retired AGL name.
