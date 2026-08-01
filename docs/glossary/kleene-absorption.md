---
title: kleene-absorption
type: definition
summary: "Three-valued operator semantics whose verdict is a function of operand values alone."
status: evolving
---

Three-valued operator semantics whose verdict is a function of operand values alone.

## Disposition

Fold into the rewrite evaluation spec when it lands, and retire this entry in the same change. The full ruling belongs beside the operator semantics it governs: error as the third value, which values absorb under each operator, why short-circuiting stays sound, and why strict error-poisoning was rejected. It sits here only because no evaluation spec exists yet.
