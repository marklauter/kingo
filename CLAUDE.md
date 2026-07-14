# Kingo

Kingo is a Google Zanzibar-inspired ReBAC authorization system, exploring relationship-based access control with policy rewrite rules.

## House rules

Load the `csharp:writing-csharp` skill before writing code or performing a code review.

One repo-level override to that skill: the internal domain and libraries take **no runtime null checks, ever** — nullable reference annotations are the contract, and CA1062 is disabled repo-wide (`Directory.Build.props` `<NoWarn>`). The skill's `ArgumentNullException.ThrowIfNull` rule applies only at the API edges (the service hosts, where callers are uncontrolled); those edges are 100% guarded, so the domain behind them is already protected and never needs null validation.

## Docs and notes

Design docs live under `docs/`; working notes, decisions, and todos live in `docs/notes/`, indexed by `docs/notes/index.md`.

Notes follow the Open Knowledge Format (OKF v0.1, spec: https://github.com/GoogleCloudPlatform/knowledge-catalog/tree/main/okf): a directory of markdown files with YAML frontmatter, where the file path is the concept's identity, `type` is the only required key, and consumers tolerate unknown keys. This repo's frontmatter profile is flat Obsidian-style properties:

- `type` (`note`, `decision`, `todo`, `index`), `title`, `summary` (one sentence, quoted), `tags` (array), `created` (date, set once).
- No `updated`/`timestamp` key and no `log.md` — git history is the modification record.
- Todo lifecycle lives in frontmatter properties (`status`, `priority`, `effort`, `blocked_by`), never in tags.

Edges between notes are wikilinks (`[[note-name]]`), which turn the directory into a traversable graph. When you add, rename, close, or delete a note, update its frontmatter *and* its entry in `docs/notes/index.md` in the same change — the index is the progressive-disclosure entry point and must not drift from the files.

Where this profile deviates from the OKF spec, the profile wins — do not "normalize" toward the spec: we use `summary` not `description`, `created` not `timestamp`, wikilinks not bundle-relative markdown links, and our `index.md` carries frontmatter (the spec's index files carry none).
