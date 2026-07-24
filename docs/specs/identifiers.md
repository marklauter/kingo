---
title: identifiers
summary: "Every theory, namespace, and relationship is named by a fully-qualified immutable path. The path is the identity — there is no display label and no surrogate key."
tags: [spec, theory]
created: 2026-07-23
status: evolving
cites:
  - "[[theory]]"
  - "[[catalog]]"
  - "[[namespace]]"
  - "[[relationship]]"
---

# identifiers

An identifier names an entity. It is immutable, and it is the only identity the thing has. No surrogate key sits beside it. Renaming is not an operation: a different identifier is a different thing.

Three kinds of thing carry one, and each contains the one above it:

- A [[theory]] is one segment: `io`.
- A [[namespace]] is `<theory>/<namespace>`: `io/file`.
- A [[relationship]] is `<theory>/<namespace>#<relation>`: `io/file#viewer`.

Each segment is unique within the segment that precedes it. Theory names are unique across the [[catalog]], namespace names within their theory, relation names within their namespace. Composing unique segments makes the whole path unique. `io/file` is the namespace's name, and there is no namespace called `file`.

## The path is one value

An identifier is stored, compared, and sorted as a single string. Its segments are projections of that string, computed on demand rather than held beside it, so the identifier stays one value with one representation.

Storing the path whole also orders it. Every namespace in a theory is contiguous in the key space, so listing a theory's namespaces is a prefix scan.

## Notation

Each separator marks one boundary and only that boundary:

- `/` separates a theory from a namespace.
- `#` introduces a relation.

What a string names is recoverable from which separators it carries, not from counting its segments. `/` all the way down would make a relationship and a resource both three segments with nothing to tell them apart.

Grammars in this corpus are EBNF: `::=` defines, `|` alternates, `( )` groups, `{ }` repeats zero or more times, `[ ]` marks optional, quoted text is literal, `'x'…'y'` is an inclusive character range, and `⟨…⟩` names a production.

```ebnf
⟨relationship path⟩ ::= ⟨namespace path⟩ '#' ⟨relationship name⟩
⟨namespace path⟩    ::= ⟨theory name⟩ '/' ⟨namespace name⟩

⟨theory name⟩       ::= ⟨name⟩
⟨namespace name⟩    ::= ⟨name⟩
⟨relationship name⟩ ::= ⟨name⟩
⟨name⟩              ::= ⟨name-start⟩ { ⟨name-char⟩ }
⟨name-start⟩        ::= ⟨letter⟩ | '_'
⟨name-char⟩         ::= ⟨letter⟩ | ⟨digit⟩ | '_'
⟨letter⟩            ::= 'a'…'z' | 'A'…'Z'
⟨digit⟩             ::= '0'…'9'
```

`⟨theory name⟩`, `⟨namespace name⟩`, and `⟨relationship name⟩` are Kingo's own names. The three share one character grammar but stay distinct productions: each names one kind of thing, qualified by its position in the path. The [[facts]] grammar builds resources, subject-sets, and facts on top of these.

```
io                                              theory
io/file                                         namespace
io/file#viewer                                  relationship
```
