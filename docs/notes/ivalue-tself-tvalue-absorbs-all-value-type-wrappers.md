# IValue<TSelf, TValue> absorbs all value-type wrappers

Tags: todo,hexagonal
Tomorrow's slice — rewrite every value-type wrapper to implement IValue<TSelf, TValue>, retire IStringConvertible<T>, and drop the throw-on-Empty smell.

## Observation
Existing value-type wrappers (`NamespaceIdentifier`, `RelationIdentifier` in `Kingo.Pdl`) implement `IStringConvertible<T>` with `static abstract T From(string)` and `static abstract T Empty()` — the latter implemented as a throw because no valid empty value exists.

`IValue<TSelf, TValue>` (in `Values`) is the canonical contract for value-type wrappers:
- `TValue Value { get; }` — public access to the wrapped primitive.
- `static abstract TSelf Create(TValue)` — trusted path, no validation.
- `static abstract Result<TSelf> Parse(string)` — untrusted path, full validation, returns `Result<TSelf>`.
- `static abstract bool TryParse(string, out TSelf)` — BCL adapter; each implementor declares it on its own type so reflection-based pipelines (ASP.NET Core parameter binding, etc.) discover it. The canonical body lives in the sibling helper `ValueParser.TryParse<TSelf, TValue>` so each implementor's declaration can be a one-line delegation. (Named `ValueParser` because a helper named `Value` is shadowed by the `Value` property the interface mandates on every implementor — the unqualified call would never compile at its intended call site.)
- Inherits `IComparable<TSelf>`, `IEquatable<TSelf>`, `IComparisonOperators<TSelf, TSelf, bool>`.

`IStringConvertible<T>` and `StringConvertible<T>` (JSON converter) are slated for removal.

## Interpretation
Every value-type wrapper in Kingo will eventually implement `IValue<TSelf, TValue>`. The new contract is strictly richer (carries trust-path semantics, integrates with BCL parser infrastructure, brings full comparison surface) and drops the throw-on-`Empty` smell — types without a meaningful zero simply don't gain one.

## Next
`NamespaceIdentifier` and `RelationIdentifier` already implement `IValue<TSelf, TValue>` but with hand-rolled `TryParse` bodies that predate the `Value` sibling helper. Migrate them to the one-line delegation form so the parse-to-`bool`+`out` projection lives in one place across every wrapper.

Rewrite each value-type wrapper:
- Drop `[JsonConverter]` attribute (see [move-jsonconverter-off-identifier-types-into-the-json-adapter](move-jsonconverter-off-identifier-types-into-the-json-adapter.md)).
- Drop `IStringConvertible<T>` implementation, `From(string)`, `Empty()`.
- Implement `IValue<TSelf, TValue>`:
  - Expose `Value` as a public property.
  - Implement `Create(TValue)` (trusted, no validation).
  - Implement `Parse(string) → Result<TSelf>` (validation lives here).
  - Declare `TryParse` as a one-line delegation: `public static bool TryParse(string s, out FooId parsed) => ValueParser.TryParse<FooId, string>(s, out parsed);`. The declaration on the wrapper type is what reflection-based pipelines find. (No `[MaybeNullWhen(false)]` — `TSelf` is constrained to `struct`, so the `out` param is never null.)
  - Confirm `CompareTo`, `Equals`, `<`, `<=`, `>`, `>=` are present (record struct synthesizes `==`/`!=`).
- Move identifiers to `Kingo` (domain core) as part of [dissolve-kingo-pdl-under-hexagonal-layout](dissolve-kingo-pdl-under-hexagonal-layout.md).
- Delete `Kingo/IStringConvertible.cs` and `Kingo/Json/StringConvertible.cs` once nothing references them.
- Update ArchUnitNET tests: add a rule that every public `readonly record struct` under the domain namespace implements `IValue<TSelf, TValue>`.

If a wrapper has a meaningful zero (e.g., a numeric ID with `Zero = 0`), add a separate `IZero<TSelf>` interface and have it implement that on top of `IValue`. Don't pollute `IValue` with a throwing default.
