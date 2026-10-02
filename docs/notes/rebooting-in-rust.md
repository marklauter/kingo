---
title: Rebooting in Rust
type: note
summary: "Working notes from a 2026-10-02 design conversation: keep strategic DDD and drop most tactical ceremony; move the whole back end to Rust around one core crate; build Check first so it forces the storage layout; archive the C# main and restart from a clean-room spec; port the theory document and rewrite-expression parser through a language-neutral conformance corpus."
tags: [architecture, rust, reboot, parser, storage]
status: evolving
---

# Rebooting in Rust

Working notes from a design conversation (Mark, 2026-10-02). Nothing here is ruled; each section ends in a leaning, not a decision. A decision record should follow if the reboot goes ahead.

## Is the DDD worth it?

The domain is a calculus — the rewrite algebra (`Theories`), set membership (`Facts`), and two interpreters over them (`Closures`) — not a business domain.

- **Keep (strategic):** the ubiquitous language (`docs/glossary/`), the Facts/Theories split meeting only in Closures, ports and adapters, and the load-profile service split ([[grouping-the-apis-into-services]]). Language-neutral, and where the design value lives.
- **Drop or relax (tactical):** aggregates and value-object wrappers on every identifier, the `Create`/`Checked`/`Parse` triad, ArchUnit rules enforcing `sealed` and `readonly record struct`. In a language with ADTs these collapse into sum types, newtypes, and smart constructors; the C# form is ceremony paying for missing language features.

## Language split

A Python control plane over a Rust ACL core was considered and rejected as framed: Write is not CRUD. It validates facts against the current theory and refuses theory writes that would orphan live facts ([[preventing-drift-between-facts-and-theories]]), so it needs the theory model and the rewrite algebra. A Python reimplementation is a second source of truth for the core semantics.

| Option | Gains | Costs |
|---|---|---|
| Rust core + Rust Check/Expand + Python control plane via PyO3 | One source of truth; Python iteration speed for CRUD/admin | Two toolchains; FFI packaging |
| **All Rust (axum hosts)** | One language; core shared natively | Slower iteration on boring endpoints |
| Python calls a Rust Write/Validate service over RPC | No FFI | Extra hop (fine, Write is rare); a contract to version |
| Stay .NET, core in F# | Keeps ecosystem and tests; DUs | GC on the hot path |

**Leaning: all Rust.** Rust earns its place on Check (no GC at p99, cache and future Leopard-style index density). Invariant regardless of option: the theory parser, rewrite AST, and interpreters are one crate every host links; nothing reimplements them.

## Front end (deferred)

A SPA admin console — theory editor, fact browser, Expand tree viewer, Watch stream. React leans ahead of Angular on ecosystem fit (React Flow for graphs, Monaco wrappers) and its functional component model; Angular wins if several developers need enforced consistency, and RxJS fits Watch. Framework-independent and more important: **compile the core crate to WASM** so the editor validates and previews rewrites with the exact code the Write service runs. Generate TS types from Rust (`ts-rs` or OpenAPI via `utoipa`); never hand-write mirrored DTOs. Not a priority until the REST services are right.

## Build Check first

Agreed: query-first design. Check is the only latency-critical path, so its reads should own the key design; Write is rare and conforms. Check needs two access patterns: point lookup `(subjectset, identity, snapshot)` and range read `subjectset @ snapshot`.

Caveats, all from [[storage-versioning-design]]:

