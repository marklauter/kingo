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

        // <exclusion> ::= <term> [ '!' <term> ]*
        Exclusion =
            from include in Term
            from exclude in Token.EqualTo(RewriteExpressionToken.Exclusion).IgnoreThen(Term).Many()
            select exclude.Length == 0
                ? include
                : exclude.Aggregate(include, (acc, ex) => new ExclusionRewrite(acc, ex));

        // <rewrite> ::= <exclusion> [ ('&' | '|') <exclusion> ]*
        RewriteExpression =
            from first in Exclusion
            from rest in Token.EqualTo(RewriteExpressionToken.Intersection).Or(Token.EqualTo(RewriteExpressionToken.Union))
                .Then(op => Exclusion.Select(right => (op, right)))
                .Many()
            select rest.Length == 0
                ? first
                : rest.Aggregate(first, (left, item) =>
                    item.op.Kind == RewriteExpressionToken.Union
                        ? left is UnionRewrite union
                            ? new UnionRewrite(union.Children.Add(item.right))
                            : new UnionRewrite([left, item.right])
                        : left is IntersectionRewrite intersection
                            ? new IntersectionRewrite(intersection.Children.Add(item.right))
                            : new IntersectionRewrite([left, item.right]));
    }
}
