# Architecture

Tags: hexagonal,ddd
Kingo organizes as hexagonal with a DDD core: Kingo holds domain types, Kingo.Serialization defines ports, Kingo.Serialization.{Json,Yaml,...} are adapters.

## Organization

The project follows hexagonal architecture with a DDD core at the center. Projects layer outward from pure domain to concrete I/O.

### Domain core — `Kingo`

The center. Pure types describing the ubiquitous domain: namespaces, relations, identifiers, the subject-set rewrite AST, foundational primitives like `Result<T>` / `Error` / `IValue<TSelf, TValue>`. No knowledge of how anything is persisted, serialized, transported, rendered, or authenticated.

Today, domain types are distributed across `Kingo` and `Kingo.Pdl`; consolidation into `Kingo` is tracked by [dissolve-kingo-pdl-under-hexagonal-layout](dissolve-kingo-pdl-under-hexagonal-layout.md).

### Ports — `Kingo.Serialization`, future `Kingo.Storage`, etc.

Interfaces that describe what the core needs from the outside world, without specifying how. A port says "give me something that can deserialize a policy from a string"; it does not say "give me a YamlDotNet deserializer." Ports live close enough to the core that they share its language; the implementations live elsewhere.

### Adapters — `Kingo.Serialization.Json`, `Kingo.Serialization.Yaml`, `Kingo.Serialization.Pdl`, future storage adapters, transport adapters, etc.

Concrete implementations of the ports, using whichever third-party library or platform is appropriate. Adapters know about YamlDotNet, System.Text.Json, DynamoDbLite, ASP.NET Core. Domain code never directly references them; it talks to the port.

## Principles

- **The domain doesn't know how it's stored.** No serialization attributes, ORM annotations, or framework references on domain types. The mapping happens at the adapter boundary.
- **Ports speak the domain's language.** A port interface uses domain types as parameters and returns; it does not leak adapter-specific concepts (no `JsonElement`, no `YamlNode`) into the core.
- **Adapters are swappable in principle.** The system should function with any conforming implementation of a port. In practice this is most visible at test time, where in-memory or fake adapters substitute for the production ones.
- **First slice sets the layer.** When a new layer appears (the first storage adapter, the first transport adapter), the structural rules for that layer are encoded immediately — naming, project shape, ArchUnit rules. The first example teaches by existing.

## Influences

- Evans, *Domain-Driven Design* — the ubiquitous language, the model as the heart of the application.
- Cockburn, "Hexagonal Architecture" — ports and adapters as the substrate-independence pattern.
- Writing-csharp principles — particularly "The domain doesn't know how it's stored" and "Make invalid states unrepresentable."

## Open threads
- [dissolve-kingo-pdl-under-hexagonal-layout](dissolve-kingo-pdl-under-hexagonal-layout.md)
- [move-jsonconverter-off-identifier-types-into-the-json-adapter](move-jsonconverter-off-identifier-types-into-the-json-adapter.md)
- [ivalue-tself-tvalue-absorbs-all-value-type-wrappers](ivalue-tself-tvalue-absorbs-all-value-type-wrappers.md)
