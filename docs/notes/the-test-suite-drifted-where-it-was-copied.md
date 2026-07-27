---
title: The test suite drifted where it was copied
summary: "Audit of all nine test projects against the writing-csharp testing directives: 65 findings, 16 high. Every substantive project hand-rolled one case list into several near-identical files, and each copy lost a different case."
tags: [note, testing, audit]
created: 2026-07-27
status: evolving
---

# The test suite drifted where it was copied

Audited on 2026-07-27 against the `csharp:writing-csharp` testing directives, across all nine test projects. 65 findings: 16 high, 26 medium, 23 low.

| Project | Findings (H/M/L) | Untested units |
| --- | --- | --- |
| `Results.Tests` | 12 (2/5/5) | none |
| `Kingo.Theories.Tests` | 12 (1/6/5) | 3, all covered transitively |
| `Kingo.Documents.Tests` | 12 (2/4/6) | none |
| `Kingo.Tests` | 14 (4/6/4) | 6 dead or stub symbols |
| Thin tail (`Facts`, `Values`, `Closures`, `Serialization.Json`, `Kingo.Testing`) | 15 (7/5/3) | nearly all |

## One live defect

Every `Parse` in `src/Kingo` accepts a trailing newline. `IdentifierGrammar` anchors all three patterns with `^…$` (`src/Kingo/IdentifierGrammar.cs:15,17,19`), and .NET's `$` matches at end of input *or* immediately before a terminal `\n`; `RegexOptions.Singleline` governs `.` only and does not change this.

```
ResourceId.Parse("doc\n")       → Success("doc\n")
NamespaceName.Parse("file\n")   → Success("file\n")
NamespacePath.Parse("io/f\n")   → Success("io/f\n")
```

All six identifier types share these patterns, so all six are affected. `ResourceId` and `SubjectId` are stored verbatim, so the newline rides into a persisted identifier. Two ids differing only by that newline then render identically in logs and error messages. The fix is `\z` in place of `$` in `NamePattern`, `NamespacePathPattern`, and `IdPattern`. Per the skill's bug-fix rule, the rejecting case goes in first and proves red.

All six test files test only the *interior* newline (`"a\nb"`), which is why the suite is green over it.

## The dominant pattern

A case list written once, copied into a family of sibling files, then diverged — each copy losing a different case. It is the top finding in all four substantive projects:

- `tests/Kingo.Tests` — six identifier files. `SubjectIdTests` omits the NUL case its `ResourceIdTests` twin has, though both compile the identical `IdPattern`. Its `Unchecked_BypassesValidation_AcceptsRejectedInput` feeds `"a#b@c"`, which `Parse` *accepts*. The test would survive `Unchecked` starting to validate.
- `tests/Results.Tests` — `Error.Conflict` asserts only `ErrorType`; the four sibling factories all assert Code and Message. Transposed constructor arguments pass.
- `tests/Kingo.Documents.Tests` — `Union` appears in every operand position of every printer helper, `Intersection` in only some. The gap lands on `PrintTerm`'s `Intersection` arm: `Exclusion(a, Intersection([b,c]))` must print `a ! (b & c)`, and dropping `Intersection` from that `or` list emits `a ! b & c`, which re-parses to a structurally different tree with the suite green.
- `tests/Kingo.Theories.Tests` — the depth bound is tested past-bound for all three operators, at-bound for `Exclusion` only, and that one assertion is stranded in `NamespaceTests.cs:374`.

Two remedies, and they differ per project. Where the sibling types share a contract (`Kingo.Tests`, over `IValue`), collapse the copies into one shared contract test so a divergence cannot recur. Where they share only a shape (`Documents`, `Theories`), a `[Theory]` over the union's inhabitants forces the author to name every position.

## Green that means nothing

