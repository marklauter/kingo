---
title: Move inline wikilinks into frontmatter edges
type: todo
summary: "Body prose reads as raw markdown, so inline `[[wikilinks]]` are noise. Sweep them out of docs/, promoting each to a typed frontmatter predicate; journal entries are immutable and keep theirs."
tags: [docs]
created: 2026-07-31
priority: low
status: open
---

# Move inline wikilinks into frontmatter edges

The corpus is read as raw markdown rather than through Obsidian, and inline `[[wikilinks]]` clutter the sentences they sit in. Every document edge moves to frontmatter, where the key is the predicate.

`references/expressing-edges.md` gives the body and the frontmatter as two places to express an edge and prefers neither, so the grammar permits this. The `hoplite-skills:spec` skill does not: it instructs wikilinking every glossary term the concept composes, at first use. This convention overrides that instruction, the same shape as the CA1062 override in CLAUDE.md.

Frontmatter buys a second thing besides the reading. An edge under a key has to be named on the way out, where a body link takes the `links-to` default.

## The shape of done

No `[[...]]` in body prose under `docs/`, every edge it carried present in frontmatter under a predicate that says what it is.

- Not a bracket-strip. A body link is often the only edge to its target, so each one is promoted to a frontmatter key first and deleted second. Dropping one loses the edge.
- The predicate comes from the sentence. Where the prose says refines, supersedes, contradicts, or supports, that word becomes the key. `cites` covers the rest.
- Journal entries under `docs/journal/` are immutable and are left alone, inline links included.
- `docs/todos/rewrite-interpreters.md` is the largest single job at 23 links, then `docs/specs/closures.md` at 20 and `docs/specs/fact-reader-port.md` at 17. Those counts include frontmatter that is already correct.

One case is already converted and stands as the pattern: `docs/specs/identifiers.md` and `docs/specs/closures.md` each lost a body link to `catalog` whose target was already in `cites`, leaving the word plain in the sentence.

## Qualified targets

`catalog` is the corpus's only colliding slug — `docs/glossary/catalog.md` and `docs/specs/catalog.md` — so a bare `[[catalog]]` has two targets. Both are qualified by the shortest unique path, `[[glossary/catalog]]` and `[[specs/catalog]]`, which the sweep preserves. Every other slug is unique and stays bare.
