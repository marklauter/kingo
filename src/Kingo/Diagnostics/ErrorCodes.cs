using Results;

namespace Kingo.Diagnostics;

public static class ErrorCodes
{
    public static class Identity
    {
        public static readonly ErrorCode Empty = ErrorCode.Unchecked("identity.empty");
        public static readonly ErrorCode Invalid = ErrorCode.Unchecked("identity.invalid");
    }

    public static class NamespaceName
    {
        public static readonly ErrorCode Empty = ErrorCode.Unchecked("namespace_name.empty");
        public static readonly ErrorCode Invalid = ErrorCode.Unchecked("namespace_name.invalid");
    }

    public static class NamespacePath
    {
        public static readonly ErrorCode Empty = ErrorCode.Unchecked("namespace_path.empty");
        public static readonly ErrorCode Invalid = ErrorCode.Unchecked("namespace_path.invalid");
    }

    public static class RelationName
    {
        public static readonly ErrorCode Empty = ErrorCode.Unchecked("relation_name.empty");
        public static readonly ErrorCode Invalid = ErrorCode.Unchecked("relation_name.invalid");
    }

    public static class ResourceId
    {
        public static readonly ErrorCode Empty = ErrorCode.Unchecked("resource_id.empty");
        public static readonly ErrorCode Invalid = ErrorCode.Unchecked("resource_id.invalid");
    }

    public static class TheoryName
    {
        public static readonly ErrorCode Empty = ErrorCode.Unchecked("theory_name.empty");
        public static readonly ErrorCode Invalid = ErrorCode.Unchecked("theory_name.invalid");
    }
}
