---
title: identifiers
summary: "Every domain, namespace, and relationship is named by a fully-qualified immutable path. The path is the identity — there is no display label and no surrogate key."
tags: [spec, schema]
created: 2026-07-23
status: evolving
cites:
  - "[[domain]]"
  - "[[schema]]"
  - "[[namespace]]"
  - "[[relationship]]"
---

# identifiers

An identifier names an entity. It is immutable, and it is the only identity the thing has. No surrogate key sits beside it. Renaming is not an operation: a different identifier is a different thing.

Three kinds of thing carry one, and each contains the one above it:

- A [[domain]] is one segment: `io`.
- A [[namespace]] is `<domain>/<namespace>`: `io/file`.
- A [[relationship]] is `<domain>/<namespace>#<relation>`: `io/file#viewer`.

Each segment is unique within the segment that precedes it. Domain names are unique across the [[schema]], namespace names within their domain, relation names within their namespace. Composing unique segments makes the whole path unique. `io/file` is the namespace's name, and there is no namespace called `file`.

## The path is one value

An identifier is stored, compared, and sorted as a single string. Its segments are projections of that string, computed on demand rather than held beside it, so the identifier stays one value with one representation.

Storing the path whole also orders it. Every namespace in a domain is contiguous in the key space, so listing a domain's namespaces is a prefix scan.

## Notation

Each separator marks one boundary and only that boundary:

- `/` separates a domain from a namespace.
- `#` introduces a relation.

What a string names is recoverable from which separators it carries, not from counting its segments. `/` all the way down would make a relationship and a resource both three segments with nothing to tell them apart.

Grammars in this corpus are EBNF: `::=` defines, `|` alternates, `( )` groups, `{ }` repeats zero or more times, `[ ]` marks optional, quoted text is literal, `'x'…'y'` is an inclusive character range, and `⟨…⟩` names a production.

```ebnf
⟨relationship path⟩ ::= ⟨namespace path⟩ '#' ⟨relationship name⟩
⟨namespace path⟩    ::= ⟨spec name⟩ '/' ⟨namespace name⟩
```

`⟨spec name⟩`, `⟨namespace name⟩`, and `⟨relationship name⟩` are Kingo's own names — opaque segments here, their character grammar the `⟨name⟩` production of [[domains]]. These two productions say only how the segments compose into a qualified path. The [[facts]] grammar builds resources, subject-sets, and facts on top of them.

```
io                                              domain
io/file                                         namespace
io/file#viewer                                  relationship
```
