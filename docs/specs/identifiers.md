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

An identifier names a thing the way a variable name names a variable. It is immutable, it is the only identity the thing has, and there is no surrogate key beside it. Renaming is not an operation: a different identifier is a different thing.

Three kinds of thing carry one, and each contains the one above it:

- A [[spec]] is one segment: `io`.
- A [[namespace]] is `<spec>/<namespace>`: `io/file`.
- A [[relationship]] is `<spec>/<namespace>/<relation>`: `io/file/viewer`.

Each segment is unique within the segment that precedes it. Spec names are unique across the [[schema]], namespace names within their spec, relation names within their namespace. Composing unique segments makes the whole path unique, so `io/file` is the namespace's name — not a qualified form of a shorter name. There is no namespace called `file`.

## The path is one value

An identifier is stored, compared, and sorted as a single string. Its segments are derived from it by projection, not held beside it, so the parts of a path are computed on demand and the whole remains one value.

Storing the path whole also orders it. Every namespace in a spec is contiguous in the key space, so listing a spec's namespaces is a prefix scan.

## Bare names live only in source text

A bare relation name is not an identifier. It appears in an SDL document, where the surrounding envelope supplies the spec and the namespace, and the parser qualifies it before anything downstream sees it. Nothing in the domain holds an unqualified name.

One position is an exception. In a [[fact-to-subject-set]], the computed relation is applied to the resource the walk arrives at, and that resource's namespace is not known until the facts are read. The name stays bare in the stored rewrite and qualifies during evaluation. Every other position — [[this]], a [[computed-subject-set]], and the factset's first element — resolves against the namespace the rewrite is defined in, so the parser qualifies it.

## Facts

A [[resource]] is `<spec>/<namespace>:<resource-id>`. A [[subject-set]] is a resource and a relation, `io/doc:readme#viewer`, and the relation qualifies against the resource's own namespace — so that subject-set names the relationship `io/doc/viewer`.

A [[fact]] may span specs. In `sales/doc:readme#viewer@org/group:eng#member`, each side qualifies independently, which is how one group is defined once and referenced from anywhere.

## What an identifier never carries

A version is a property of a thing, not part of its name. Putting a version in an identifier would make every version a different thing and strand every fact that referenced the last one.

The separators here are provisional. Whatever they become, parsing must recover what kind of thing a string names, not only how many segments it has.
