# Move JsonConverter off identifier types into the JSON adapter

Tags: todo,hexagonal
Domain types currently carry [JsonConverter] attributes — a leak under hexagonal layering. Move converter registration to the JSON adapter at the JsonSerializerOptions level.

## Observation
`NamespaceIdentifier` and `RelationIdentifier` (currently in `Kingo.Pdl`) each carry:

```csharp
[JsonConverter(typeof(StringConvertible<NamespaceIdentifier>))]
[JsonConverter(typeof(StringConvertible<RelationIdentifier>))]
```

`StringConvertible<T>` is the JSON converter at `Kingo/Json/StringConvertible.cs`. `IStringConvertible<T>` is the CRTP contract these wrappers implement.

## Interpretation
The attribute sits on the domain type, declaring *how the type is serialized*. That couples domain to serialization — exactly the leak writing-csharp warns against ("the domain doesn't know how it's stored," generalized to "doesn't know how it's serialized"). Identifier types should be pure domain primitives; the JSON-format opinion belongs at the adapter boundary.

## Next
- During the planned identifier rewrite, drop `[JsonConverter]` from `NamespaceIdentifier`, `RelationIdentifier`, and any future value-type wrapper.
- Register converters at the `JsonSerializerOptions` level inside `Kingo.Serialization.Json` so the adapter owns the mapping.
- `IStringConvertible<T>` is being phased out alongside this work; the replacement (`IValue<TSelf, TValue>`) and its JSON adapter ship together.
- Decide whether the adapter exposes a `JsonSerializerOptions` factory (`KingoJsonOptions.Default`) or registers types via attributes-on-the-options.

Coordinated with [dissolve-kingo-pdl-under-hexagonal-layout](dissolve-kingo-pdl-under-hexagonal-layout.md).
