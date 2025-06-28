using Kingo.Facts;

namespace Kingo.Specifications;

public sealed record NamespaceSpec(
    Namespace Name,
    IReadOnlyList<RelationshipSpec> Relationships);
