---
title: code review findings — identifiers rename arc
summary: "Triaged and validated findings from the medium code review of the identifiers/spec-name rename branch, checked against the code and the governing docs [[identifiers]] and [[specs]]. Two of the eight original findings do not survive validation."
tags: [note, review]
created: 2026-07-24
status: evolving
cites:
  - "[[identifiers]]"
  - "[[specs]]"
---

# code review findings — identifiers rename arc

A snapshot of the medium `/code-review` run on the `specs` branch at commit `c754c60`, after validating each finding against the code and the two governing docs. Work on this branch is continuing, so a finding may already be resolved by the time it is read; each names its files and lines so it can be re-checked directly.

Re-triaged 2026-07-24 against the current `specs` tree. Seven of the eight original findings are now closed: findings 1 and 7 as before, plus finding 4 (the docs and the code have since reconciled), finding 2 (the doc it named is fixed, leaving only a one-line stale comment), finding 3 (invalid — its divergence needs a value no constructor can produce without the caller breaking `Unchecked`'s contract), finding 8 (fixed — the projections that caused the double scan were removed), and finding 5 (fixed — the stale `Fact`/`Resource` examples are now qualified). One stands: finding 6, a refactor gated on the `[[ivalue-tself-tvalue-absorbs-all-value-type-wrappers]]` todo.

## Cleanups

### 6 — three name types share one grammar (confirmed shape; the reviewer's remedy is wrong)

`SpecName`, `NamespaceName`, and `RelationshipName` are structurally identical: same value, same `NamePattern`, differing only in type name and error-code prefix. The reviewer read that as duplication to collapse. It is not. The three are distinct domain concepts, and the separate types are what make illegal states unrepresentable — a spec name cannot be passed where a relationship name is required. Merging them into one shared name type would erase that guarantee and is the regression, not the fix. The structural sameness is incidental; the type-level distinctness is load-bearing.

The one real residue is mechanical: each type declares its own `[GeneratedRegex(IdentifierGrammar.NamePattern, ...)]` partial (`SpecName.cs:62`, `NamespaceName.cs:62`, `RelationshipName.cs:66`), so the source generator emits three compiled engines for `^[A-Za-z_][A-Za-z0-9_]*$`, and a parse-behaviour change touches three files. `IdentifierGrammar.cs:17` already shares the pattern const; sharing the compiled engine and the boilerplate is the `[[ivalue-tself-tvalue-absorbs-all-value-type-wrappers]]` route, which pushes the shared implementation into a generic base while keeping the three types. Keep the types; deduplicate the plumbing under them if and when that todo lands.

### 8 — NamespacePath scans for the separator twice (resolved 2026-07-24)

The double scan came from the two projections, `Spec` and `Name`, each reading a `SeparatorIndex` computed property. Rather than caching the index — eager-at-construction forces a character scan on every path built, whether or not the halves are ever read, and a `readonly record struct` cannot memoize lazily — the projections were removed outright. Their only consumer was one test, so nothing production read the halves. `NamespacePath` now holds the qualified string whole with no `Spec`/`Name`/`SeparatorIndex` members; the rationale is recorded in the type doc. If a caller ever needs the segments, they belong in an extension method, not on the value. Removed the projection test; `Kingo.Tests` passes at 100%.

### 5 — Fact and Resource carried the pre-qualification grammar in XML-doc examples (resolved 2026-07-24)

`Fact.cs` and `Resource.cs` showed bare examples — `doc:readme#viewer@anne`, `doc:readme#viewer@team:sales#member`, `folder:x#parent@folder:y`, `doc:readme` — missing the `<spec>/` qualifier that `NamespacePath` requires, while `SubjectSet.cs` alone was correct. The message half was already gone, the parse layer having moved out of `Kingo.Graphs`. The examples are now qualified (`io/doc:readme#viewer@anne`, `io/doc:readme#viewer@io/team:sales#member`, `io/folder:x#parent@io/folder:y`, `io/doc:readme`), consistent with `SubjectSet.cs`. Doc-comment only; `Kingo.Graphs.Tests` passes at 100%.

### 7 — single-char delimiters were strings (resolved 2026-07-24)

The four separators in `IdentifierGrammar` are now `char` (`SpecSeparator = '/'` and the other three). Continuing work had already pulled the parse layer out of `Kingo.Graphs`, so the `Fact`/`Resource`/`SubjectSet` string scans the finding named are gone. The one remaining consumer, `NamespacePath.SeparatorIndex`, now uses `IndexOf(char, StringComparison.Ordinal)` and `Name` adds a literal `1` for the separator width instead of `.Length`. One wrinkle: `NamespacePathPattern` keeps `/` as a regex literal rather than composing from `SpecSeparator`, because `GeneratedRegex` needs a compile-time-constant pattern and a `char` does not fold into one. Kingo builds clean and `Kingo.Tests` passes at 100%.

## Does not survive validation

### 3 — NamespacePath.Spec and Name diverge on a value with no separator (invalid)

The claimed divergence — `Spec` throwing `ArgumentOutOfRangeException` while `Name` returns the whole string — requires a `NamespacePath` whose `Value` has no `/`. No such value is representable: `Parse` enforces `name/name`, and the only other constructor is `Unchecked`, a trusted constructor whose precondition is the caller's contract. A caller that hands `Unchecked` a string with no separator has already broken that contract, and its own misuse is its defect. For every value the type can legitimately hold there is exactly one `/`, so `Spec` and `Name` never disagree. Making both sides fail identically would mean adding a runtime check against a contract violation — the guard the domain deliberately does not carry. No action.

### 1 — the `&`/`|` precedence change is the spec, not a regression (moot)

`specs.md:59` fixes the binding order: `!` tightest, then `&`, then `|`, so `a | b & c` is `a | (b & c)` (line 65), and the EBNF at 71–73 is a precedence cascade. That is exactly the tree the new parser produces. The old equal-precedence left-fold was the defect the change corrects, not the other way round, and the SDL parser is being replaced by [[specs]] regardless. The only thing to carry forward: the replacement parser must implement the binding order the spec now fixes — which it already specifies. No action.

### 2 — the SubjectId charset was not narrowed (invalid; doc fix all but done)

The finding assumed a rule of `^[A-Za-z0-9_][A-Za-z0-9_.:-]*$`. The actual rule is `IdentifierGrammar.IdPattern` = `^[^\s\p{C}]+$` (`IdentifierGrammar.cs:51`) — any run of visible, non-control characters. `user:anne` matches it and parses fine, so the "stored facts stop loading" scenario does not occur. The `SubjectId.cs` type doc (lines 10–13) has since been rewritten to match `IdPattern` ("a non-empty run of visible characters with no whitespace and no control characters"). One stale residue remains: the comment on the internal `SubjectIdPatterns` regex (`SubjectId.cs:61–62`) still says an id "must never contain the fact-grammar delimiters `/` `:` `#` `@`", which contradicts the permissive pattern directly below it. Fix that comment. The deeper tension it points at is standing caller-grammar work, not something this branch introduced: a permissive id containing a delimiter makes a first-delimiter fact split ambiguous.
