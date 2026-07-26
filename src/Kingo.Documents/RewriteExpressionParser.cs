using Kingo.Theories;
using Results;
using Superpower;
using Superpower.Display;
using Superpower.Parsers;
using Superpower.Tokenizers;
using System.Collections.Immutable;

namespace Kingo.Documents;

internal static class RewriteExpressionParser
{
    public static Result<SubjectSetRewrite> Parse(string expression)
    {
        var tokens = Tokenizer.TryTokenize(expression);
        if (!tokens.HasValue)
            return Result.Failure<SubjectSetRewrite>(Error.Validation(Diagnostics.ErrorCodes.Theory.Rewrite, $"invalid rewrite expression '{expression}': {tokens}"));

        if (WouldOverflowTheParserStack(tokens.Value))
            return Result.Failure<SubjectSetRewrite>(
                Error.Validation(Diagnostics.ErrorCodes.Theory.Rewrite, $"invalid rewrite expression '{expression}': parenthesis nesting exceeds {SubjectSetRewrite.MaxDepth} levels"));

        var parsed = Expression.AtEnd().TryParse(tokens.Value);
        return !parsed.HasValue
            ? Result.Failure<SubjectSetRewrite>(Error.Validation(Diagnostics.ErrorCodes.Theory.Rewrite, $"invalid rewrite expression '{expression}': {parsed}"))
            : ExceedsMaxDepth(parsed.Value)
                ? Result.Failure<SubjectSetRewrite>(SubjectSetRewrite.DepthError())
                : Transform(parsed.Value);
    }

    private static bool WouldOverflowTheParserStack(Superpower.Model.TokenList<RewriteExpressionToken> tokens)
    {
        var kinds = tokens.Select(token => token.Kind).ToArray();
        var depth = 0;
        for (var i = 0; i < kinds.Length; i++)
        {
            if (kinds[i] == RewriteExpressionToken.LeftParen)
            {
                if (IsFactsetShape(kinds, i))
                {
                    i += 4;
                    continue;
                }

                if (++depth > SubjectSetRewrite.MaxDepth)
                    return true;
            }
            else if (kinds[i] == RewriteExpressionToken.RightParen && depth > 0)
            {
                depth--;
            }
        }

        return false;
    }

    private static bool IsFactsetShape(RewriteExpressionToken[] kinds, int openParen) =>
        openParen + 4 < kinds.Length
        && kinds[openParen + 1] == RewriteExpressionToken.Identifier
        && kinds[openParen + 2] == RewriteExpressionToken.Comma
        && kinds[openParen + 3] == RewriteExpressionToken.Identifier
        && kinds[openParen + 4] == RewriteExpressionToken.RightParen;

    private static bool ExceedsMaxDepth(RewriteNode root)
    {
        var pending = new Stack<(RewriteNode Node, int Depth)>();
        pending.Push((root, 1));
        while (pending.Count > 0)
        {
            var (node, depth) = pending.Pop();
            if (depth > SubjectSetRewrite.MaxDepth)
                return true;

            switch (node)
            {
                case RewriteNode.Union union:
                    foreach (var child in union.Children)
                        pending.Push((child, depth + 1));
                    break;
                case RewriteNode.Intersection intersection:
                    foreach (var child in intersection.Children)
                        pending.Push((child, depth + 1));
                    break;
                case RewriteNode.Exclusion exclusion:
                    pending.Push((exclusion.Include, depth + 1));
                    pending.Push((exclusion.Exclude, depth + 1));
                    break;
                default:
                    break;
            }
        }

        return false;
    }

    private static Result<SubjectSetRewrite> Transform(RewriteNode node) =>
        node switch
        {
            RewriteNode.This => Result.Success<SubjectSetRewrite>(SubjectSetRewrite.This.Default),
            RewriteNode.ComputedSubjectSet computed => RelationName.Parse(computed.Relation)
                .Map(SubjectSetRewrite (relation) => SubjectSetRewrite.ComputedSubjectSet.Create(relation)),
            RewriteNode.FactToSubjectSet factTo => Result.Apply(
                RelationName.Parse(factTo.FactsetRelation)
                    .Map(Func<RelationName, SubjectSetRewrite> (factset) => computed => SubjectSetRewrite.FactToSubjectSet.Create(factset, computed)),
                RelationName.Parse(factTo.ComputedSubjectSetRelation)),
            RewriteNode.Union union => union.Children.Select(Transform).Sequence()
                .Bind(children => SubjectSetRewrite.Union.Create(children).Map(SubjectSetRewrite (rewrite) => rewrite)),
            RewriteNode.Intersection intersection => intersection.Children.Select(Transform).Sequence()
                .Bind(children => SubjectSetRewrite.Intersection.Create(children).Map(SubjectSetRewrite (rewrite) => rewrite)),
            _ => TransformExclusion((RewriteNode.Exclusion)node),
        };

