using Kingo.Primitives;

namespace Kingo.Facts;

// it's like a resource indicator, but scheme is omitted because we know the context
// <namespace>:<resource-id>
// eg: doc:readme.md
public sealed record Resource(Identifier Namespace, Identifier Name);