1. **The time model is write-side.** Check consumes a Kookie; Write produces the total order behind it (commit-wait, timestamp source, interval stamps). Rule the Kookie/total-order design on paper before Check's storage code.
2. **Write needs patterns Check never reveals:** the reverse existence query, theory-at-K lookup, conditional writes, atomic multi-fact changesets (DynamoDB's 100-item transaction limit), and Watch's changelog order. Keep the paper pass of all three consumers before code.
3. **Check needs data before Write exists.** A seed loader writes raw fact rows outside the domain, and must not grow into a half-Write that skips the drift invariants.

Sequence: Kookie/total-order ruling (paper) → Check end to end against real storage, seed-loaded → paper pass of Write and Watch patterns against the layout → Write.

The existing port spec ([[fact-reader-port]]) is C#-shaped (`ValueTask`, `Result`, overloads, `CancellationToken`). Its semantics carry over and its signature does not: absence is the empty set; the only failure is "the snapshot could not be consulted"; that failure is a value so [[kleene-absorption]] can absorb it. In Rust: a trait method returning `Result<Vec<Fact>, Unavailable>`.

## The reboot

**Leaning: do it.** About 1,100 lines of source; the value is the ~75 docs, mostly language-neutral. The procedure already exists ([[clean-room-procedure]]): the C# implementation becomes the excluded prior attempt, and its lessons arrive as requirements in a handoff note rather than as ported code. A line-by-line port would import C# idioms the clean room exists to filter.

| Mechanics | Gains | Costs |
|---|---|---|
| **Branch/tag current main (e.g. `archive/csharp`), then one commit deleting `src/`, `tests/`, .NET infra** | Doc history and blame survive; the switch is one readable commit | Main's log still shows the C# era |
| Orphan branch as new main | Clean history | Loses history on the docs being kept |
| New repo | Clean slate | Breaks wikilinks, history, name continuity |

**Leaning: the first.** Keep `docs/`, `CLAUDE.md`, `LICENSE`, `images/`; replace dotnet CI, dependabot, and CodeQL config with cargo, clippy, and `cargo-deny`.

Watch for:

1. About 16 docs reference C#/.NET specifics. Sweep them before the clean-room session or they become inputs contradicting the handoff note; [[architecture]] and [[fact-reader-port]] need rewrites, not edits. Some decisions (e.g. [[deciding-which-types-parse-text]]) may dissolve.
2. The tests are the most valuable code being discarded. Extract their cases as language-neutral examples or property-test generators.
3. This is the second reboot (`main-archive`, `dictionary-encoding`). Record why — Rust for the hot path, one core crate, no Python/.NET — as a decision record, so a third doesn't relitigate it.

Order: archive → doc sweep → handoff note for the core crate (theory model, rewrite algebra, Check interpreter) → Kookie/total-order ruling → Check against real storage.

## Porting the parser

The user-visible format is the **theory document** (YAML: `name:` plus a `namespaces:` map); relations carry **rewrite expressions** such as `(this | editor | (parent, viewer)) ! banned`. About 200 lines of parser against about 900 lines of parse/print/round-trip tests — the tests already define the language.

The clean-room spec is three artifacts:

1. **EBNF with precedence and associativity**, read off the Superpower parser:
   - `!` binds tightest and is left-associative: `a ! b ! c` is `(a ! b) ! c`.
   - then `&`, then `|`; union and intersection are n-ary (flattened), not nested binary.
   - `(a, b)` is fact-to-subjectset and is tried before parenthesized grouping.
   - `this` is case-insensitive; identifiers are C-style; `#` starts a comment.
2. **Rules the grammar doesn't carry:** the depth limit (pre-parse token scan); the theory document's YAML shapes (a relation is a bare name or a single `name: expr` pair; null or empty means a namespace with no relations); canonical print form; the known round-trip gap (single-child union/intersection prints as its bare child).
3. **A conformance corpus:** test cases lifted out of xUnit into data files — input plus expected tree (as an S-expression) or expected error code. Run it against the C# parser *before archiving*; a passing run proves the spec matches the implementation. The Rust parser must then pass the same corpus.

Parser libraries:

| Option | Fit | Tradeoff |
|---|---|---|
| **chumsky** | Closest to Superpower (tokenize, then parse tokens); spans, error recovery, best diagnostics — matters if the editor runs it via WASM | API churn between versions; compile times |
| winnow | nom's successor in ergonomics; used by `toml`; stable, fast | User-facing errors need more work |
| nom | Mature, fast | Clunkier; basic errors |
| pest | Grammar in a PEG file | Generic parse tree walked by hand; weak typing |
| Hand-written Pratt/recursive descent | ~150 lines, zero deps | Error handling by hand |

**Leaning: chumsky**, hand-written a close second given the grammar's size.

Rust-specific:

- **The depth guard matters more.** Stack overflow aborts the process and cannot be caught; keep the pre-parse token scan.
- **Check the YAML crate landscape at start.** `serde_yaml` was archived in 2024 and its forks have been unsettled. The parser needs the node tree with source positions, not just serde, for the same reason the C# code uses YamlDotNet's node model; `saphyr` is the likely candidate.

Order: extract the corpus and EBNF, make the C# parser pass → archive → Rust parser test-first against the corpus → `proptest` print/parse round-trip.
