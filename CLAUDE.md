# Kingo

Kingo is a Google Zanzibar-inspired ReBAC authorization system: a relationship-based access control system with subject-set rewrite rules.

## House rules

Load the `csharp:writing-csharp` skill before writing code or performing a code review.

One repo-level override to that skill: the internal domain and libraries take **no runtime null checks, ever** — nullable reference annotations are the contract. The skill's `ArgumentNullException.ThrowIfNull` rule applies only at the API edges (the service hosts, where callers are uncontrolled); those edges are 100% guarded, so the domain behind them is already protected and never needs null validation. CA1062 is suppressed **per project** via `GlobalSuppressions.cs` in each domain/library project — deliberately not solution-wide, so edge-facing projects (API hosts, ports) do not inherit it and keep the rule active. A new domain/library project gets the same `GlobalSuppressions.cs`; an edge project doesn't.

## Docs and notes

Design docs live under `docs/`; working notes, decisions, and todos live in `docs/notes/`, indexed by `docs/notes/index.md`.

Notes use the Hoplite frontmatter standard, layered under the Open Knowledge Format (OKF v0.1, spec: https://github.com/GoogleCloudPlatform/knowledge-catalog/tree/main/okf). Hoplite is the authority for frontmatter and edges — **read both spec files before editing frontmatter or link structure**:

- `D:\projects\hoplite\hoplite\docs\hoplite\frontmatter.md` — flat Obsidian Properties; four special keys (`title`, `summary`, `aliases`, `tags`), all else open vocabulary; a key's value decides what it is: scalar = claim, quoted wikilink = typed edge with the key as predicate.
- `D:\projects\hoplite\hoplite\docs\hoplite\expressing-edges.md` — every inline link is an edge (`links-to` by default, typed by an adjacent `<!--predicate-->` comment); frontmatter wikilink values must be quoted; `aliases` keeps `[[old-name]]` resolving after a rename.

Kingo-specific conventions on top:

- `type` is always present (`note`, `decision`, `todo`, `index`) — it's OKF's one required key.
- `summary` is one quoted sentence; `created` is a date set once. No `updated`/`timestamp` key and no `log.md` — git history is the modification record.
- Todo lifecycle lives in frontmatter properties (`status`, `priority`, `effort`, `blocked_by`), never in tags.
- When you add, rename, close, or delete a note, update its frontmatter *and* its entry in `docs/notes/index.md` in the same change — the index is the progressive-disclosure entry point and must not drift from the files.

Where OKF and Hoplite disagree, Hoplite wins — do not "normalize" toward the OKF spec: `summary` not `description`, `created` not `timestamp`, wikilinks over bundle-relative markdown links, and our `index.md` carries frontmatter.