    private static Result<SubjectSetRewrite> TransformExclusion(RewriteNode.Exclusion exclusion) =>
        Result.Apply(
            Transform(exclusion.Include)
                .Map(Func<SubjectSetRewrite, (SubjectSetRewrite Include, SubjectSetRewrite Exclude)> (include) => exclude => (include, exclude)),
            Transform(exclusion.Exclude))
        .Bind(operands => SubjectSetRewrite.Exclusion.Create(operands.Include, operands.Exclude).Map(SubjectSetRewrite (rewrite) => rewrite));

    private enum RewriteExpressionToken
    {
        None,

        [Token(Category = "identifier", Example = "myRelation")]
        Identifier,

        [Token(Category = "keyword", Example = "this")]
        This,

        [Token(Category = "operator", Example = "|")]
        Union,

        [Token(Category = "operator", Example = "&")]
        Intersection,

        [Token(Category = "operator", Example = "!")]
        Exclusion,

        [Token(Category = "delimiter", Example = "(")]
        LeftParen,

        [Token(Category = "delimiter", Example = ")")]
        RightParen,

        [Token(Category = "delimiter", Example = ",")]
        Comma,
    }

    private static readonly Tokenizer<RewriteExpressionToken> Tokenizer =
        new TokenizerBuilder<RewriteExpressionToken>()
            .Ignore(Span.WhiteSpace)
            .Ignore(Comment.ShellStyle)
            .Match(Character.EqualTo('('), RewriteExpressionToken.LeftParen)
            .Match(Character.EqualTo(')'), RewriteExpressionToken.RightParen)
            .Match(Character.EqualTo(','), RewriteExpressionToken.Comma)
            .Match(Character.EqualTo('|'), RewriteExpressionToken.Union)
            .Match(Character.EqualTo('&'), RewriteExpressionToken.Intersection)
            .Match(Character.EqualTo('!'), RewriteExpressionToken.Exclusion)
            .Match(Span.EqualToIgnoreCase("this"), RewriteExpressionToken.This, requireDelimiters: true)
            .Match(Superpower.Parsers.Identifier.CStyle, RewriteExpressionToken.Identifier, requireDelimiters: true)
            .Build();

    private static readonly TokenListParser<RewriteExpressionToken, RewriteNode> Expression = BuildExpressionParser();

    private static TokenListParser<RewriteExpressionToken, RewriteNode> BuildExpressionParser()
    {
        var identifier = Token.EqualTo(RewriteExpressionToken.Identifier).Select(token => token.ToStringValue());

        var thisTerm = Token.EqualTo(RewriteExpressionToken.This).Select(_ => (RewriteNode)RewriteNode.This.Instance);

        var computed = identifier.Select(name => (RewriteNode)new RewriteNode.ComputedSubjectSet(name));

        var factToSubjectSet =
            from lparen in Token.EqualTo(RewriteExpressionToken.LeftParen)
            from factset in identifier
            from comma in Token.EqualTo(RewriteExpressionToken.Comma)
            from computedSubjectSetRelation in identifier
            from rparen in Token.EqualTo(RewriteExpressionToken.RightParen)
            select (RewriteNode)new RewriteNode.FactToSubjectSet(factset, computedSubjectSetRelation);

        TokenListParser<RewriteExpressionToken, RewriteNode>? expressionRef = null;
        var term = factToSubjectSet.Try().Or(thisTerm.Try()).Or(computed)
            .Or(Superpower.Parse.Ref(() => expressionRef!)
                .Between(Token.EqualTo(RewriteExpressionToken.LeftParen), Token.EqualTo(RewriteExpressionToken.RightParen)));

        var exclusion =
            from include in term
            from excludes in Token.EqualTo(RewriteExpressionToken.Exclusion).IgnoreThen(term).Many()
            select excludes.Aggregate(include, (accumulated, exclude) => new RewriteNode.Exclusion(accumulated, exclude));

        var intersection =
            from first in exclusion
            from rest in Token.EqualTo(RewriteExpressionToken.Intersection).IgnoreThen(exclusion).Many()
            select Chain(first, rest, static children => new RewriteNode.Intersection(children));

        expressionRef =
            from first in intersection
            from rest in Token.EqualTo(RewriteExpressionToken.Union).IgnoreThen(intersection).Many()
            select Chain(first, rest, static children => new RewriteNode.Union(children));

        return expressionRef;
    }

    private static RewriteNode Chain(RewriteNode first, RewriteNode[] rest, Func<ImmutableArray<RewriteNode>, RewriteNode> materialize) =>
        rest.Length == 0 ? first : materialize([first, .. rest]);
}
