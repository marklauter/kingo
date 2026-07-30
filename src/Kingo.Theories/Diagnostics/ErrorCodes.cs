using Results;

namespace Kingo.Theories.Diagnostics;

/// <summary>
/// The core-invariant validation failure codes emitted by the aggregate in <c>Kingo.Theories</c>. The code string is the wire contract; this class is the single
/// source for emission, grouped by the invariant that raises it, and the one place each literal is lifted into an <see cref="ErrorCode"/>. Tests pin the
/// literal values independently.
/// </summary>
public static class ErrorCodes
{
    public static class Theory
    {
        public static readonly ErrorCode Empty = ErrorCode.Unchecked("theory.empty");
        public static readonly ErrorCode DuplicateNamespace = ErrorCode.Unchecked("theory.duplicate_namespace");
    }

    public static class Namespace
    {
        public static readonly ErrorCode DuplicateRelationship = ErrorCode.Unchecked("namespace.duplicate_relationship");
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
