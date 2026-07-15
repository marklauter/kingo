---
type: note
title: SDL — YAML structure with embedded rewrite expressions
summary: "The Schema Definition Language: a YAML outer structure carrying the namespace map, with each relationship's optional rewrite expression as a small embedded language parsed separately. Implemented by the Kingo.Sdl adapter."
aliases: [pdl-yaml]
tags: [note, spec, sdl, yaml]
created: 2026-05-12
status: evolving
---

# SDL — YAML structure with embedded rewrite expressions

The Schema Definition Language defines namespaces and their relationships ([[domain-language]]: the schema-side record is `Relationship`, the algebra is `SubjectSetRewrite`). The outer structure is YAML; each relationship's optional **rewrite expression** is a small embedded language parsed with [Superpower](https://github.com/datalust/superpower). Implemented by `Kingo.Sdl` per [[dissolve-kingo-pdl-under-hexagonal-layout]] — parse errors accumulate as `Result` validation failures, the transform exits through `Namespace.Create`, and the serializer's newline is pinned to `\n`.

This split is deliberate. YAML carries the namespace map, comments, indentation, and editor tooling. The rewrite expression — e.g. `(this | editor | (parent, viewer)) ! banned` — would be awkward to encode in pure YAML, so it lives in a string and gets parsed separately.

## Operator precedence

Inside a rewrite expression:

| Operator | Meaning      | Precedence            | Associativity |
|----------|--------------|-----------------------|---------------|
| `!`      | exclusion    | highest               | left          |
| `&`      | intersection | lower (same as `\|`)  | left          |
| `\|`     | union        | lower (same as `&`)   | left          |

`!` binds tighter than `&` / `|`. `&` and `|` share precedence and are read left-to-right; mix them with parentheses if grouping matters. Chained `!` associates left, matching mathematical set difference: `a ! b ! c` is `(a ! b) ! c`.

A structural note the adapter honors: a run of consecutive same-operator applications is one n-ary node (`a | b | c` is a single three-child union), and a parenthesized operand is opaque — `(a | b) | c` is a union whose first child is a union. The renderer parenthesizes by grammar position, so any `SubjectSetRewrite` tree round-trips to a structurally equal tree.

## Example

```yaml
# rewrite set operators:
#   ! = exclusion operator
#   & = intersection operator
#   | = union operator

file:                           # namespace
  - owner                       # empty relationship - implicit this
  - editor: this | owner        # relationship with union rewrite
  - viewer: >                   # relationship with union, tupleset, and exclusion rewrites
      (this | editor | (parent, viewer)) ! banned
  - auditor: this & viewer      # relationship with intersection rewrite
  - banned                      # empty relationship - implicit this

# second namespace defined within same document
folder:
  - owner
  - viewer: (this | (parent, viewer)) ! banned
  - banned
```

A bare relationship name (e.g. `owner`, `banned`) has no rewrite — semantically equivalent to `this`. A namespace with no relationships (`file:` alone, or `file: []`) is valid. Identifiers are case-insensitive and normalize to lowercase — that is the core's `Parse` rule, not the adapter's.

## Rewrite grammar (BNF)

```bnf
<rewrite>             ::= <exclusion> [ ('&' | '|') <exclusion> ]*
<exclusion>           ::= <term> [ '!' <term> ]*
<term>                ::= 'this'
                        | <computed-subjectset>
                        | <tuple-to-subjectset>
                        | '(' <rewrite> ')'

<computed-subjectset> ::= <identifier>
<tuple-to-subjectset> ::= '(' <identifier> ',' <identifier> ')'
<identifier>          ::= [a-zA-Z_][a-zA-Z0-9_]*
```

`computed-subjectset` references another relationship in the same namespace. `tuple-to-subjectset` walks a tupleset relationship (first identifier) and then evaluates a second relationship on the resulting subject — Zanzibar's mechanism for inherited permissions (e.g. "viewer on folder grants viewer on file via parent").

**Reserved words (SDL-level, not core):** `this` and `...` cannot name a relationship in SDL. `this` always lexes as the direct-membership keyword — a relationship so named could never be referenced, and a reference would silently mean direct membership — and `...` (the tuple grammar's unspecified-relationship sentinel) cannot lex in a rewrite expression at all. The adapter rejects documents defining them (`sdl.relationship.reserved`) and throws on serializing schemas that use them (a document invariant the domain cannot express — caller's defect). These are facts about *this format's grammar*; the core `RelationshipIdentifier` accepts both, and a format without an embedded expression language (JSON) has no such collision. Relatedly: namespace identity is case-insensitive while YAML keys are not, so two keys normalizing to the same namespace are rejected (`schema.duplicate_namespace`, via `Schema.Create`).

## Prior art

The quarry implementation (`src/Kingo.Pdl/` on the archive branches, see [[sources]]) proved the YamlDotNet + Superpower split and was salvaged as reference for the adapter. It threw `PdlParseException` and flattened parenthesized operands on reparse; both are fixed in `Kingo.Sdl`.
