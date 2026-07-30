using Results;

namespace Kingo.Documents.Diagnostics;

/// <summary>
/// The parse and print failure codes emitted by the theory document adapter in <c>Kingo.Documents</c>. The code string is the wire contract; this class is the
/// single source for emission, and the one place each literal is lifted into an <see cref="ErrorCode"/>. Tests pin the literal values independently.
/// </summary>
public static class ErrorCodes
{
    public static class Theory
    {
        public static readonly ErrorCode Document = ErrorCode.Unchecked("theory.document");
        public static readonly ErrorCode Syntax = ErrorCode.Unchecked("theory.syntax");
        public static readonly ErrorCode Namespace = ErrorCode.Unchecked("theory.namespace");
        public static readonly ErrorCode Relationship = ErrorCode.Unchecked("theory.relationship");
        public static readonly ErrorCode RelationshipReserved = ErrorCode.Unchecked("theory.relationship.reserved");
        public static readonly ErrorCode Rewrite = ErrorCode.Unchecked("theory.rewrite");
    }
}
