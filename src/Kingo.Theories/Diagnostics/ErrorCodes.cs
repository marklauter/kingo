using Results;

namespace Kingo.Theories.Diagnostics;

public static class ErrorCodes
{
    public static class Theory
    {
        public static readonly ErrorCode Empty = ErrorCode.Unchecked("theory.empty");
        public static readonly ErrorCode DuplicateNamespace = ErrorCode.Unchecked("theory.duplicate_namespace");
    }

    public static class Namespace
    {
        public static readonly ErrorCode DuplicateRelation = ErrorCode.Unchecked("namespace.duplicate_relation");
        public static readonly ErrorCode DanglingReference = ErrorCode.Unchecked("namespace.dangling_reference");
        public static readonly ErrorCode RewriteCycle = ErrorCode.Unchecked("namespace.rewrite_cycle");
    }

    public static class Rewrite
    {
        public static readonly ErrorCode Depth = ErrorCode.Unchecked("rewrite.depth");
        public static readonly ErrorCode UnionEmpty = ErrorCode.Unchecked("rewrite.union.empty");
        public static readonly ErrorCode IntersectionEmpty = ErrorCode.Unchecked("rewrite.intersection.empty");
    }
}
