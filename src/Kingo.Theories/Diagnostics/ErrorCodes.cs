namespace Kingo.Theories.Diagnostics;

public static class ErrorCodes
{
    public static class Theory
    {
        public const string Empty = "theory.empty";
        public const string DuplicateNamespace = "theory.duplicate_namespace";
    }

    public static class Namespace
    {
        public const string DuplicateRelation = "namespace.duplicate_relation";
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
