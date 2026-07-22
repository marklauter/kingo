---
title: SDL — YAML structure with embedded rewrite expressions
summary: "The Schema Definition Language: a YAML document carrying the schema's name and its namespace map, with each relationship's optional rewrite expression as a small embedded language parsed separately. Implemented by the Kingo.Sdl adapter."
aliases: [pdl-yaml, sdl-yaml]
tags: [note, spec, sdl, yaml]
created: 2026-05-12
status: superseded
superseded-by: "[[agl]]"
---

# SDL — YAML structure with embedded rewrite expressions

> Superseded 2026-07-22 by [[agl]]. The rewrite-expression grammar, precedence, and reserved words folded into that spec unchanged; the document envelope below (`schema:`/`namespaces:` keys, `SchemaIdentifier`, `sdl.document`/`schema_id.*` errors) predates [[schema-dissolves-into-administration]] and is stale. Kept for the prior-art record until the corpus reframe lands ([[dissolve-schema-into-administration]]).

The Schema Definition Language names a schema and defines its namespaces and their relationships ([[domain-language]]: the schema-side record is `Relationship`, the algebra is `SubjectSetRewrite`). SDL is one of the two sublanguages of the **Authorization Graph Language (AGL)** — Kingo's counterpart to SQL, named in [[domain-language]]. SDL is the DDL half, defining what edges may exist; FML, the Fact Mutation Language, is the DML half that mutates the edges that do ([[graph-document-is-bulk-dml]]). The outer structure is YAML; each relationship's optional **rewrite expression** is a small embedded language parsed with [Superpower](https://github.com/datalust/superpower). Implemented by `Kingo.Sdl` per [[dissolve-kingo-pdl-under-hexagonal-layout]] — parse errors accumulate as `Result` validation failures, the transform exits through `Namespace.Create` and `Schema.Create`, and the serializer's newline is pinned to `\n`.

This split is deliberate. YAML carries the name, the namespace map, comments, indentation, and editor tooling. The rewrite expression — e.g. `(this | editor | (parent, viewer)) ! banned` — would be awkward to encode in pure YAML, so it lives in a string and gets parsed separately.

## Document envelope

A document is a single YAML mapping with exactly two keys (settled 2026-07-15, with `SchemaIdentifier`):

- `schema:` — the schema's name, and its **domain key** (name-as-identity, provisional — see [[domain-language]] open items). Same grammar as a namespace name: `^[A-Za-z_][A-Za-z0-9_]*$`, case-insensitive, normalized to lowercase. Both are authored vocabulary, so they share a rule; a resource id is client-minted and does not.
- `namespaces:` — the namespace map.

The name lives **in the document** rather than arriving as a parse argument, which is what keeps `parse ∘ print = id` covering the schema whole — the printer can emit every part of the value it was given. Missing or misshapen keys are `sdl.document`; a name that breaks the grammar is `schema_id.empty` / `schema_id.invalid`, since `SchemaIdentifier` owns it.

## Operator precedence

Inside a rewrite expression:

| Operator | Meaning      | Precedence            | Associativity |
|----------|--------------|-----------------------|---------------|
| `!`      | exclusion    | highest               | left          |
| `&`      | intersection | lower (same as `\|`)  | left          |
| `\|`     | union        | lower (same as `&`)   | left          |

`!` binds tighter than `&` / `|`. `&` and `|` share precedence and are read left-to-right; mix them with parentheses if grouping matters. Chained `!` associates left, matching mathematical set difference: `a ! b ! c` is `(a ! b) ! c`.

A structural note the adapter honors: a run of consecutive same-operator applications is one n-ary node (`a | b | c` is a single three-child union), and a parenthesized operand is opaque — `(a | b) | c` is a union whose first child is a union. The printer parenthesizes by grammar position, so any `SubjectSetRewrite` tree round-trips to a structurally equal tree.

## Example

```yaml
# rewrite set operators:
#   ! = exclusion operator
#   & = intersection operator
#   | = union operator

schema: io                      # the schema's name, and its domain key

namespaces:
  file:                           # namespace
    - owner                       # empty relationship - implicit this
    - parent                      # the factset relationship the viewer rewrite walks
    - editor: this | owner        # relationship with union rewrite
    - viewer: >                   # relationship with union, factset, and exclusion rewrites
        (this | editor | (parent, viewer)) ! banned
    - auditor: this & viewer      # relationship with intersection rewrite
    - banned                      # empty relationship - implicit this

  # second namespace defined within same document
  folder:
    - owner
    - parent
    - viewer: (this | (parent, viewer)) ! banned
    - banned
```

A bare relationship name (e.g. `owner`, `banned`) has no rewrite — semantically equivalent to `this`. A namespace with no relationships (`file:` alone, or `file: []`) is valid. Identifiers are case-insensitive and normalize to lowercase — that is the core's `Parse` rule, not the adapter's.

The bare name is the *only* spelling of "no rewrite": a `<name>:` pair with a missing value is rejected (`sdl.relationship`) rather than defaulted, since it always reads as a forgotten expression. And in expression position SDL owns the scalar's raw text, not YAML's typing — a plain `null` is the identifier `null` (a legal relationship name), which is also what keeps that name round-tripping, because the serializer emits it unquoted.

## Rewrite grammar (BNF)

```bnf
<rewrite>             ::= <exclusion> [ ('&' | '|') <exclusion> ]*
<exclusion>           ::= <term> [ '!' <term> ]*
<term>                ::= 'this'
                        | <computed-subjectset>
                        | <fact-to-subjectset>
                        | '(' <rewrite> ')'

<computed-subjectset> ::= <identifier>
<fact-to-subjectset> ::= '(' <identifier> ',' <identifier> ')'
<identifier>          ::= [a-zA-Z_][a-zA-Z0-9_]*
```

`computed-subjectset` references another relationship in the same namespace. `fact-to-subjectset` walks a factset relationship (first identifier) and then evaluates a second relationship on the resulting resource — Zanzibar's mechanism for inherited permissions (e.g. "viewer on folder grants viewer on file via parent"). Every computed-subjectset target and every factset first identifier must name a relationship defined in the namespace, and computed-subjectset references must not form a cycle. These are `Namespace.Create` invariants ([[namespace-create-validation]]), so a parse surfaces them as `namespace.dangling_reference` / `namespace.rewrite_cycle`. A factset's second identifier resolves in the namespaces of the resources the facts name, so it is not checked here.

**Reserved words (SDL-level, not core):** `this` and `...` cannot name a relationship in SDL. `this` always lexes as the direct-membership keyword — a relationship so named could never be referenced, and a reference would silently mean direct membership — and `...` (the fact grammar's unspecified-relationship sentinel) cannot lex in a rewrite expression at all. The adapter rejects documents defining them (`sdl.relationship.reserved`) and throws on serializing schemas that use them (a document invariant the domain cannot express — caller's defect). These are facts about *this format's grammar*; the core `RelationshipIdentifier` accepts both, and a format without an embedded expression language (JSON) has no such collision. Relatedly: namespace identity is case-insensitive while YAML keys are not, so two keys normalizing to the same namespace are rejected (`schema.duplicate_namespace`, via `Schema.Create`).

## Prior art

The quarry implementation (`src/Kingo.Pdl/` on the archive branches, see [[sources]]) proved the YamlDotNet + Superpower split and was salvaged as reference for the adapter. It threw `PdlParseException` and flattened parenthesized operands on reparse; both are fixed in `Kingo.Sdl`.
