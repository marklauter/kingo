# PDL — YAML structure with embedded rewrite expressions

> **Status (2026-07-14):** this note still speaks in quarry-era terms (`Relation`, `src/Kingo.Pdl`). The drift is deliberate — the terminology pass happens when the `Kingo.Serialization.Pdl` adapter work starts, since the format itself may evolve then too. Current domain vocabulary lives in [domain-language](domain-language.md): the policy record is `Relationship`, the algebra is `SubjectSetRewrite`, the stored fact is `Statement`.

The Policy Definition Language defines namespaces and their relations. The outer structure is YAML; each relation's optional **rewrite expression** is a small embedded language parsed with [Superpower](https://github.com/datalust/superpower).

This split is deliberate. YAML carries the namespace map, comments, indentation, and editor tooling. The rewrite expression — e.g. `(this | editor | (parent, viewer)) ! banned` — would be awkward to encode in pure YAML, so it lives in a string and gets parsed separately.

## Operator precedence

Inside a rewrite expression:

| Operator | Meaning      | Precedence            | Associativity |
|----------|--------------|-----------------------|---------------|
| `!`      | exclusion    | highest               | non-assoc     |
| `&`      | intersection | lower (same as `\|`)  | left          |
| `\|`     | union        | lower (same as `&`)   | left          |

`!` binds tighter than `&` / `|`. `&` and `|` share precedence and are read left-to-right; mix them with parentheses if grouping matters.

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

# second policy defined within same document
folder:
  - owner
  - viewer: (this | (parent, viewer)) ! banned
  - banned
```

A bare relation name (e.g. `owner`, `banned`) has no rewrite — semantically equivalent to `this`.

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

`computed-subjectset` references another relation in the same namespace. `tuple-to-subjectset` walks a tuple relation (first identifier) and then evaluates a second relation on the resulting subject — Zanzibar's mechanism for inherited permissions (e.g. "viewer on folder grants viewer on file via parent").

## Prior art

The dictionary-encoding quarry branch contains a working implementation (`src/Kingo.Pdl/`) using YamlDotNet for the outer parser and Superpower for the embedded grammar, plus a round-tripping serializer. See `docs/notes/sources.md`.
