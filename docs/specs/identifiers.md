---
title: identifiers
summary: "Every spec, namespace, and relationship is named by a fully-qualified immutable path. The path is the identity — there is no display label and no surrogate key."
tags: [spec, schema]
created: 2026-07-23
status: evolving
cites:
  - "[[spec]]"
  - "[[schema]]"
  - "[[namespace]]"
  - "[[this]]"
  - "[[relationship]]"
  - "[[resource]]"
  - "[[fact]]"
  - "[[subject-set]]"
  - "[[fact-to-subject-set]]"
  - "[[computed-subject-set]]"
---

# identifiers

An identifier names a thing the way a variable name names a variable. It is immutable, and it is the only identity the thing has. No surrogate key sits beside it. Renaming is not an operation: a different identifier is a different thing.

Three kinds of thing carry one, and each contains the one above it:

- A [[spec]] is one segment: `io`.
- A [[namespace]] is `<spec>/<namespace>`: `io/file`.
- A [[relationship]] is `<spec>/<namespace>#<relation>`: `io/file#viewer`.

Each segment is unique within the segment that precedes it. Spec names are unique across the [[schema]], namespace names within their spec, relation names within their namespace. Composing unique segments makes the whole path unique. `io/file` is the namespace's name, and there is no namespace called `file`.

## The path is one value

An identifier is stored, compared, and sorted as a single string. Its segments are projections of that string, computed on demand rather than held beside it, so the identifier stays one value with one representation.

Storing the path whole also orders it. Every namespace in a spec is contiguous in the key space, so listing a spec's namespaces is a prefix scan.

## Bare names live only in source text

A bare relation name is not an identifier. It appears in an SDL document, where the surrounding envelope supplies the spec and the namespace, and the parser qualifies it before anything downstream sees it. Nothing in the domain holds an unqualified name.

One position is an exception. In a [[fact-to-subject-set]], the computed relation is applied to the resource the walk arrives at, and that resource's namespace is not known until the facts are read. The name stays bare in the stored rewrite and qualifies during evaluation. Every other position resolves against the namespace the rewrite is defined in, so the parser qualifies it: [[this]], a [[computed-subject-set]], and the factset's first element.

## Facts

A [[resource]] is `<spec>/<namespace>:<resource-id>`. A [[subject-set]] is a resource and a relation, `io/doc:readme#viewer`, and the relation qualifies against the resource's own namespace, so that subject-set names the relationship `io/doc#viewer`.

A [[fact]] may span specs. In `sales/doc:readme#viewer@org/group:eng#member`, each side qualifies independently, which is how one group is defined once and referenced from anywhere.

## What an identifier never carries

A version is a property of a thing, not part of its name. Putting a version in an identifier would make every version a different thing and strand every fact that referenced the last one.

## Notation

Each separator marks one boundary and only that boundary:

- `/` separates a spec from a namespace.
- `:` separates a namespace from a resource id.
- `#` introduces a relation.

What a string names is recoverable from which separators it carries, not from counting its segments. `/` all the way down would make a relationship and a resource both three segments with nothing to tell them apart.

```
⟨fact⟩         ::= ⟨subject-set⟩ '@' ⟨subject⟩
⟨subject⟩      ::= ⟨subject id⟩ | ⟨subject-set⟩
⟨subject-set⟩  ::= ⟨resource⟩ '#' ⟨relationship name⟩
⟨relationship⟩ ::= ⟨namespace⟩ '#' ⟨relationship name⟩
⟨resource⟩     ::= ⟨namespace⟩ ':' ⟨resource id⟩
⟨namespace⟩    ::= ⟨spec name⟩ '/' ⟨namespace name⟩
```

All five terminals are opaque strings to the grammar, but they come from two different places. `⟨spec name⟩`, `⟨namespace name⟩`, and `⟨relationship name⟩` are Kingo's own names, and the rules above govern them. `⟨resource id⟩` and `⟨subject id⟩` belong to the caller — a natural key, a surrogate key, a GUID, whatever their system uses. Kingo compares them and never interprets them.

Two things differ from Zanzibar's tuple grammar. `⟨namespace⟩` is composed rather than terminal, because it carries a spec. And `⟨relationship⟩` has no counterpart there — Zanzibar names a relation only as part of a userset, bound to an object id, while Kingo needs the unbound form as the key a rewrite is stored under.

```
io                                              spec
io/file                                         namespace
io/file#viewer                                  relationship
io/file:readme                                  resource
io/file:readme#viewer                           subject-set

io/file:readme#viewer@10                        fact, subject
io/file:readme#viewer@io/group:eng#member       fact, subject-set
sales/doc:readme#viewer@org/group:eng#member    fact spanning two specs
```
