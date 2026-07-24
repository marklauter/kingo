using Kingo.Schemas;

namespace Kingo.Sdl;

/// <summary>
/// Prints a <c>SubjectSetRewrite</c> tree as rewrite-expression text (grammar: [[specs]]). Parenthesization is decided by grammar position so the
/// emitted text reparses to a structurally equal tree. The grammar is the precedence cascade <c>union → intersection → exclusion → term</c>, and each position
/// takes exactly what its level admits: a union's operand is an <c>&lt;intersection&gt;</c>, so only a nested union needs parentheses; an intersection's
/// operand and the include side of <c>!</c> are an <c>&lt;exclusion&gt;</c>, so a union or a nested intersection needs them; the exclude side of <c>!</c> is a
/// <c>&lt;term&gt;</c>, so any compound there needs them. Rewrites
/// referencing the reserved relationship name (<see cref="IsReserved"/>) cannot be expressed and throw; the one degenerate tree the grammar cannot express —
/// a single-child union/intersection (the empty shape is unrepresentable; its <c>Create</c> refuses it) — renders as its child and reparses to the simpler
/// shape; constructing it is the caller's defect.
/// </summary>
internal static class RewriteExpressionPrinter
{
    /// <summary>
    /// The rewrite grammar's reserved word: <c>this</c> always lexes as the direct-membership keyword — emitting it as an identifier would silently reparse a
    /// computed reference into <c>SubjectSetRewrite.This</c>. Case-insensitive because the tokenizer matches the keyword case-insensitively while <c>Unchecked</c> performs
    /// no normalization. (<c>...</c> is no longer here: it is not a relationship — it is the <c>#...</c> marker of the <c>Fact.ResourceFact</c> member
    /// production — so it cannot be a <see cref="RelationshipName"/> at all.) Reservation is on names, not paths: the grammar only ever writes
    /// bare names.
    /// </summary>
    public static bool IsReserved(RelationshipName relationship) =>
        string.Equals(relationship.Value, "this", StringComparison.OrdinalIgnoreCase);

    public static string Print(SubjectSetRewrite rewrite) =>
        rewrite switch
        {
            SubjectSetRewrite.This => "this",
            SubjectSetRewrite.ComputedSubjectSet computed => PrintIdentifier(computed.Relationship),
            SubjectSetRewrite.FactToSubjectSet factTo => $"({PrintIdentifier(factTo.FactsetRelationship)}, {PrintIdentifier(factTo.ComputedSubjectSetRelationship)})",
            SubjectSetRewrite.Union union => string.Join(" | ", union.Children.Select(PrintUnionOperand)),
            SubjectSetRewrite.Intersection intersection => string.Join(" & ", intersection.Children.Select(PrintExclusionOperand)),
            // the last inhabitant of the closed hierarchy: a discard arm (rather than a type pattern)
            // keeps the compiler from synthesizing an unreachable default branch under the switch
            _ => PrintExclusion((SubjectSetRewrite.Exclusion)rewrite),
        };

    private static string PrintExclusion(SubjectSetRewrite.Exclusion exclusion) =>
        $"{PrintExclusionOperand(exclusion.Include)} ! {PrintTerm(exclusion.Exclude)}";

    private static string PrintIdentifier(RelationshipName relationship) =>
        IsReserved(relationship)
            ? throw new ArgumentException($"relationship '{relationship}' cannot be referenced in a SDL rewrite expression: 'this' is reserved by the grammar")
            : relationship.Value;

    /// <summary>
    /// An operand of <c>|</c> sits at the <c>&lt;intersection&gt;</c> level: because <c>&amp;</c> binds tighter, an intersection renders bare there and
    /// reparses as the same child. Only a nested union needs parentheses — the chain would otherwise absorb it into one n-ary node.
    /// </summary>
    private static string PrintUnionOperand(SubjectSetRewrite rewrite) =>
        rewrite is SubjectSetRewrite.Union ? $"({Print(rewrite)})" : Print(rewrite);

    /// <summary>
    /// An operand of <c>&amp;</c>, and the include side of <c>!</c>, sit at the <c>&lt;exclusion&gt;</c> level: a union would regroup and a nested intersection
    /// would be absorbed, so both need parentheses. An exclusion renders bare — it is the level itself on the include side, where the left fold restores the
    /// nesting, and it binds tighter than <c>&amp;</c> as an operand.
    /// </summary>
    private static string PrintExclusionOperand(SubjectSetRewrite rewrite) =>
        rewrite is SubjectSetRewrite.Union or SubjectSetRewrite.Intersection ? $"({Print(rewrite)})" : Print(rewrite);

    /// <summary>The exclude side of <c>!</c> is a <c>&lt;term&gt;</c>: any compound — including a nested exclusion — needs parentheses.</summary>
    private static string PrintTerm(SubjectSetRewrite rewrite) =>
        rewrite is SubjectSetRewrite.Union or SubjectSetRewrite.Intersection or SubjectSetRewrite.Exclusion ? $"({Print(rewrite)})" : Print(rewrite);
}
