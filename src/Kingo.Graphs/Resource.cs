namespace Kingo.Graphs;

/// <summary>
/// A namespaced resource — the <c>&lt;resource&gt;</c> production of the fact grammar: <c>&lt;namespace&gt;:&lt;resource-id&gt;</c> (e.g. <c>doc:readme</c>).
/// A value object of the fact context: no stored state of its own — a resource exists as the anchor facts attach to, and carries
/// <see cref="Namespace"/> as a reference-by-identity.
/// </summary>
public sealed record Resource(
    NamespacePath Namespace,
    ResourceId Id);
