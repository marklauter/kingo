---
title: The walk gathers constraints; set theory decides
type: note
summary: "Contains intermixes graph theory and set theory with separate guards: traversal over facts discovers which set expressions apply, and Kleene evaluation of the assembled expression produces the verdict. The walk assembles the question; it never answers it."
tags: [interpreters, closures]
created: 2026-07-21
status: evolving
---

# The walk gathers constraints; set theory decides

[[contains]] reads as a graph walk, but the traversal never answers the membership question — it assembles it. Subjectsets are traversable because stored subjectset members and factset hops ([[theories]]) re-enter the evaluation (the private recursive variant behind the public `Contains`); each re-entry discovers another instantiated set expression, a computed subjectset rooted at a resolved resource. When the constraints are in hand, the verdict is [[kleene-absorption]] applied to the assembled expression, not a property of any path.

The recursion therefore has two sources, and each has its own guard:

- **Set-theoretic recursion lives in the [[theory]]**: `SubjectSetRewrite.ComputedSubjectSet` crossings. Finite, made acyclic at `Namespace.Create` ([[namespace-create-validation]]), and free in the depth accounting.
- **Graph-theoretic recursion lives in the facts**: factset hops and subjectset-valued member expansions. User-writable, unbounded, and exactly what the depth bound meters.

The graph is the resource-to-resource notation: `Fact.ResourceFact` — `io/folder:x#parent@io/folder:y#...` — is the edge the walk traverses. The walkable pointer is the only member encoding that is graph rather than set ([[resource-fact-case]]). The other two encodings denote sets: an identity its singleton, a subjectset its derived set. A [[fact]] asserts inclusion between sets ([[ubiquitous-language]]), so the walk is confined to the pointer encoding and the rest is set algebra.

The framing also explains why "reachability" was rejected when [[closure]] was named. An `!` operand contributes a constraint no walk can witness: non-membership's proof is the exhausted search. So a verdict is a function over several reachability questions, and the no-proof-path ruling in [[rewrite-interpreters]] is the same fact seen from the Decision's side.

Walking is a sound intuition only for the union-only fragment of the algebra; "derivable" is the word that survives exclusion ([[closure]]).
