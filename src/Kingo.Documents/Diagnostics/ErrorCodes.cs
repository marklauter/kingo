namespace Kingo.Documents.Diagnostics;

/// <summary>
/// The parse and print failure codes emitted by the theory document adapter in <c>Kingo.Documents</c>. The code string is the wire contract; this class is the
/// single source for emission. Tests pin the literal values independently.
/// </summary>
public static class ErrorCodes
{
    public static class Theory
    {
        public const string Document = "theory.document";
        public const string Syntax = "theory.syntax";
        public const string Namespace = "theory.namespace";
        public const string Relationship = "theory.relationship";
        public const string RelationshipReserved = "theory.relationship.reserved";
        public const string Rewrite = "theory.rewrite";
    }
}
