---
title: theories
summary: "A theory is a named grouping of namespaces of relations, each relation a named rewrite. An entity of the model, projected to and from YAML as its wire and storage form."
tags: [spec, theory]
created: 2026-07-23
status: evolving
cites:
  - "[[identifiers]]"
  - "[[theory]]"
  - "[[fact]]"
  - "[[namespace]]"
  - "[[relation]]"
  - "[[rewrite]]"
  - "[[subjectset]]"
  - "[[factset]]"
---

# Theories

A [[theory]] is a named grouping of [[namespace]]s — each namespace a grouping of [[relation]]s, each relation a named [[rewrite]].

A theory is intensional: its rewrites define [[subjectset]]s from other subjectsets, deriving memberships rather than recording them. Recorded memberships are [[fact]]s, the extensional half. A membership question is answered by reading the facts through the theory.

A theory is an entity of the model. It enters the system as a YAML document and is later stored the same way, but the YAML is a projection of the theory, not the theory itself. This document defines the theory: the shape its projection takes, the grammar of its rewrites, and the rules a well-formed theory obeys.

## Projection

A theory projects to a YAML document with two keys. `theory:` is the theory's name; `namespaces:` maps each namespace name to its relation list. The name travels inside the projection rather than arriving out of band, so the printer emits every part of the theory it was given and the document round-trips. A write carries a whole document and replaces the theory whole. Omit a relation and the write removes it.

```yaml
theory: io

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

## Identifiers (names)

Identifiers normalize to lowercase. YAML keys do not, so the parser rejects two keys that normalize alike.

## Rewrite grammar

A relation's rewrite is an expression in a scalar, parsed separately from the YAML. The parser reads the scalar's raw text rather than YAML's typed value, so a plain `null` in expression position is the name `null`, not a missing value.

Three operators, in binding order:

- `!` exclusion, binds tightest
- `&` intersection
- `|` union

Each binds tighter than the one below it, so `a | b & c` is `a | (b & c)`. This matches ordinary math: `&` binds before `|`, the way `×` binds before `+`. Each level reads left to right, so `a ! b ! c` is `(a ! b) ! c`.

EBNF conventions are given in [[identifiers]].

```ebnf
⟨rewrite⟩             ::= ⟨union⟩
⟨union⟩               ::= ⟨intersection⟩ { '|' ⟨intersection⟩ }
⟨intersection⟩        ::= ⟨exclusion⟩ { '&' ⟨exclusion⟩ }
⟨exclusion⟩           ::= ⟨term⟩ { '!' ⟨term⟩ }
⟨term⟩                ::= 'this'
                        | ⟨computed-subjectset⟩
                        | ⟨fact-to-subjectset⟩
                        | '(' ⟨rewrite⟩ ')'

⟨computed-subjectset⟩ ::= ⟨relation name⟩
⟨fact-to-subjectset⟩  ::= '(' ⟨factset⟩ ',' ⟨computed-subjectset⟩ ')'
⟨factset⟩             ::= ⟨relation name⟩

⟨relation name⟩       ::= ⟨name⟩     excluding 'this'
⟨name⟩                ::= ⟨name-start⟩ { ⟨name-char⟩ }
⟨name-start⟩          ::= ⟨letter⟩ | '_'
⟨name-char⟩           ::= ⟨letter⟩ | ⟨digit⟩ | '_'
⟨letter⟩              ::= 'a'…'z' | 'A'…'Z'
⟨digit⟩               ::= '0'…'9'
```

`⟨factset⟩` names the relation whose facts the walk reads. `⟨computed-subjectset⟩` is evaluated on each resource that the walk reaches.

Every name a rewrite holds is a `⟨relation name⟩`, evaluated against the resource in hand. Its character grammar — `⟨name-start⟩` through `⟨digit⟩` — also forms the `theory:` value and the namespace keys, the `⟨theory name⟩` and `⟨namespace name⟩` productions in [[identifiers]].

A run of one operator parses to a single n-ary node. Parentheses survive as structure, so the parser never flattens across them. The printer parenthesizes by grammar position, so a [[rewrite]] tree round-trips to a structurally equal tree.

Two constraints the grammar can't carry:

- A rewrite nests at most 100 levels deep; a run of `|` or `&` is one level however wide, so operand count is free. Grouping-parenthesis depth is bounded on its own, refused as `theory.rewrite`, and the parsed tree's height as `rewrite.depth`.
- A union or an intersection takes at least one operand. An empty one has no members to take, so it is refused rather than given semantics.

A computed-subjectset names another relation in the same namespace. A fact-to-subjectset walks a [[factset]], then evaluates a second relation on the resource it reaches.

## Rules

Several of these rules exist to make a theory determinate — so that the theory and the facts settle every membership question one way. Definitions must resolve (every computed-subjectset and the factset half of every fact-to-subjectset names a relation that exists) and must not circle back on themselves, so no derived set is left without a settled membership. Depth is bounded and empty operators are refused, so every rewrite's meaning stays finite and definite. That determinacy is what earns the word theory: exactly one answer per membership question.

- `- name: <rewrite>` defines a relation. `- name` alone is shorthand for `- name: this`; `- name:` with nothing after is rejected as a forgotten rewrite.
- A namespace may hold no relations: `file:` or `file: []`.
- A namespace cannot name the same relation twice. Names normalize to lowercase first, so `Owner` and `owner` collide.
- Every computed-subjectset and the factset half of every fact-to-subjectset must name a relation in the same namespace, defined before or after. The computed half is unchecked: the namespace it resolves in isn't known until facts are read.
- Computed-subjectset references cannot form a cycle. Only computed edges count, so a walk may still reach its own relation, as `folder`'s `viewer` does through `(parent, viewer)`.
- A theory defines at least one namespace; an empty `namespaces:` map is rejected.
- No relation may be named `this`, any casing. It lexes as the direct-membership keyword, so the name could never be referenced. The core accepts it; this format reserves it.

## Reference

- [Algebra of sets](https://en.wikipedia.org/wiki/Algebra_of_sets) — the laws the operators obey, and the precedence convention they inherit.
