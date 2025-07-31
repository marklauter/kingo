using LanguageExt;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace Kingo.Policies.Yaml;

internal static class RewriteExpressionParser
{
    public static Eff<SubjectSetRewrite> Parse(string pdl) =>
        RewriteExpressionTokenizer
            .Tokenize(pdl)
            .Bind(Parse);

    private static Eff<SubjectSetRewrite> Parse(TokenList<RewriteExpressionToken> input)
    {
        var parseResult = RewriteExpression.AtEnd().TryParse(input);
        return parseResult.HasValue
            ? Prelude.Pure(parseResult.Value)
            : ParseError.New(ParseErrorCodes.ParseEerror, $"parse error: {parseResult}");
    }

    private static readonly TokenListParser<RewriteExpressionToken, SubjectSetRewrite> RewriteExpression;

    static RewriteExpressionParser()
    {
        var identifier = Token.EqualTo(RewriteExpressionToken.Identifier).Select(token => token.ToStringValue());

        var directTerm = Token.EqualTo(RewriteExpressionToken.This).Select(_ => (SubjectSetRewrite)DirectRewrite.Default);

        var computedSubjectSet = identifier.Select(name => (SubjectSetRewrite)new ComputedSubjectSetRewrite(RelationIdentifier.From(name)));

        var tupleToSubjectSet =
            from lparen in Token.EqualTo(RewriteExpressionToken.LeftParen)
            from name in identifier
            from comma in Token.EqualTo(RewriteExpressionToken.Comma)
            from mapsTo in identifier
            from rparen in Token.EqualTo(RewriteExpressionToken.RightParen)
            select (SubjectSetRewrite)new TupleToSubjectSetRewrite(RelationIdentifier.From(name), RelationIdentifier.From(mapsTo));

        var term = tupleToSubjectSet.Try().Or(directTerm.Try()).Or(computedSubjectSet)
            .Or(Superpower.Parse.Ref(() => RewriteExpression!
                .Between(Token.EqualTo(RewriteExpressionToken.LeftParen), Token.EqualTo(RewriteExpressionToken.RightParen))));

        var exclusion =
            from include in term
            from exclude in Token.EqualTo(RewriteExpressionToken.Exclusion).IgnoreThen(term).Many()
            select exclude.Aggregate(include, (acc, ex) => new ExclusionRewrite(acc, ex));

        RewriteExpression =
            from first in exclusion
            from rest in Token.EqualTo(RewriteExpressionToken.Intersection).Or(Token.EqualTo(RewriteExpressionToken.Union))
                .Then(op => exclusion.Select(right => (op, right)))
                .Many()
            select rest.Aggregate(first, (left, item) => CreateBinaryRewrite(left, item.op.Kind, item.right));
    }

    private static SubjectSetRewrite CreateBinaryRewrite(SubjectSetRewrite left, RewriteExpressionToken op, SubjectSetRewrite right) =>
        op == RewriteExpressionToken.Union
            ? left is UnionRewrite union
                ? new UnionRewrite(union.Children.Add(right))
                : new UnionRewrite([left, right])
            : left is IntersectionRewrite intersection
                ? new IntersectionRewrite(intersection.Children.Add(right))
                : new IntersectionRewrite([left, right]);
}
