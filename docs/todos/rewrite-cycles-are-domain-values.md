---
title: Rewrite cycles are domain values
type: todo
summary: "DetectCycles renders its result into the Error message, so the cycle path has no representation but prose and the tests pin the wording. Give it a RewriteCycle type holding a canonical ImmutableArray<RelationName>, and make the message its projection."
tags: [theory, testing]
created: 2026-07-29
status: open
priority: medium
effort: medium
supports: "[[namespace-create-validation]]"
---

# Rewrite cycles are domain values

`Namespace.DetectCycles` finds a cycle as a walk and immediately renders it into an `ErrorMessage`: `rewrite cycle in namespace 'file': 'a' -> 'b' -> 'c' -> 'a'`. The path exists only inside that string, so a test with something to say about the cycle has nothing to assert against but the wording. Roughly a dozen assertions in `Kingo.Theories.Tests` do exactly that, and swapping a `GroupBy` for a `ToLookup` reddens three of them without changing behaviour ([[the-test-suite-drifted-where-it-was-copied]]).

`Error` cannot carry the path. It holds `ErrorType`, `ErrorCode`, and `ErrorMessage`, it comes from `MSL.Results`, and widening it there to hold a domain payload would be the wrong repository and the wrong shape ([[architecture]]).

## Shape

A value type in `Kingo.Theories`:

```csharp
public sealed record RewriteCycle
{
    public ImmutableArray<RelationName> Path { get; }

    private RewriteCycle(ImmutableArray<RelationName> path) => Path = path;

    public static Result<RewriteCycle> Create(ImmutableArray<RelationName> path) => /* non-empty, distinct */;

    public override string ToString() =>
        string.Join(" -> ", Path.Append(Path[0]).Select(step => $"'{step}'"));

    public bool Equals(RewriteCycle? other) =>
        other is not null && Path.AsSpan().SequenceEqual(other.Path.AsSpan());

    public override int GetHashCode() => SequenceHash.Of(Path);
}
```

Three decisions carry it:

**The path is canonical, not the walk that found it.** `a -> b -> c -> a` and `b -> c -> a -> b` are one cycle, and which one the search reports depends on where it entered the graph. `Create` rotates the least `RelationName` to index 0, so the representation is a function of the cycle rather than of the traversal. Two detections of one cycle then compare equal and dedup is a `HashSet<RewriteCycle>`. `SequenceHash` already exists in `Kingo.Theories` for the same reason on `SubjectSetRewrite`.

**The closing element is derived.** Storing `[a, b, c, a]` puts "first equals last" in the type, where `Create` has to defend it and every reader has to remember it. Store the distinct cycle; `ToString` closes the loop.

**`Error` stays the projection.** `DetectCycles` returns `ImmutableArray<RewriteCycle>` and `Namespace.Create` maps each one to `Error.Validation(ErrorCodes.Namespace.RewriteCycle, ErrorMessage.Unchecked($"rewrite cycle in namespace '{name}': {cycle}"))`. The failure channel stays `ImmutableArray<Error>`, so nothing about `Result<T>` changes and no caller of `Namespace.Create` is affected.

## Done looks like

- `RewriteCycle` exists with `Create`, canonical `Path`, structural equality, and `ToString`.
- `DetectCycles` returns cycles, not messages.
- The cycle tests assert against `DetectCycles` structurally: `Assert.Equal(cycle(a, b, c), Assert.Single(detected))`. Exactly one test pins the rendering, on `RewriteCycle.ToString()`, where the format is the contract.
- The dangling-reference and duplicate-relation errors are untouched: neither carries a structure beyond the names already in its message.

## Open question

`DetectCycles` is `private` today. Two ways to open it: `internal`, which the tests already reach through the `InternalsVisibleTo` that `Directory.Build.props:22` grants every project's `.Tests` sibling, or `public` for both `RewriteCycle` and the detector. Internal is the smaller surface and enough for the tests. Public is right if a cycle is vocabulary a caller outside the assembly would ever hold: an admin surface reporting which relations loop wants the cycles as data rather than as prose parsed back out of an error message. Decide with the first consumer; the type's shape is the same either way.
