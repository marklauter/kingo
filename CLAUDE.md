# Kingo

Kingo is a Google Zanzibar-inspired ReBAC authorization system: a relationship-based access control system with subject-set rewrite rules.

## House rules

Load the `csharp:writing-csharp` skill before writing code or performing a code review.

One repo-level override to that skill: the domain and libraries take **no runtime null checks** — nullable annotations are the contract. `ArgumentNullException.ThrowIfNull` applies only at the API edges, where callers are uncontrolled and CA1062 stays active. Domain/library projects suppress CA1062 by setting `<KingoDomainLibrary>true</KingoDomainLibrary>` (see `Directory.Build.targets`); edge projects omit the flag.

## Docs and notes

The docs corpus is owned by the hoplite skills. Before writing anything under `docs/`, load the hoplite skill that owns the artifact's form and follow it — the skills carry the frontmatter standard, file locations, and edge/link syntax:

- Term — a word plus its smallest phrase → `hoplite-skills:glossary`.
- Concept — composed from locked terms → `hoplite-skills:spec`.
- Decision — a hard-to-reverse trade-off → `hoplite-skills:decision`.
- Note — more than a fleeting thought → `hoplite-skills:taking-notes`.
- Todo — a task to be completed or a follow-up needed → `hoplite-skills:todo`.
- Journal entry — what happened and why, immutable and dated → `hoplite-skills:journaling`.
- Designing or sharpening the domain model itself → `hoplite-skills:domain-modeling`.

Any prose artifact gets a `hoplite-skills:proofreading` pass before it's committed or presented.
