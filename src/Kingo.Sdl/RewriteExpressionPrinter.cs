using Kingo.Domains;

namespace Kingo.Sdl;

/// <summary>
/// Renders a <c>SubjectSetRewrite</c> tree as rewrite-expression text (grammar: [[specs]]). Parenthesization is decided by grammar position,
/// so the emitted text reparses to a structurally equal tree. The grammar is the precedence cascade <c>union → intersection → exclusion →
/// term</c>, and each position takes exactly what its level admits. A union's operand is an <c>&lt;intersection&gt;</c>, so only a nested
/// union needs parentheses. An intersection's operand and the include side of <c>!</c> are an <c>&lt;exclusion&gt;</c>, so a union or a
/// nested intersection needs them. The exclude side of <c>!</c> is a <c>&lt;term&gt;</c>, so any compound there needs them. A single-child
/// union or intersection is the one degenerate tree the grammar cannot express (the empty shape is unrepresentable, and its <c>Create</c>
/// refuses it). It renders as its child and reparses to the simpler shape, and constructing it is the caller's defect.
/// </summary>
internal static class RewriteExpressionPrinter
{
    /// <summary>
    /// Reports whether <paramref name="relationship"/> is the rewrite grammar's reserved word. <c>this</c> always lexes as the
    /// direct-membership keyword, so emitting it as an identifier would silently reparse a computed reference into
    /// <c>SubjectSetRewrite.This</c>. The comparison is case-insensitive because the tokenizer matches the keyword case-insensitively while
    /// <c>Unchecked</c> performs no normalization. Reservation is on names, not paths, because the grammar only ever writes bare names.
    /// </summary>
    /// <returns><see langword="true"/> when <paramref name="relationship"/> is <c>this</c> (case-insensitive); otherwise <see langword="false"/>.</returns>
    public static bool IsReserved(RelationshipName relationship) =>
        string.Equals(relationship.Value, "this", StringComparison.OrdinalIgnoreCase);

    /// <summary>Renders <paramref name="rewrite"/> as rewrite-expression text.</summary>
    /// <returns>The rewrite-expression text that reparses to a structurally equal tree.</returns>
    /// <exception cref="ArgumentException"><paramref name="rewrite"/> references the reserved relationship name <c>this</c>, which cannot be expressed in a rewrite expression.</exception>
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
    /// Renders an operand of <c>|</c>, which sits at the <c>&lt;intersection&gt;</c> level. Because <c>&amp;</c> binds tighter, an intersection
    /// renders bare there and reparses as the same child. Only a nested union needs parentheses, which the chain would otherwise absorb into
    /// one n-ary node.
    /// </summary>
    /// <returns><paramref name="rewrite"/> as text, wrapped in parentheses when it is a nested union.</returns>
    private static string PrintUnionOperand(SubjectSetRewrite rewrite) =>
        rewrite is SubjectSetRewrite.Union ? $"({Print(rewrite)})" : Print(rewrite);

    /// <summary>
    /// Renders an operand of <c>&amp;</c> or the include side of <c>!</c>, which sit at the <c>&lt;exclusion&gt;</c> level. A union would
    /// regroup and a nested intersection would be absorbed, so both need parentheses. An exclusion renders bare, because it is the level
    /// itself on the include side where the left fold restores the nesting, and it binds tighter than <c>&amp;</c> as an operand.
    /// </summary>
    /// <returns><paramref name="rewrite"/> as text, wrapped in parentheses when it is a union or a nested intersection.</returns>
    private static string PrintExclusionOperand(SubjectSetRewrite rewrite) =>
        rewrite is SubjectSetRewrite.Union or SubjectSetRewrite.Intersection ? $"({Print(rewrite)})" : Print(rewrite);

    /// <summary>Renders the exclude side of <c>!</c>, which is a <c>&lt;term&gt;</c>. Any compound, including a nested exclusion, needs parentheses.</summary>
    /// <returns><paramref name="rewrite"/> as text, wrapped in parentheses when it is a union, intersection, or exclusion.</returns>
    private static string PrintTerm(SubjectSetRewrite rewrite) =>
        rewrite is SubjectSetRewrite.Union or SubjectSetRewrite.Intersection or SubjectSetRewrite.Exclusion ? $"({Print(rewrite)})" : Print(rewrite);
}
