---
title: Quarries — prior work preserved on other branches
summary: "The reboot branch is an orphan; main-archive and dictionary-encoding remain on the remote as reference material. Cherry-pick files as needed; do not merge."
tags: [note, reference, quarry]
created: 2026-05-12
status: locked
---

# Quarries — prior work preserved on other branches

The `reboot` branch is an orphan with no shared history. Two earlier lines of work remain on the remote as reference material. Cherry-pick files from them as needed; do not try to merge.

## `main-archive`

A safety-net copy of `main` taken before the reboot.

- Original in-memory ReBAC proof-of-concept.
- `AclStore` class with hand-rolled `Map<Key, SubjectMap>` index, where each cell holds two HashSets (`subjects`, `SubjectSets`).
- Zanzibar `CHECK` evaluator with the full set of rewrite cases (`This`, `ComputedSubjectSetRewrite`, `TupleToSubjectSetRewrite`, union, intersection, exclusion).
- Built on LanguageExt's persistent `Map<,>` for structural sharing.

Notable files:

- `src/Kingo.Acl/AclReader.cs`
- `src/Kingo.Storage/DocumentWriter.cs` — lock-free CAS over persistent maps
- `src/Kingo/` — domain primitives (`Subject`, `SubjectSet`, `Relationship`, etc.)

## `dictionary-encoding`

An ambitious refactor that bundled several efforts. Builds clean apart from `src/dead-code/`.

- SQLite persistence (`src/Kingo.Storage/Sqlite/*`, `src/Kingo.Storage/Context/*`) — header/journal table layout, MVCC, integration tests. ~840 lines of test coverage.
- YAML PDL parser (`src/Kingo.Pdl/*`) — YamlDotNet outer parser, Superpower embedded grammar for rewrite expressions, round-tripping serializer.
- Partial bit-packing key encoder (`src/Kingo.DictionaryEncoding/KeyEncoder.cs`) — scaffold only; `Pack`/`Unpack` math sits commented-out, `Encode` is a stub.
- LanguageExt removed throughout (decision: FP wrappers don't earn their keep in IO-bound code).
- `src/dead-code/` — abandoned in-memory and earlier SQLite drafts, does not build.

Notable files:

- `src/Kingo.Pdl/PdlParser.cs`, `PdlSerializer.cs`, `RewriteExpressionParser.cs`
- `src/Kingo.Pdl.Tests/Data/doc.policy.yml` — canonical example (also reproduced in [[specs]])
- `src/Kingo.Storage/Sqlite/SqliteDocumentReader.D.cs`, `SqliteDocumentWriter.D.cs`, `SqliteSequence.N.cs`

## How to lift files

Read a file without checkout:

```
git show dictionary-encoding:src/Kingo.Pdl/PdlParser.cs
```

Copy a file into the working tree:

```
git checkout dictionary-encoding -- src/Kingo.Pdl/PdlParser.cs
```

Both operations leave the quarry branch untouched. Cherry-pick judiciously — most quarry code still uses LanguageExt and predates the current build conventions, so expect to adapt as you carry pieces across.
