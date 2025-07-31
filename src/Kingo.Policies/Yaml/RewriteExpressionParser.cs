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
        // do not check parseResult.Value is not null because HasValue already does that
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
    private static readonly TokenListParser<RewriteExpressionToken, SubjectSetRewrite> Union;
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

        var nonParenthesizedTerm =
            TupleToSubjectSetRewriteParser.Try()
                .Or(DirectTerm.Try())
                .Or(ComputedSubjectSetRewriteParser);

        Term =
            nonParenthesizedTerm
                .Or(Superpower.Parse.Ref(() => RewriteExpression!
                    .Between(Token.EqualTo(RewriteExpressionToken.LeftParen), Token.EqualTo(RewriteExpressionToken.RightParen))));

        // <intersection> ::= <term> [ '&' <term> ]*
        Intersection =
            Superpower.Parse.Chain(Token.EqualTo(RewriteExpressionToken.Intersection), Term, (op, left, right) =>
                left is IntersectionRewrite intersection
                    ? new IntersectionRewrite(intersection.Children.Add(right))
                    : new IntersectionRewrite([left, right]));

        // <exclusion> ::= <intersection> [ '!' <intersection> ]*
        Exclusion =
            from include in Intersection
            from exclude in Token.EqualTo(RewriteExpressionToken.Exclusion).IgnoreThen(Intersection).Many()
            select exclude.Length == 0
                ? include
                : exclude.Aggregate(include, (acc, ex) => new ExclusionRewrite(acc, ex));

        // <union> ::= <exclusion> [ '|' <exclusion> ]*
        Union =
            Superpower.Parse.Chain(Token.EqualTo(RewriteExpressionToken.Union), Exclusion, (op, left, right) =>
                left is UnionRewrite union
                    ? new UnionRewrite(union.Children.Add(right))
                    : new UnionRewrite([left, right]));

        // <rewrite> ::= <union>
        RewriteExpression = Union;
    }
}
