---
title: domains
summary: "A domain is a named grouping of namespaces of relationships, each relationship a named subject-set rewrite. An entity of the model, projected to and from YAML as its wire and storage form."
tags: [spec, schema]
created: 2026-07-23
status: evolving
cites:
  - "[[identifiers]]"
  - "[[domain]]"
  - "[[namespace]]"
  - "[[relationship]]"
  - "[[subject-set-rewrite]]"
  - "[[this]]"
  - "[[computed-subject-set]]"
  - "[[fact-to-subject-set]]"
  - "[[factset]]"
---

# Domains

A [[domain]] is a named grouping of [[namespace]]s — each namespace a grouping of [[relationship]]s, each relationship a named [[subject-set-rewrite]].

A domain is an entity of the model. It enters the system as a YAML document and is later stored the same way, but the YAML is a projection of the domain, not the domain itself. This document defines the domain: the shape its projection takes, the grammar of its rewrites, and the rules a well-formed domain obeys.

## Projection

A domain projects to a YAML document with two keys. `domain:` is the domain's name; `namespaces:` maps each namespace name to its relationship list. The name travels inside the projection rather than arriving out of band, so the printer emits every part of the domain it was given and the document round-trips. A write carries a whole document and replaces the domain whole. Omit a relationship and the write removes it.

```yaml
domain: io

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

A relationship's rewrite is an expression in a scalar, parsed separately from the YAML. The parser reads the scalar's raw text rather than YAML's typed value, so a plain `null` in expression position is the name `null`, not a missing value.

Three operators, in binding order:

- `!` exclusion, binds tightest
- `&` intersection
- `|` union

Each binds tighter than the one below it, so `a | b & c` is `a | (b & c)`. This matches ordinary math: `&` binds before `|`, the way `×` binds before `+`. Each level reads left to right, so `a ! b ! c` is `(a ! b) ! c`.

EBNF conventions are given in [[identifiers]].

```ebnf
⟨subject-set-rewrite⟩  ::= ⟨union⟩
⟨union⟩                ::= ⟨intersection⟩ { '|' ⟨intersection⟩ }
⟨intersection⟩         ::= ⟨exclusion⟩ { '&' ⟨exclusion⟩ }
⟨exclusion⟩            ::= ⟨term⟩ { '!' ⟨term⟩ }
⟨term⟩                 ::= 'this'
                         | ⟨computed-subject-set⟩
                         | ⟨fact-to-subject-set⟩
                         | '(' ⟨subject-set-rewrite⟩ ')'

⟨computed-subject-set⟩ ::= ⟨relationship name⟩
⟨fact-to-subject-set⟩  ::= '(' ⟨factset relationship⟩ ',' ⟨computed-subject-set relationship⟩ ')'
⟨factset relationship⟩ ::= ⟨relationship name⟩
⟨computed-subject-set relationship⟩ ::= ⟨relationship name⟩

⟨relationship name⟩    ::= ⟨name-start⟩ { ⟨name-char⟩ }     excluding 'this'
⟨name-start⟩           ::= ⟨letter⟩ | '_'
⟨name-char⟩            ::= ⟨letter⟩ | ⟨digit⟩ | '_'
⟨letter⟩               ::= 'a'…'z' | 'A'…'Z'
⟨digit⟩                ::= '0'…'9'
```

`⟨factset relationship⟩` names the relationship whose facts the walk reads. `⟨computed-subject-set relationship⟩` is evaluated on each resource that the walk reaches.

Every name a rewrite holds is a `⟨relationship name⟩`, evaluated against the resource in hand. Its character grammar — `⟨name-start⟩` through `⟨digit⟩` — also forms the `domain:` value and the namespace keys, the `⟨domain name⟩` and `⟨namespace name⟩` productions in [[identifiers]].

A run of one operator parses to a single n-ary node. Parentheses survive as structure, so the parser never flattens across them. The printer parenthesizes by grammar position, so a [[subject-set-rewrite]] tree round-trips to a structurally equal tree.

Two constraints the grammar can't carry:

- A rewrite nests at most 100 levels deep; a run of `|` or `&` is one level however wide, so operand count is free. Grouping-parenthesis depth is bounded on its own, refused as `domain.rewrite`, and the parsed tree's height as `rewrite.depth`.
- A union or an intersection takes at least one operand. An empty one has no members to take, so it is refused rather than given semantics.

A [[computed-subject-set]] names another relationship in the same namespace. A [[fact-to-subject-set]] walks a [[factset]], then evaluates a second relationship on the resource it reaches.

## Rules

- `- name: <rewrite>` defines a relationship. `- name` alone is shorthand for `- name: this`; `- name:` with nothing after is rejected as a forgotten rewrite.
- A namespace may hold no relationships: `file:` or `file: []`.
- A namespace cannot name the same relationship twice. Names normalize to lowercase first, so `Owner` and `owner` collide.
- Every [[computed-subject-set]] and the factset half of every [[fact-to-subject-set]] must name a relationship in the same namespace, defined before or after. The computed half is unchecked: the namespace it resolves in isn't known until facts are read.
- Computed-subject-set references cannot form a cycle. Only computed edges count, so a walk may still reach its own relationship, as `folder`'s `viewer` does through `(parent, viewer)`.
- A domain defines at least one namespace; an empty `namespaces:` map is rejected.
- No relationship may be named `this`, any casing. It lexes as the direct-membership keyword, so the name could never be referenced. The core accepts it; this format reserves it.

## Reference

- [Algebra of sets](https://en.wikipedia.org/wiki/Algebra_of_sets) — the laws the operators obey, and the precedence convention they inherit.
