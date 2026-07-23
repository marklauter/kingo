---
title: spec document
summary: "The authored form of a spec: a YAML document naming one spec and defining its namespaces and their relationships, with each rewrite written as an expression in a small embedded language."
tags: [spec, schema]
created: 2026-07-23
status: evolving
cites:
  - "[[identifiers]]"
  - "[[spec]]"
  - "[[namespace]]"
  - "[[relationship]]"
  - "[[subject-set-rewrite]]"
  - "[[this]]"
  - "[[computed-subject-set]]"
  - "[[fact-to-subject-set]]"
  - "[[factset]]"
---

# spec document

A spec document is how a [[spec]] is written. One document names one spec and defines every [[namespace]] in it, so the document is the unit of authoring and the unit of atomic change. A write replaces the spec whole: relationships the document omits are removed.

The outer form is YAML, which carries the names, the map structure, comments, and indentation. Each [[relationship]]'s rewrite lives in a scalar as a small embedded language, parsed separately. An expression like `(this | editor | (parent, viewer)) ! banned` has operators, precedence, and grouping, and YAML has no way to spell those except as nested mappings.

## Envelope

A document is one YAML mapping with two keys:

- `spec:` — the spec's name.
- `namespaces:` — a map of namespace name to relationship list.

The name lives in the document rather than arriving out of band, so a document round-trips: the printer can emit every part of the value it was given.

```yaml
spec: io

namespaces:
  file:
    - owner
    - parent
    - editor: this | owner
    - viewer: >
        (this | editor | (parent, viewer)) ! banned
    - auditor: this & viewer
    - banned

  folder:
    - owner
    - parent
    - viewer: (this | (parent, viewer)) ! banned
    - banned
```

## The document holds names, the domain holds paths

Every name in a document is bare. The namespace keys are bare, and so is every relationship name and every name inside a rewrite. The document's `spec:` supplies the missing segment, and the parser qualifies each name before the domain sees it — `file` under `spec: io` becomes `io/file`, and its `viewer` becomes `io/file#viewer`. See [[identifiers]].

One position keeps its bare name. In a [[fact-to-subject-set]] the second name is evaluated against the resource the walk arrives at, whose namespace is not known until the facts are read, so it stays unqualified in the stored rewrite.

Names are case-insensitive and normalize to lowercase. YAML keys are not, so two keys that normalize to the same namespace are rejected.

## Rewrite expressions

| Operator | Meaning      | Precedence           | Associativity |
|----------|--------------|----------------------|---------------|
| `!`      | exclusion    | highest              | left          |
| `&`      | intersection | lower (same as `\|`) | left          |
| `\|`     | union        | lower (same as `&`)  | left          |

`!` binds tighter than `&` and `|`. Those two share a precedence and read left to right, so mix them with parentheses when grouping matters. Chained `!` associates left, matching set difference: `a ! b ! c` is `(a ! b) ! c`.

```bnf
<rewrite>             ::= <exclusion> [ ('&' | '|') <exclusion> ]*
<exclusion>           ::= <term> [ '!' <term> ]*
<term>                ::= 'this'
                        | <computed-subjectset>
                        | <fact-to-subjectset>
                        | '(' <rewrite> ')'

<computed-subjectset> ::= <name>
<fact-to-subjectset>  ::= '(' <name> ',' <name> ')'
<name>                ::= [a-zA-Z_][a-zA-Z0-9_]*
```

A run of the same operator is one n-ary node, so `a | b | c` is a single three-child union. A parenthesized operand is opaque, so `(a | b) | c` is a union whose first child is a union. The printer parenthesizes by grammar position, so any [[subject-set-rewrite]] tree round-trips to a structurally equal tree.

A [[computed-subject-set]] names another relationship in the same namespace. A [[fact-to-subject-set]] walks a [[factset]] relationship, then evaluates a second relationship on the resource that walk reaches.

## What the parser checks

Every computed-subject-set name and every factset first name must be defined in the same namespace, and computed-subject-set references must not form a cycle. A factset's second name is not checked, because the namespace it resolves in depends on facts.

A relationship written as a bare name has no rewrite and means [[this]]. A `<name>:` pair with a missing value is rejected rather than defaulted, since it always reads as a forgotten expression. A namespace with no relationships is valid.

`this` and `...` cannot name a relationship. `this` always lexes as the direct-membership keyword, so a relationship so named could never be referenced. `...` is the fact grammar's unspecified-relationship sentinel and cannot lex in a rewrite at all. Both are constraints of this format. A relationship path accepts either name, and a format with no embedded expression language has no such collision.
