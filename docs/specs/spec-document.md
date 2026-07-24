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

One document defines one [[spec]]: its name, its [[namespace]]s, and their [[relationship]]s. A write replaces the spec whole. Omit a relationship and the write removes it.

## Envelope

Two keys. `spec:` is the spec's name, `namespaces:` a map of namespace name to relationship list. The name lives in the document rather than arriving out of band, so the printer can emit every part of the value it was given and a document round-trips.

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

## Names and paths

Every name in a document is bare: namespace keys, relationship names, and names inside a rewrite. `spec:` supplies the missing segment and the parser qualifies each one before the domain sees it. Under `spec: io`, `file` becomes `io/file` and its `viewer` becomes `io/file#viewer` ([[identifiers]]).

One name stays bare. A [[fact-to-subject-set]]'s second name applies to the resource the walk reaches, whose namespace depends on facts, so the evaluator qualifies it instead.

Names normalize to lowercase. YAML keys do not, so the parser rejects two keys that normalize alike.

## Rewrites

A relationship's rewrite is an expression in a scalar, parsed separately from the YAML. The parser reads the scalar's raw text rather than YAML's typed value, so a plain `null` in expression position is the name `null`, not a missing value.

Three operators, in binding order:

- `!` exclusion, binds tightest
- `&` intersection
- `|` union

Each binds tighter than the one below it, so `a | b & c` is `a | (b & c)`. Set algebra reads `A ∩ B ∪ C` the same way, on the convention that puts `×` over `+`. Each level reads left to right, so `a ! b ! c` is `(a ! b) ! c`.

EBNF, with the operators and conventions given in [[identifiers]].

```ebnf
⟨rewrite⟩              ::= ⟨union⟩
⟨union⟩                ::= ⟨intersection⟩ { '|' ⟨intersection⟩ }
⟨intersection⟩         ::= ⟨exclusion⟩ { '&' ⟨exclusion⟩ }
⟨exclusion⟩            ::= ⟨term⟩ { '!' ⟨term⟩ }
⟨term⟩                 ::= 'this'
                         | ⟨computed-subject-set⟩
                         | ⟨fact-to-subject-set⟩
                         | '(' ⟨rewrite⟩ ')'

⟨computed-subject-set⟩ ::= ⟨name⟩
⟨fact-to-subject-set⟩  ::= '(' ⟨factset name⟩ ',' ⟨computed name⟩ ')'
⟨factset name⟩         ::= ⟨name⟩
⟨computed name⟩        ::= ⟨name⟩

⟨name⟩                 ::= ⟨name-start⟩ { ⟨name-char⟩ }     excluding 'this'
⟨name-start⟩           ::= ⟨letter⟩ | '_'
⟨name-char⟩            ::= ⟨letter⟩ | ⟨digit⟩ | '_'
⟨letter⟩               ::= 'a'…'z' | 'A'…'Z'
⟨digit⟩                ::= '0'…'9'
```

`⟨factset name⟩` names the relationship whose facts the walk reads. `⟨computed name⟩` is evaluated on each resource that walk reaches.

`⟨name⟩` is the production behind `⟨spec name⟩`, `⟨namespace name⟩`, and `⟨relationship name⟩` in [[identifiers]], and it governs all four name positions in a document: the `spec:` value, the namespace keys, the relationship names, and the names inside a rewrite.

A run of one operator parses to a single n-ary node. Parentheses survive as structure, so the parser never flattens across them. The printer parenthesizes by grammar position, so a [[subject-set-rewrite]] tree round-trips to a structurally equal tree.

Two constraints the grammar can't carry:

- A rewrite tree is at most 100 levels deep. Deeper is refused as `rewrite.depth`.
- A union or an intersection takes at least one operand. An empty one has no members to take, so it is refused rather than given semantics.

A [[computed-subject-set]] names another relationship in the same namespace. A [[fact-to-subject-set]] walks a [[factset]], then evaluates a second relationship on the resource it reaches.

## Rules

- A relationship written as a bare name, `- owner`, has an implicit [[this]] rewrite.
- A relationship written as a pair with no expression, `- owner:`, is rejected. It always reads as a forgotten rewrite, so it gets a pointed error rather than a default.
- A namespace with no relationships is valid, written `file:` or `file: []`.
- A namespace may not define the same relationship name twice, before or after normalization.
- Every computed-subject-set name and every `⟨factset name⟩` must name a relationship in the same namespace, in any order. The parser doesn't check `⟨computed name⟩`, because the namespace it resolves in depends on facts.
- Computed-subject-set references must not form a cycle. The check covers computed-subject-set edges only, so a walk may reach its own relationship, as `folder`'s `viewer` does through `(parent, viewer)`.
- A spec defines at least one namespace. The absence of namespaces is the absence of a spec, so an empty `namespaces:` map is rejected.
- `this` cannot name a relationship, in any case. It lexes as the direct-membership keyword, so a relationship so named could never be referenced. The core accepts the name; the restriction is this format's.

## Reference

- [Algebra of sets](https://en.wikipedia.org/wiki/Algebra_of_sets) — the laws the operators obey, and the precedence convention they inherit.
