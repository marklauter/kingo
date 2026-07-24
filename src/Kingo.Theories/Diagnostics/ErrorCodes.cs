namespace Kingo.Theories.Diagnostics;

/// <summary>
/// The core-invariant validation failure codes emitted by the aggregate in <c>Kingo.Theories</c>. The code string is the wire contract; this class is the single
/// source for emission, grouped by the invariant that raises it. Tests pin the literal values independently.
/// </summary>
public static class ErrorCodes
{
    public static class Theory
    {
        public const string Empty = "theory.empty";
        public const string DuplicateNamespace = "theory.duplicate_namespace";
    }

    public static class Namespace
    {
        public const string DuplicateRelationship = "namespace.duplicate_relationship";
        public const string DanglingReference = "namespace.dangling_reference";
        public const string RewriteCycle = "namespace.rewrite_cycle";
    }

    public static class Rewrite
    {
        public const string Depth = "rewrite.depth";
        public const string UnionEmpty = "rewrite.union.empty";
        public const string IntersectionEmpty = "rewrite.intersection.empty";
    }
}
