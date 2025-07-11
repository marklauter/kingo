namespace Kingo;

// it's like a resource indicator, but scheme is omitted because we know the context
// <policy-name>:<resource-id>
// eg: doc:readme.md
public sealed record Resource(PolicyName Namespace, Identifier Name);

