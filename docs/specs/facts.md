---
title: facts
type: specification
summary: "A fact is one edge in the authorization graph: a subjectset joined to a subject. The subject is an identity, a subjectset, or a resource member."
tags: [graphs]
created: 2026-07-24
status: evolving
cites:
  - "[[fact]]"
  - "[[theory]]"
  - "[[subjectset]]"
  - "[[subject]]"
  - "[[identity]]"
  - "[[resource]]"
  - "[[relation]]"
  - "[[namespace]]"
---

# Facts

A [[fact]] is one edge in the authorization graph, composed of a [[subjectset]] and a [[subject]]. A fact is a value built from typed parts; the strings in this document are notation for a fact, not the fact itself.

Facts are the extensional half of the model: memberships recorded outright. A [[theory]]'s rewrites are the intensional half, deriving further memberships from the facts. A membership question is answered by reading the facts through a theory.

Subject has three shapes:

- An identity — an opaque key the user owns, `10`.
- A [[subjectset]] — a resource and a relation name, `io/group:eng#member`.
- A resource member — a bare [[resource]] marked `#...`, `io/folder:A#...`, the resource-to-resource edge.

A subjectset is a [[resource]] and a relation name. In `io/doc:readme#viewer` the relation name qualifies against the resource's own namespace, so the subjectset names the [[relation]] `io/doc#viewer`. A resource is a [[namespace]] and a user-supplied id: `io/doc:readme`.

Facts may span theories. In `sales/doc:readme#viewer@org/group:eng#member`, each side qualifies independently, which is how one group is defined once and referenced from anywhere.

## Grammar

EBNF conventions are given in [[identifiers]]. Kingo's names are spelled in full below, down to `⟨letter⟩` and `⟨digit⟩`. The ids are owned by the user, opaque terminals this grammar leaves undefined.

```ebnf
⟨fact⟩            ::= ⟨subjectset⟩ '@' ⟨subject⟩
⟨subject⟩         ::= ⟨identity⟩ | ⟨subjectset⟩ | ⟨resource member⟩
⟨resource member⟩ ::= ⟨resource⟩ '#' '...'
⟨subjectset⟩      ::= ⟨resource⟩ '#' ⟨relation name⟩
⟨resource⟩        ::= ⟨namespace path⟩ ':' ⟨resource id⟩
⟨namespace path⟩  ::= ⟨theory name⟩ '/' ⟨namespace name⟩

⟨theory name⟩     ::= ⟨name⟩
⟨namespace name⟩  ::= ⟨name⟩
⟨relation name⟩   ::= ⟨name⟩
⟨name⟩            ::= ⟨name-start⟩ { ⟨name-char⟩ }
⟨name-start⟩      ::= ⟨letter⟩ | '_'
⟨name-char⟩       ::= ⟨letter⟩ | ⟨digit⟩ | '_'
⟨letter⟩          ::= 'a'…'z' | 'A'…'Z'
⟨digit⟩           ::= '0'…'9'
```

`⟨resource id⟩` and `⟨identity⟩` are owned by the user — a natural key, a surrogate key, a GUID, whatever the user's system uses. Kingo compares them and never interprets them. The `:` separates a namespace from a resource id.

```
io/doc:readme#viewer@10                         fact, identity
io/doc:readme#viewer@io/group:eng#member        fact, subjectset
io/folder:A#viewer@io/folder:B#...              fact, resource member
sales/doc:readme#viewer@org/group:eng#member    fact spanning two theories
```