- `src/Kingo.Serialization.Json` contains **zero `.cs` files** — a `.csproj` and nothing else. Its test project's five inherited architecture facts pass against an assembly with no types: four via `.WithoutRequiringPositiveResults()` (`tests/Kingo.Testing/ArchitectureTestsBase.cs:23,35,49`), the fifth because `GetTypes()` is empty.
- `src/Kingo.Facts` is three positional records with no hand-written members. Its 141 test lines assert compiler-generated record equality and positional assignment — the C# compiler's contract, not ours.
- `src/Kingo.Closures` is two memberless placeholder records (`Decision`, `Expansion`) with no tests, while its `.csproj` already references `Kingo`, `Kingo.Facts`, `Kingo.Theories`, and `Results`.
- `tests/Kingo.Testing` is the shared ArchUnitNET base every project inherits, and it carries real hand-written reflection with no test over it. Its name does not end in `.Tests`, so `Directory.Build.props` never sets `IsTestProject`/`CollectCoverage` and no `<Include>` filter names it. It is invisible to the coverage ratchet. Its `ImplementsIValue` detector string-matches the literal ``"Values.IValue`2"`` (`ArchitectureTestsBase.cs:64`), so renaming that namespace turns the wrapper rule into a silent no-op across all seven consuming projects.

## Missing architectural rules

Two skill invariants the corpus relies on have no ArchUnitNET rule, and both have a live consumer:

- **Closed hierarchies.** `SubjectSetRewrite`'s CA1034 suppression (`src/Kingo.Theories/SubjectSetRewrite.cs:7`) is justified by the nesting closing the case set against a seventh inhabitant — and nothing enforces that. `SubjectSetRewriteTests.cs:344` claims to, but only exercises the C# `switch`: a seventh inhabitant simply stays absent from the list and the test remains green. `Fact` (`src/Kingo.Facts/Fact.cs:6`) has the same exposure via its `private protected` constructor.
- **Persistence ignorance.** No rule forbids `[Table]`, `[Column]`, `[Key]`, `[JsonPropertyName]`, or `[DataMember]` on domain types. None exist in `src/` today, so the rule would pass on arrival — the cheapest moment to add it, and it matters before `Kingo.Serialization.Json` gets its first converter.

Layer dependencies are enforced once, hand-rolled over `GetReferencedAssemblies()` in `tests/Kingo.Facts.Tests/Architecture/ArchitectureTests.cs:10` — not ArchUnitNET, not in the shared base, and blind to transitive references. See [[architecture]] for the layering it is meant to hold.

## Contract-coupling

Concentrated in `Kingo.Theories.Tests`: roughly a dozen assertions pin error-array ordering and exact message wording, including the rendered cycle path `'a' -> 'b' -> 'c' -> 'a'`. Nothing in `Namespace.Create` promises an order, so swapping a `GroupBy` for a `ToLookup` reddens three tests without changing behaviour.

The root cause is structural: `Results.Error` carries only `Type`, `Code`, and `Message`, so a cycle path has nowhere to live but the message string, and the test has no other handle. The skill's own remedy is that a modeled failure should carry its context — which points at a `RewriteCycleError` holding `ImmutableArray<RelationName> Path` rather than at a looser assertion.

Elsewhere it is isolated: `Assert.Same` pinning an allocation optimisation (`ApplyUnitTests.cs:47`), and a hash-code *inequality* assertion (`SubjectSetRewriteTests.cs:216`) that the `GetHashCode` contract never promised.

## Where to start

1. The trailing-newline defect — red first, then `\z`, then the six rejecting cases.
2. The closed-hierarchy ArchUnitNET rule, covering both `SubjectSetRewrite` and `Fact`; delete the pattern-match test it replaces.
3. `Kingo.Testing` into the coverage ratchet, and its `IValue` detector off the hardcoded string.
4. Collapse the six identifier test files onto one shared `IValue` contract test.

Deferred by nature: `Kingo.Serialization.Json` and `Kingo.Closures` need code before they need tests. Their case lists are drafted in the audit and should be written with the first slice, per the skill's rule that the first instance sets the pattern.
