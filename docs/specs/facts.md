---
title: facts
summary: "A fact is one edge in the authorization graph: a subject-set joined to a subject. The subject is a subject id, a subject-set, or an object-set."
tags: [spec, graphs]
created: 2026-07-24
status: evolving
cites:
  - "[[fact]]"
  - "[[subject-set]]"
  - "[[subject]]"
  - "[[resource]]"
  - "[[relationship]]"
  - "[[namespace]]"
---

# Facts

A [[fact]] is one edge in the authorization graph, composed of a [[subject-set]] and a [[subject]]. A fact is a value built from typed parts; the strings in this document are notation for a fact, not the fact itself.

Subject has three shapes:

- A subject id — an opaque key the user owns, `10`.
- A [[subject-set]] — a resource and a relationship name, `io/group:eng#member`.
- An object-set — a bare [[resource]] marked `#...`, `io/folder:A#...`, the object-object edge.

A subject-set is a [[resource]] and a relationship name. In `io/doc:readme#viewer` the relationship name qualifies against the resource's own namespace, so the subject-set names the [[relationship]] `io/doc#viewer`. A resource is a [[namespace]] and a user-supplied id: `io/doc:readme`.

Facts may span domains. In `sales/doc:readme#viewer@org/group:eng#member`, each side qualifies independently, which is how one group is defined once and referenced from anywhere.

## Grammar

EBNF conventions are given in [[identifiers]]. Kingo's names are spelled in full below, down to `⟨letter⟩` and `⟨digit⟩`. The ids are owned by the user, opaque terminals this grammar leaves undefined.

```ebnf
⟨fact⟩              ::= ⟨subject-set⟩ '@' ⟨subject⟩
⟨subject⟩           ::= ⟨subject id⟩ | ⟨subject-set⟩ | ⟨object-set⟩
⟨object-set⟩        ::= ⟨resource⟩ '#' '...'
⟨subject-set⟩       ::= ⟨resource⟩ '#' ⟨relationship name⟩
⟨resource⟩          ::= ⟨namespace path⟩ ':' ⟨resource id⟩
⟨namespace path⟩    ::= ⟨spec name⟩ '/' ⟨namespace name⟩

⟨spec name⟩         ::= ⟨name⟩
⟨namespace name⟩    ::= ⟨name⟩
⟨relationship name⟩ ::= ⟨name⟩
⟨name⟩              ::= ⟨name-start⟩ { ⟨name-char⟩ }
⟨name-start⟩        ::= ⟨letter⟩ | '_'
⟨name-char⟩         ::= ⟨letter⟩ | ⟨digit⟩ | '_'
⟨letter⟩            ::= 'a'…'z' | 'A'…'Z'
⟨digit⟩             ::= '0'…'9'
```

`⟨resource id⟩` and `⟨subject id⟩` are owned by the user — a natural key, a surrogate key, a GUID, whatever the user's system uses. Kingo compares them and never interprets them. The `:` separates a namespace from a resource id.

```
io/doc:readme#viewer@10                         fact, subject id
io/doc:readme#viewer@io/group:eng#member        fact, subject-set
io/folder:A#viewer@io/folder:B#...              fact, object-set
sales/doc:readme#viewer@org/group:eng#member    fact spanning two domains
```
