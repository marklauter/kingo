---
title: facts
summary: "A fact is one edge in the graph: a subject-set joined to a subject. The subject is a subject id, a subject-set, or an object-set."
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

# facts

A [[fact]] is one edge in the graph, composed of a [[subject-set]] and a [[subject]].

Subject has three shapes:

- A subject id — an opaque caller key, `10`.
- A [[subject-set]] — a resource and a relation, `io/group:eng#member`.
- An object-set — a bare [[resource]] marked `#...`, `io/folder:A#...`, the object-object edge.

A subject-set is a [[resource]] and a relation. In `io/doc:readme#viewer` the relation qualifies against the resource's own namespace, so the subject-set names the [[relationship]] `io/doc#viewer`. A resource is a [[namespace]] and a caller-supplied id: `io/doc:readme`.

Facts may span specs. In `sales/doc:readme#viewer@org/group:eng#member`, each side qualifies independently, which is how one group is defined once and referenced from anywhere.

## Grammar

Grammar conventions and the name productions (`⟨namespace⟩`, `⟨relationship name⟩`, and the separators) are defined in [[identifiers]]. This grammar builds the fact tree on top of them.

```ebnf
⟨fact⟩         ::= ⟨subject-set⟩ '@' ⟨subject⟩
⟨subject⟩      ::= ⟨subject id⟩ | ⟨subject-set⟩ | ⟨object-set⟩
⟨object-set⟩   ::= ⟨resource⟩ '#' '...'
⟨subject-set⟩  ::= ⟨resource⟩ '#' ⟨relationship name⟩
⟨resource⟩     ::= ⟨namespace⟩ ':' ⟨resource id⟩
```

`⟨resource id⟩` and `⟨subject id⟩` are the caller's — a natural key, a surrogate key, a GUID, whatever their system uses. Kingo compares them and never interprets them. The `:` separates a namespace from a resource id.

```
io/doc:readme#viewer@10                         fact, subject id
io/doc:readme#viewer@io/group:eng#member        fact, subject-set
io/folder:A#viewer@io/folder:B#...              fact, object-set
sales/doc:readme#viewer@org/group:eng#member    fact spanning two specs
```
