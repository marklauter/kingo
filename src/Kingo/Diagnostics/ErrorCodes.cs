namespace Kingo.Diagnostics;

/// <summary>
/// The identifier-grammar validation failure codes emitted by the value types in <c>Kingo</c>. The code string is the wire contract; this class is the single
/// source for emission, grouped by the type that raises it. Tests pin the literal values independently.
/// </summary>
public static class ErrorCodes
{
    public static class NamespaceName
    {
        public const string Empty = "namespace_name.empty";
        public const string Invalid = "namespace_name.invalid";
    }

    public static class NamespacePath
    {
        public const string Empty = "namespace_path.empty";
        public const string Invalid = "namespace_path.invalid";
    }

    public static class RelationName
    {
        public const string Empty = "relation_name.empty";
        public const string Invalid = "relation_name.invalid";
    }

    public static class ResourceId
    {
        public const string Empty = "resource_id.empty";
        public const string Invalid = "resource_id.invalid";
    }

    public static class SubjectId
    {
        public const string Empty = "subject_id.empty";
        public const string Invalid = "subject_id.invalid";
    }

    public static class TheoryName
    {
        public const string Empty = "theory_name.empty";
        public const string Invalid = "theory_name.invalid";
    }
}
