# First slice introduces sealed records and value-type wrappers without ArchUnitNET gates

Severity: nit
Type: code
Location: `Kingo.Policies/PdlDocument.cs:1`
Principle: The first slice sets the pattern
The pattern lands without an architectural test enforcing it for future slices.

## Observation
This diff is the first concrete example on `reboot` of two recurring patterns:
1. Sealed records as the standard AST/data-carrier shape (`PdlDocument`, `Namespace`, `Relation`, and the `SubjectSetRewrite` hierarchy).
2. Value-type wrappers as the identifier shape (`NamespaceIdentifier`, `RelationIdentifier`).

No ArchUnitNET tests ship with the diff to enforce either pattern. The codebase's `Directory.Packages.props` does not reference `TngTech.ArchUnitNET` yet (the Plumber canonical pattern does).

## Why it matters
writing-csharp: "ArchUnitNET rule ships in the same change set as the first instance of the pattern." Without the gate, the next slice can land an unsealed record or a primitive-typed identifier and nothing in the build will notice. The discipline is cheap to add now (two-project codebase) and progressively more expensive as the type count grows.

## Suggested fix
Two options, in increasing scope:

1. Defer until there's a clearer architectural shape (more projects, defined layers). Acknowledge the gap explicitly in a doc note so future-you doesn't forget.
2. Land a minimal `Kingo.Architecture.Tests` project now with two tests:
   - All concrete classes and records in `Kingo.Policies` are `sealed`.
   - All public structs in `Kingo.Policies` are `readonly` and implement `IEquatable<TSelf>`.

Option 2 is closer to the writing-csharp prescription. Option 1 is the smaller commit if you're not ready for a third project.
