using Results;

namespace Kingo.Diagnostics;

/// <summary>
/// The identifier-grammar validation failure codes emitted by the value types in <c>Kingo</c>. The code string is the wire contract; this class is the single
/// source for emission, grouped by the type that raises it, and the one place each literal is lifted into an <see cref="ErrorCode"/>. Tests pin the literal
/// values independently.
/// </summary>
public static class ErrorCodes
{
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

    public static class RelationshipName
    {
        public static readonly ErrorCode Empty = ErrorCode.Unchecked("relationship_name.empty");
        public static readonly ErrorCode Invalid = ErrorCode.Unchecked("relationship_name.invalid");
    }

    public static class ResourceId
    {
        public static readonly ErrorCode Empty = ErrorCode.Unchecked("resource_id.empty");
        public static readonly ErrorCode Invalid = ErrorCode.Unchecked("resource_id.invalid");
    }

    public static class SubjectId
    {
        public static readonly ErrorCode Empty = ErrorCode.Unchecked("subject_id.empty");
        public static readonly ErrorCode Invalid = ErrorCode.Unchecked("subject_id.invalid");
    }

    public static class TheoryName
    {
        public static readonly ErrorCode Empty = ErrorCode.Unchecked("theory_name.empty");
        public static readonly ErrorCode Invalid = ErrorCode.Unchecked("theory_name.invalid");
    }
}
