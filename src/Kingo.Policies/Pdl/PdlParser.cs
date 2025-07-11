using LanguageExt;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using static LanguageExt.Prelude;

namespace Kingo.Policies.Pdl;

/// <summary>
/// PDL BNF
/// # operator precedence: !, &, | (exclude, intersect, union)
/// # expressions
/// <policy-set>    ::= <policy> [ <policy> ]*
/// <policy>        ::= <policy-identifier> <relation-set>
/// <relation-set>  ::= <relation> [ <relation> ]*
/// <relation>      ::= <relation-identifier> [ '(' <rewrite> ')' ]
/// <rewrite>       ::= <intersection> [ '|' <intersection> ]*
/// <intersection>  ::= <exclusion> [ '&' <exclusion> ]*
/// <exclusion>     ::= <term> [ '!' <term> ]
/// <term>          ::= <direct>
///                   | <computed-subjectset-rewrite>
///                   | <tuple-to-subjectset-rewrite>
///                   | '(' <rewrite> ')'
/// 
/// # keywords (terms)
/// <policy-identifier>             ::= 'policy' <identifier>
/// <direct>                        ::= ('direct' | 'dir')
/// <relation-identifier>           ::= ('relation' | 'rel')  <identifier>
/// <computed-subjectset-rewrite>   ::= ('computed' | 'cmp') <identifier>
/// <tuple-to-subjectset-rewrite>   ::= ('tuple' | 'tpl') (' <identifier> ',' <identifier> ')'
/// <identifier>                    ::= [a-zA-Z_][a-zA-Z0-9_]*
/// 
/// <comment>       ::= '#' [^<newline>]*
/// <newline>       ::= '\n' | '\r\n'
/// </summary>
public static class PdlParser
{
    static PdlParser()
    {
        Term =
            DirectTerm
                .Or(ComputedSubjectSetRewriteParser)
                .Or(TupleToSubjectSetRewriteParser)
                .Or(Superpower.Parse.Ref(() => Rewrite!).Between(Token.EqualTo(PdlToken.LeftParen), Token.EqualTo(PdlToken.RightParen)));

        Exclusion =
            Superpower.Parse.Chain(Token.EqualTo(PdlToken.Exclusion), Term, (op, left, right) => new ExclusionRewrite(left, right));

        Intersection =
            Superpower.Parse.Chain(Token.EqualTo(PdlToken.Intersection), Exclusion, (op, left, right) =>
                left is IntersectionRewrite intersection
                    ? new IntersectionRewrite(intersection.Children.Add(right))
                    : new IntersectionRewrite(Seq(left, right)));

        Rewrite =
            Superpower.Parse.Chain(Token.EqualTo(PdlToken.Union), Intersection, (op, left, right) =>
                left is UnionRewrite union
                    ? new UnionRewrite(union.Children.Add(right))
                    : new UnionRewrite(Seq(left, right)));

        Relation =
            from _ in Token.EqualTo(PdlToken.RelationshipPrefix)
            from name in Identifier
            from rewrite in Rewrite.Between(Token.EqualTo(PdlToken.LeftParen), Token.EqualTo(PdlToken.RightParen)).OptionalOrDefault(DirectRewrite.Default)
            select new Relation(RelationName.From(name), rewrite);

        Policy =
            from _ in Token.EqualTo(PdlToken.PolicyPrefix)
            from name in Identifier
            from relations in Relation.Many()
            select new Policy(PolicyName.From(name), toSeq(relations));

        PolicySet =
            from policies in Policy.Many()
            select new PolicySet(toSeq(policies));
    }

    private static readonly TokenListParser<PdlToken, string> Identifier =
        Token.EqualTo(PdlToken.Identifier).Select(token => token.ToStringValue());

    private static readonly TokenListParser<PdlToken, SubjectSetRewrite> DirectTerm =
        Token.EqualTo(PdlToken.Direct).Select(_ => (SubjectSetRewrite)DirectRewrite.Default);

    private static readonly TokenListParser<PdlToken, SubjectSetRewrite> ComputedSubjectSetRewriteParser =
        from _ in Token.EqualTo(PdlToken.ComputedPrefix)
        from name in Identifier
        select (SubjectSetRewrite)new ComputedSubjectSetRewrite(RelationName.From(name));

    private static readonly TokenListParser<PdlToken, SubjectSetRewrite> TupleToSubjectSetRewriteParser =
        from _ in Token.EqualTo(PdlToken.TuplePrefix)
        from lparen in Token.EqualTo(PdlToken.LeftParen)
        from name in Identifier
        from comma in Token.EqualTo(PdlToken.Comma)
        from mapsTo in Identifier
        from rparen in Token.EqualTo(PdlToken.RightParen)
        select (SubjectSetRewrite)new TupleToSubjectSetRewrite(RelationName.From(name), RelationName.From(mapsTo));

    private static readonly TokenListParser<PdlToken, SubjectSetRewrite> Term;

    private static readonly TokenListParser<PdlToken, SubjectSetRewrite> Rewrite;

    private static readonly TokenListParser<PdlToken, SubjectSetRewrite> Exclusion;

    private static readonly TokenListParser<PdlToken, SubjectSetRewrite> Intersection;

    private static readonly TokenListParser<PdlToken, Relation> Relation;

    private static readonly TokenListParser<PdlToken, Policy> Policy;

    private static readonly TokenListParser<PdlToken, PolicySet> PolicySet;

    public static Either<ParseError, PdlDocument> Parse(string pdl) =>
        PdlTokenizer
        .TryTokenize(pdl)
        .Bind(TryParse)
        .Map(ps => new PdlDocument(pdl, ps));

    private static Either<ParseError, PolicySet> TryParse(TokenList<PdlToken> input)
    {
        var parseResult = PolicySet.AtEnd().TryParse(input);
        return parseResult.HasValue
            ? parseResult.Value
            : ParseError.New(ErrorCodes.ParseEerror, $"parse error: {parseResult}");
    }
}
