---
title: namespace documents
summary: "The declaration document Kingo's Write endpoint accepts: a YAML sequence of relationship definitions (a name and its subjectset-rewrite) grouped under a namespace, in one of two forms (single namespace or block), upsert-only and versioned by the store timeline."
tags: [spec, documents]
created: 2026-07-22
status: evolving
cites:
  - "[[namespace]]"
  - "[[relationship]]"
  - "[[subjectset-rewrite]]"
  - "[[this]]"
  - "[[computed-subject-set]]"
  - "[[fact-to-subject-set]]"
  - "[[factset]]"
  - "[[union]]"
  - "[[intersection]]"
  - "[[exclusion]]"
  - "[[fail-closed]]"
supersedes: "[[schema-definition-language]]"
---

# Namespace documents

A namespace document defines one or more namespaces.

A namespace document is an unordered atomic batch: one Write transaction, validated on its end state and idempotent, so applying it twice lands the same state. Ordering exists only between documents. There is no script identity, run history, or version metadata ([[sdl-becomes-a-script-language]] records why the flyway-style alternative lost). Each push versions on the store timeline, and the [[kookie]] pins the config state any evaluation reads.

It is pure declaration: upsert only, create-or-replace per namespace, and absence means nothing — a namespace missing from a document is untouched, never deleted. Removal is not expressible in a namespace document. It is the Write API's explicit `DELETE /namespaces/{name}`, refused while live facts reference the namespace.

The unit of declaration is the namespace body: a YAML sequence of [[relationship]] definitions, each a name paired with its [[subjectset-rewrite]] (a bare name is the same definition with the rewrite left implicit as [[this]]).

```yaml
- owner
- parent
- viewer: (this | (parent, viewer)) ! banned
- banned
```

Two document forms carry it:

- Single namespace — the body alone is the document; the namespace name arrives out of band (`PUT /namespaces/folder`, or the filename in a config repo). The stored artifact and the wire body are the same bytes: a repo directory holds one `<namespace>.yaml` per namespace, and deploy is a loop of PUTs.
- Block — root key `namespaces:` holding a map of name to body, upserted as one atomic batch.

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

## The rewrite language

YAML carries the namespace map, comments, and editor tooling. The rewrite expression `(this | editor | (parent, viewer)) ! banned` would be awkward in pure YAML, so it lives in a string and is parsed separately (Superpower in `Kingo.Sdl`).

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

- A bare relationship name is the only spelling of implicit [[this]]. A `<name>:` pair with a missing value is rejected rather than defaulted — it always reads as a forgotten expression.
- In expression position the rewrite language owns the scalar's raw text, not YAML's typing: a plain `null` is the identifier `null`, a legal relationship name.
- Reserved words, wire-format-level only: `this` and `...` cannot name a relationship in a namespace document. `this` always lexes as the direct-membership keyword; `...` is the fact grammar's resource-member marker and cannot lex in an expression. The core `RelationshipIdentifier` accepts both — the reservation belongs to this format's grammar, and a format with no embedded expression language has no such collision.
- Namespace identity is case-insensitive while YAML keys are not, so two block-document keys normalizing to the same namespace are rejected.

## Open

- The block push's endpoint spelling.

## Related

- [[fact-documents]] — the DML side: the document that mutates the edges a namespace document permits.
- [[schema-definition-language]] — the superseded SDL note; its envelope predates [[schema-dissolves-into-administration]].
- [[dissolve-schema-into-administration]] — the remodel these document forms come from.
- [[domain-language]] — the naming record, including the retired AGL name.
