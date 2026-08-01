using Results;

namespace Kingo.Documents.Diagnostics;

public static class ErrorCodes
{
    public static class Theory
    {
        public static readonly ErrorCode Document = ErrorCode.Unchecked("theory.document");
        public static readonly ErrorCode Syntax = ErrorCode.Unchecked("theory.syntax");
        public static readonly ErrorCode Namespace = ErrorCode.Unchecked("theory.namespace");
        public static readonly ErrorCode Relation = ErrorCode.Unchecked("theory.relation");
        public static readonly ErrorCode RelationReserved = ErrorCode.Unchecked("theory.relation.reserved");
        public static readonly ErrorCode Rewrite = ErrorCode.Unchecked("theory.rewrite");
    }
}
