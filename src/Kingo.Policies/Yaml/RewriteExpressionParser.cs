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
        // do not check parseResult.Value is not null because for the love of God, HasValue already does that
        return parseResult.HasValue
            ? Prelude.Pure(parseResult.Value)
            : ParseError.New(ParseErrorCodes.ParseEerror, $"parse error: {parseResult}");
    }

    private static readonly TokenListParser<RewriteExpressionToken, string> Identifier;
    private static readonly TokenListParser<RewriteExpressionToken, SubjectSetRewrite> DirectTerm;
    private static readonly TokenListParser<RewriteExpressionToken, SubjectSetRewrite> ComputedSubjectSetRewriteParser;
    private static readonly TokenListParser<RewriteExpressionToken, SubjectSetRewrite> TupleToSubjectSetRewriteParser;
    private static readonly TokenListParser<RewriteExpressionToken, SubjectSetRewrite> Term;
    private static readonly TokenListParser<RewriteExpressionToken, SubjectSetRewrite> Exclusion;
    private static readonly TokenListParser<RewriteExpressionToken, SubjectSetRewrite> Intersection;
    private static readonly TokenListParser<RewriteExpressionToken, SubjectSetRewrite> RewriteExpression;

    static RewriteExpressionParser()
    {
        Identifier =
            Token.EqualTo(RewriteExpressionToken.Identifier).Select(token => token.ToStringValue());

        DirectTerm =
            Token.EqualTo(RewriteExpressionToken.This).Select(_ => (SubjectSetRewrite)DirectRewrite.Default);

        ComputedSubjectSetRewriteParser =
            Identifier.Select(name => (SubjectSetRewrite)new ComputedSubjectSetRewrite(RelationIdentifier.From(name)));

        TupleToSubjectSetRewriteParser =
            from lparen in Token.EqualTo(RewriteExpressionToken.LeftParen)
            from name in Identifier
            from comma in Token.EqualTo(RewriteExpressionToken.Comma)
            from mapsTo in Identifier
            from rparen in Token.EqualTo(RewriteExpressionToken.RightParen)
            select (SubjectSetRewrite)new TupleToSubjectSetRewrite(RelationIdentifier.From(name), RelationIdentifier.From(mapsTo));

        Term =
            DirectTerm
                .Or(Superpower.Parse.Try(TupleToSubjectSetRewriteParser))
                .Or(ComputedSubjectSetRewriteParser)
                .Or(Superpower.Parse.Ref(() => RewriteExpression!).Between(Token.EqualTo(RewriteExpressionToken.LeftParen), Token.EqualTo(RewriteExpressionToken.RightParen)));

        Exclusion =
            Superpower.Parse.Chain(Token.EqualTo(RewriteExpressionToken.Exclusion), Term, (op, left, right) => new ExclusionRewrite(left, right));

        Intersection =
            Superpower.Parse.Chain(Token.EqualTo(RewriteExpressionToken.Intersection), Exclusion, (op, left, right) =>
                left is IntersectionRewrite intersection
                    ? new IntersectionRewrite(intersection.Children.Add(right))
                    : new IntersectionRewrite([left, right]));

        RewriteExpression =
            Superpower.Parse.Chain(Token.EqualTo(RewriteExpressionToken.Union), Intersection, (op, left, right) =>
                left is UnionRewrite union
                    ? new UnionRewrite(union.Children.Add(right))
                    : new UnionRewrite([left, right]));
    }
}
