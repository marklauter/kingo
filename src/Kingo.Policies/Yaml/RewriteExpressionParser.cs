using Kingo.Policies.Puddle;
using LanguageExt;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using static LanguageExt.Prelude;

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

    private static readonly TokenListParser<RewriteExpressionToken, string> Identifier =
        Token.EqualTo(RewriteExpressionToken.Identifier).Select(token => token.ToStringValue());

    private static readonly TokenListParser<RewriteExpressionToken, SubjectSetRewrite> DirectTerm =
        Token.EqualTo(RewriteExpressionToken.This).Select(_ => (SubjectSetRewrite)DirectRewrite.Default);

    private static readonly TokenListParser<RewriteExpressionToken, SubjectSetRewrite> ComputedSubjectSetRewriteParser =
        Identifier.Select(name => (SubjectSetRewrite)new ComputedSubjectSetRewrite(RelationIdentifier.From(name)));

    private static readonly TokenListParser<RewriteExpressionToken, SubjectSetRewrite> TupleToSubjectSetRewriteParser =
        from lparen in Token.EqualTo(RewriteExpressionToken.LeftParen)
        from name in Identifier
        from comma in Token.EqualTo(RewriteExpressionToken.Comma)
        from mapsTo in Identifier
        from rparen in Token.EqualTo(RewriteExpressionToken.RightParen)
        select (SubjectSetRewrite)new TupleToSubjectSetRewrite(RelationIdentifier.From(name), RelationIdentifier.From(mapsTo));

    private static readonly TokenListParser<RewriteExpressionToken, SubjectSetRewrite> Term =
        DirectTerm
            .Or(TupleToSubjectSetRewriteParser)
            .Or(ComputedSubjectSetRewriteParser)
            .Or(Parse.Ref(() => RewriteExpressionParser).Between(Token.EqualTo(RewriteExpressionToken.LeftParen), Token.EqualTo(RewriteExpressionToken.RightParen)));

    private static readonly TokenListParser<RewriteExpressionToken, SubjectSetRewrite> Exclusion =
        Parse.Chain(Token.EqualTo(RewriteExpressionToken.Exclusion), Term, (op, left, right) => new ExclusionRewrite(left, right));

    private static readonly TokenListParser<RewriteExpressionToken, SubjectSetRewrite> Intersection =
        Parse.Chain(Token.EqualTo(RewriteExpressionToken.Intersection), Exclusion, (op, left, right) =>
            left is IntersectionRewrite intersection
                ? new IntersectionRewrite(intersection.Children.Add(right))
                : new IntersectionRewrite(Seq(left, right)));

    private static readonly TokenListParser<RewriteExpressionToken, SubjectSetRewrite> RewriteExpression =
        Parse.Chain(Token.EqualTo(RewriteExpressionToken.Union), Intersection, (op, left, right) =>
            left is UnionRewrite union
                ? new UnionRewrite(union.Children.Add(right))
                : new UnionRewrite(Seq(left, right)));
}
