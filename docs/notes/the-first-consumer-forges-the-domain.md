---
title: The first consumer forges the domain
summary: "Building the SDL codec pressure-tested the core: every weak name and misplaced boundary surfaced under a real consumer's load — get one real consumer onto a young domain early, before speculative design hardens the wrong vocabulary."
tags: [note, ddd, design, vocabulary]
created: 2026-07-15
status: evolving
---

# The first consumer forges the domain

Building `Kingo.Sdl` (2026-07-14/15) was scoped as an adapter work item. What it actually did was interrogate the domain until the weak names confessed. None of the following was on the work item; all of it fell out of making one real consumer parse text into the core and render it back:

- `Policy` → `Schema` and `Statement` → `Fact` — the reach-test naming pass ([[domain-language]]).
- The aggregate collapse: four grammar-driven roots → two state-driven ones (`Schema`, `Fact`), with `Resource`/`Subject`/`SubjectSet` demoted to value objects and the `Schemas`/`Graphs` layout.
- `Create` as the house Cons: private constructors, one validating construction path, no trusted-assignment bypass for composites.
- The RDF set-first reading of the tuple order — Zanzibar's `object#relation@user` isn't backwards; it's set ∋ member.
- The port that wasn't: `IDocumentSerializer<T>` / `Kingo.Serialization` dissolved once the consumer showed there would only ever be one implementation ([[realign-serialization-projects-around-their-real-consumers]]).

Why an adapter does this: a codec has no tolerance for ambiguity. Round-tripping forces every value to say exactly what it is (structural equality, canonical text), error accumulation forces every invariant to have an owner and an error code, and rendering forces the "can this state exist?" question type by type. Speculative design answers those questions with plausible guesses; a consumer answers them with build failures.

This is the semantic sibling of the writing-csharp rule "the first slice sets the pattern" ([[architecture]]): the first *slice* hardens a layer's structure; the first *consumer* hardens the domain's vocabulary. Corollary for scheduling: when a domain is young, build one real consumer through it before adding breadth — the names are still cheap to change, and a week later they aren't.
