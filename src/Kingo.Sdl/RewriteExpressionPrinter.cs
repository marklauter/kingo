using Kingo.Schemas;

namespace Kingo.Sdl;

/// <summary>
/// Prints a <c>SubjectSetRewrite</c> tree as rewrite-expression text (grammar: docs/notes/sdl-yaml.md). Parenthesization is decided by grammar position so the
/// emitted text reparses to a structurally equal tree: a union/intersection operand that is itself a union/intersection is parenthesized (the operator chain
/// would otherwise absorb or regroup it), and the exclude side of <c>!</c> is a <c>&lt;term&gt;</c>, so any compound there is parenthesized. Rewrites
/// referencing a reserved relationship name (<see cref="IsReserved"/>) cannot be expressed and throw; degenerate trees the grammar cannot express — a
/// union/intersection with zero or one child — render as their children and reparse to the simpler shape; constructing either is the caller's defect.
/// </summary>
internal static class RewriteExpressionPrinter
{
    /// <summary>
    /// The rewrite grammar's reserved words: <c>this</c> always lexes as the direct-membership keyword — emitting it as an identifier would silently reparse a
    /// computed reference into <c>ThisRewrite</c> — and the <c>...</c> sentinel cannot lex at all. Case-insensitive because the tokenizer matches the keyword
    /// case-insensitively while <c>Create</c> performs no normalization.
    /// </summary>
    public static bool IsReserved(RelationshipIdentifier relationship) =>
        relationship == RelationshipIdentifier.Nothing
        || string.Equals(relationship.Value, "this", StringComparison.OrdinalIgnoreCase);

    public static string Print(SubjectSetRewrite rewrite) =>
        rewrite switch
        {
            ThisRewrite => "this",
            ComputedSubjectSetRewrite computed => PrintIdentifier(computed.Relationship),
            TupleToSubjectSetRewrite tupleTo => $"({PrintIdentifier(tupleTo.TuplesetRelationship)}, {PrintIdentifier(tupleTo.ComputedRelationship)})",
            UnionRewrite union => string.Join(" | ", union.Children.Select(PrintOperand)),
            IntersectionRewrite intersection => string.Join(" & ", intersection.Children.Select(PrintOperand)),
            // the last inhabitant of the closed hierarchy: a discard arm (rather than a type pattern)
            // keeps the compiler from synthesizing an unreachable default branch under the switch
            _ => PrintExclusion((ExclusionRewrite)rewrite),
        };

    private static string PrintExclusion(ExclusionRewrite exclusion) =>
        $"{PrintOperand(exclusion.Include)} ! {PrintTerm(exclusion.Exclude)}";

    private static string PrintIdentifier(RelationshipIdentifier relationship) =>
        IsReserved(relationship)
            ? throw new ArgumentException($"relationship '{relationship}' cannot be referenced in a SDL rewrite expression: 'this' and '{RelationshipIdentifier.Nothing}' are reserved by the grammar")
            : relationship.Value;

    /// <summary>
    /// An operand of <c>|</c>/<c>&amp;</c> or the include side of <c>!</c> sits at the <c>&lt;exclusion&gt;</c> level: unions and intersections need
    /// parentheses; exclusions and atoms render bare.
    /// </summary>
    private static string PrintOperand(SubjectSetRewrite rewrite) =>
        rewrite is UnionRewrite or IntersectionRewrite ? $"({Print(rewrite)})" : Print(rewrite);

    /// <summary>The exclude side of <c>!</c> is a <c>&lt;term&gt;</c>: any compound — including a nested exclusion — needs parentheses.</summary>
    private static string PrintTerm(SubjectSetRewrite rewrite) =>
        rewrite is UnionRewrite or IntersectionRewrite or ExclusionRewrite ? $"({Print(rewrite)})" : Print(rewrite);
}
