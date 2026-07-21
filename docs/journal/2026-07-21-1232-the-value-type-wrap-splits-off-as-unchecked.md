---
title: The value-type wrap splits off as unchecked
summary: "Minutes after locking parse/create/print as the three construction verbs, the create glossary work exposed an overload: the value-type primitive wrap and composite construction differ on whether misuse can mint an invalid value. IValue.Create is renamed IValue.Unchecked; there are four verbs."
tags: [journal, schemas, vocabulary]
created: 2026-07-21
---

Context: [[2026-07-21-1216-parse-create-print-settle-as-the-three-construction-verbs]] locked the trio with **create** as "the trusted construction" and claimed every producer path is one of the three.

Tried: reducing the create glossary entry. Each refinement (reserved for hot paths, text only on value types, never validates the primitive) pulled the value-type wrap and composite construction further apart.

Expected: one kernel with the value type as the degenerate case.

Learned: they differ on the property that matters — misuse of the value-type wrap can mint an invalid value; a composite construction cannot, since its parts arrive typed and it enforces the relational rest. One word carrying both was an incomplete reduction. Mark renamed `IValue.Create` to `IValue.Unchecked` (the Rust `_unchecked` prior; the VS Code refactor carried the crefs), and the vocabulary is now four verbs: parse the checked lift, unchecked the primitive embedding, create composite construction, print the retraction. A three-agent sweep cleared the prose drift the rename left in docs, source comments, and test names.
