namespace Kingo.Facts;

/// <summary>
/// An object a fact is about: a <see cref="NamespacePath"/> and a caller-owned <see cref="ResourceId"/>.
/// A value object of the fact context with no stored state of its own. A resource exists as the anchor facts attach to, and carries
/// <see cref="Namespace"/> as a reference-by-identity.
/// </summary>
public sealed record Resource(
    NamespacePath Namespace,
    ResourceId Id);
