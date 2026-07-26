using Kingo.Theories;

namespace Kingo.Documents;

internal static class RewriteExpressionPrinter
{
    public static bool IsReserved(RelationName relation) =>
        string.Equals(relation.Value, "this", StringComparison.OrdinalIgnoreCase);

    public static string Print(SubjectSetRewrite rewrite) =>
        rewrite switch
        {
            SubjectSetRewrite.This => "this",
            SubjectSetRewrite.ComputedSubjectSet computed => PrintIdentifier(computed.Relation),
            SubjectSetRewrite.FactToSubjectSet factTo => $"({PrintIdentifier(factTo.FactsetRelation)}, {PrintIdentifier(factTo.ComputedSubjectSetRelation)})",
            SubjectSetRewrite.Union union => string.Join(" | ", union.Children.Select(PrintUnionOperand)),
            SubjectSetRewrite.Intersection intersection => string.Join(" & ", intersection.Children.Select(PrintExclusionOperand)),
            _ => PrintExclusion((SubjectSetRewrite.Exclusion)rewrite),
        };

    private static string PrintExclusion(SubjectSetRewrite.Exclusion exclusion) =>
        $"{PrintExclusionOperand(exclusion.Include)} ! {PrintTerm(exclusion.Exclude)}";

    private static string PrintIdentifier(RelationName relation) =>
        IsReserved(relation)
            ? throw new ArgumentException($"relation '{relation}' cannot be referenced in a rewrite expression: 'this' is reserved by the grammar")
            : relation.Value;

    private static string PrintUnionOperand(SubjectSetRewrite rewrite) =>
        rewrite is SubjectSetRewrite.Union ? $"({Print(rewrite)})" : Print(rewrite);

    private static string PrintExclusionOperand(SubjectSetRewrite rewrite) =>
        rewrite is SubjectSetRewrite.Union or SubjectSetRewrite.Intersection ? $"({Print(rewrite)})" : Print(rewrite);

    private static string PrintTerm(SubjectSetRewrite rewrite) =>
        rewrite is SubjectSetRewrite.Union or SubjectSetRewrite.Intersection or SubjectSetRewrite.Exclusion ? $"({Print(rewrite)})" : Print(rewrite);
}
