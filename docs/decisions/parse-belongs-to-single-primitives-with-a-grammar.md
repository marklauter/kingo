---
title: Parse belongs to single primitives with a grammar
summary: "A type parses its own text only when it is a single primitive whose grammar Kingo owns. Composites are constructed, never parsed, and text formats live in adapters."
tags: [decision, core]
created: 2026-07-25
status: locked
---

# Parse belongs to single primitives with a grammar

A type carries a core `Parse` when it wraps one primitive whose character rules Kingo owns. The contract is `IParse<TSelf>`, which declares the `static abstract Result<TSelf> Parse(string s)`; `IValue<TSelf, TValue>` inherits it and constrains `TSelf` to a struct, so implementing `IValue` is how a terminal picks the contract up. In code that set is exactly six types: `TheoryName`, `NamespaceName`, `NamespacePath`, `RelationName`, `ResourceId`, and `SubjectId`. The grammar is the whole contract of such a type.

Every other type is constructed, not parsed. Where the invariants are relational, construction runs through a validating factory: `Theory.Create` and `Namespace.Create`. Where the typed components are the whole contract, the primary constructor is enough, which is the case for `Resource`, `SubjectSet`, `Relation`, and the three `Fact` cases. Neither route goes through text. Composition decides the side a type lands on.

Text formats belong to adapters. `Kingo.Documents` owns the theory document and the rewrite expression language, and any future format owns itself the same way.

A terminal can lose its `Parse` too. If a canonical notation ever needs escaping, quoting, encoding variants, or versioning, it has become a wire format: the whole pair, `Parse` and `ToString`, moves to a serialization adapter and core keeps structured construction only.

A notation can be a language, and the boundary between the two decides where a new notation belongs. A type may parse a notation that represents it — a fixed composition of terminals, no recursion, with a byte-stable `ToString` inverse, the shape an ISO 8601 date has. A recursive grammar is a language, and languages get parsers that live in adapters. The rewrite expression language has operators, precedence, and parentheses, which is why it was born in `Kingo.Documents` and stays there.

A terminal carrying `Parse` is a value capable of crossing a boundary. The format on that boundary is the converter's, including whether a JSON payload spells the value as a string token or a structured object. Converters call `Parse` at the trust boundary, and nothing about the format travels inward.

The line is mechanical enough to enforce. Inside `Kingo`, `Kingo.Facts`, and `Kingo.Theories`, no type outside the `IParse` implementers declares a public `Parse`. An architecture test can assert that, alongside the tests that already pin each half of the model against referencing the other. The scope matters: `Kingo.Documents.TheoryParser.Parse` is public and correct, because an adapter's job is exactly the thing the core declines.

## Alternatives

**Composite `Parse` factories in core.** The earlier rule said a type parses itself when the fact grammar defines its text form, which put `Parse` on `Resource`, `SubjectSet`, and `Fact` for the `theory/namespace:id#relation@subject` notation. It lost because the notation cannot be enforced. A [[resource]] id and a subject id are the caller's to shape, so they admit nearly any visible character — including `/`, `:`, `#`, and `@`, the delimiters the notation depends on. No unambiguous parse exists, and the implementation cost bought nothing any consumer had asked for.

**Escape or quote the notation to make it parseable.** Reserving an escape character recovers an unambiguous grammar. It also makes the notation a wire format, which brings encoding variants, versioning, and a compatibility burden on a public contract. Nothing needs it: there is no fact markup language planned, and structured construction serves every present caller.

**Push all parsing to adapters, terminals included.** Uniform, and it moves validity out of the type. A terminal's grammar is its contract, so relocating `Parse` lets an invalid `RelationName` exist inside the core, and validity becomes an adapter's promise instead of a type's guarantee.

## Why

The rule buys invariants that hold by construction. A value that exists satisfies its grammar, and a composite that exists satisfies its relational invariants. Neither depends on which edge built it.

It also keeps third-party parsers out of core signatures. A `YamlDotNet` or combinator type in a domain method is the tell that the code belongs in an adapter. The [[theories]] side shows the scale a text form can hide. Reading a theory document takes a third-party YAML parser, a recursive walk from its node model into domain values, and a combinator parser for the rewrite expressions nested inside the scalars. All of it lives in `Kingo.Documents`.

It costs facts their round-trip text form. Anything needing to move a [[fact]] as a string must define that format in an adapter and own the escaping question this decision declines to answer.

The grammar in [[facts]] describes the structure of the `Fact` cases rather than a format anything parses. If a fact markup language ever arrives, the `Kingo.Facts` types are its abstract syntax, never its parser — the parser is new code in an adapter, and these types stay the thing it produces.

One document still assumes the superseded rule: [[rewrite-interpreters]] error condition 2 pins a `SubjectSet.Parse` refusal, and that method no longer exists. Reword it when the interpreter work starts.
