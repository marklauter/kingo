---
title: Reserved words live with the tokenizer
summary: "Relocate IsReserved beside the rewrite-expression token table: `this` is defined in three places (tokenizer keyword table, printer emit arm, IsReserved), so a future keyword added to the tokenizer would not automatically become reserved for the printers and SchemaParser."
tags: [note, todo, sdl]
created: 2026-07-21
status: open
priority: low
effort: low
---

# Reserved words live with the tokenizer

Code-review finding on the [[resource-fact-case]] change set (2026-07-21), deferred as out of scope for that patch — pre-existing structure the patch only shrank.

The reserved-word predicate `RewriteExpressionPrinter.IsReserved` (`src/Kingo.Sdl/RewriteExpressionPrinter.cs`) encodes knowledge whose authority is the tokenizer: `this` is defined in the tokenizer keyword table, the printer emit arm, and `IsReserved`. A keyword added to the tokenizer would not automatically become reserved, so `SchemaParser` would accept a relationship name no rewrite expression could ever reference. Relocate the predicate beside the token table so the keyword list is declared once and the printers and parser consume it.
