namespace Kingo.Graphs;

/// <summary>
/// A namespaced resource. The <c>&lt;resource&gt;</c> production of the fact grammar: <c>&lt;namespace&gt;:&lt;resource-id&gt;</c>, for example, <c>io/doc:readme</c>.
/// A value object of the fact context with no stored state of its own. A resource exists as the anchor facts attach to, and carries
/// <see cref="Namespace"/> as a reference-by-identity.
/// </summary>
public sealed record Resource(
    NamespacePath Namespace,
    ResourceId Id);
