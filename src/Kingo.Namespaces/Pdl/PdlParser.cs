using LanguageExt;
using LanguageExt.Common;
using Superpower;
using Superpower.Parsers;
using static LanguageExt.Prelude;
using Unit = LanguageExt.Unit;

namespace Kingo.Policies.Pdl;

public static class PdlParser
{
    private static readonly TokenListParser<PdlToken, string> Identifier =
        Token.EqualTo(PdlToken.Identifier)
            .Select(t => t.Span.ToStringValue());

    private static readonly TokenListParser<PdlToken, Unit> OptionalNewlines =
        Token.EqualTo(PdlToken.Newline).Many().Select(_ => unit);

    private static readonly TokenListParser<PdlToken, Unit> RequiredNewline =
        Token.EqualTo(PdlToken.Newline).Select(_ => unit);

    private static readonly TokenListParser<PdlToken, Unit> CommentLine =
        Token.EqualTo(PdlToken.Comment)
            .Then(_ => RequiredNewline)
            .Select(_ => unit);

    private static readonly TokenListParser<PdlToken, Unit> CommentLines =
        CommentLine.Many().Select(_ => unit);

    // <policy-identifier> ::= 'pn:' <policy-name>
    private static readonly TokenListParser<PdlToken, string> PolicyIdentifierParser =
        Token.EqualTo(PdlToken.PolicyPrefix)
            .Then(_ => Identifier);

    // <relationship-identifier> ::= 're:' <relationship-name>
    private static readonly TokenListParser<PdlToken, string> RelationshipIdentifier =
        Token.EqualTo(PdlToken.RelationshipPrefix)
            .Then(_ => Identifier);

    // <all-direct-subjects> ::= 'this'
    private static readonly TokenListParser<PdlToken, SubjectSetRewrite> AllDirectSubjects =
        Token.EqualTo(PdlToken.This)
            .Value((SubjectSetRewrite)This.Default);

    // <computed-subjectset-rewrite> ::= 'cp:' <relationship-name>
    private static readonly TokenListParser<PdlToken, SubjectSetRewrite> ComputedSubjectSetRewriteParser =
        Token.EqualTo(PdlToken.ComputedPrefix)
            .IgnoreThen(Identifier)
            .Select(relationshipName => (SubjectSetRewrite)ComputedSubjectSetRewrite.Cons(RelationshipName.From(relationshipName)));

    // <tuple-to-subjectset-rewrite> ::= 'tp:(' <tupleset-relationship> ',' <computed-subjectset-relationship> ')'
    private static readonly TokenListParser<PdlToken, SubjectSetRewrite> TupleToSubjectSetRewriteParser =
        Token.EqualTo(PdlToken.TuplePrefix)
            .IgnoreThen(Token.EqualTo(PdlToken.LeftParen))
            .IgnoreThen(Identifier)
            .Then(tuplesetRelation => Token.EqualTo(PdlToken.Comma)
                .IgnoreThen(Identifier)
                .Then(computedSubjectSetRelation => Token.EqualTo(PdlToken.RightParen)
                    .Select(_ => (SubjectSetRewrite)TupleToSubjectSetRewrite.Cons(
                        RelationshipName.From(tuplesetRelation),
                        RelationshipName.From(computedSubjectSetRelation)))));

    // Forward reference for recursive grammar
    private static readonly TokenListParser<PdlToken, SubjectSetRewrite> RewriteRule =
        Superpower.Parse.Ref(() => UnionExpression);

    // Parenthesized expression: '(' <rewrite-rule> ')'
    private static readonly TokenListParser<PdlToken, SubjectSetRewrite> ParenthesizedExpression =
        Token.EqualTo(PdlToken.LeftParen)
            .IgnoreThen(RewriteRule)
            .Then(rule => Token.EqualTo(PdlToken.RightParen).Select(_ => rule));

    // Primary expressions (highest precedence)
    private static readonly TokenListParser<PdlToken, SubjectSetRewrite> PrimaryExpression =
        AllDirectSubjects
            .Or(ComputedSubjectSetRewriteParser)
            .Or(TupleToSubjectSetRewriteParser)
            .Or(ParenthesizedExpression);

    // Exclusion expressions: <primary> | '!' <exclusion>
    private static readonly TokenListParser<PdlToken, SubjectSetRewrite> ExclusionExpression =
        Superpower.Parse.Chain(
            Token.EqualTo(PdlToken.Exclusion),
            PrimaryExpression,
            (op, left, right) => ExclusionRewrite.Cons(left, right));

    // Intersection expressions: <exclusion> ('&' <exclusion>)*
    private static readonly TokenListParser<PdlToken, SubjectSetRewrite> IntersectionExpression =
        Superpower.Parse.Chain(
            Token.EqualTo(PdlToken.Intersection),
            ExclusionExpression,
            (op, left, right) => CombineIntersection(left, right));

    // Union expressions: <intersection> ('|' <intersection>)*
    private static readonly TokenListParser<PdlToken, SubjectSetRewrite> UnionExpression =
        Superpower.Parse.Chain(
            Token.EqualTo(PdlToken.Union),
            IntersectionExpression,
            (op, left, right) => CombineUnion(left, right));

    // Helper methods to combine union and intersection expressions
    private static UnionRewrite CombineUnion(SubjectSetRewrite left, SubjectSetRewrite right) =>
        left is UnionRewrite union
            ? UnionRewrite.Cons(union.Children.Add(right))
            : UnionRewrite.Cons(Seq(left, right));

    private static IntersectionRewrite CombineIntersection(SubjectSetRewrite left, SubjectSetRewrite right) =>
        left is IntersectionRewrite intersection
            ? IntersectionRewrite.Cons(intersection.Children.Add(right))
            : IntersectionRewrite.Cons(Seq(left, right));

    // <relationship> ::= <relationship-identifier> | <relationship-identifier> '(' <rewrite-rule> ')'
    private static readonly TokenListParser<PdlToken, Relationship> RelationshipParser =
        RelationshipIdentifier.Then(name =>
            (from leftParen in Token.EqualTo(PdlToken.LeftParen)
             from rule in RewriteRule
             from rightParen in Token.EqualTo(PdlToken.RightParen)
             select rule)
            .OptionalOrDefault(This.Default)
            .Select(rewriteRule => new Relationship(RelationshipName.From(name), rewriteRule)));

    // <relationship-line> ::= <relationship> <newline> | <comment> <newline>
    private static readonly TokenListParser<PdlToken, Option<Relationship>> RelationshipLine =
        (from r in RelationshipParser.Select(Option<Relationship>.Some)
         from nl in RequiredNewline
         select r)
        .Try()
        .Or(CommentLine.Value(Option<Relationship>.None));

    // <relationship-list> ::= <relationship-line> | <relationship-list> <relationship-line>
    private static readonly TokenListParser<PdlToken, Seq<Relationship>> RelationshipList =
        RelationshipLine
            .Many()
            .Select(relationships => toSeq(relationships).Somes());

    // <policy> ::= <policy-identifier> <newline> <relationship-list>
    private static readonly TokenListParser<PdlToken, Policy> PolicyParser =
        PolicyIdentifierParser
            .Then(name => RequiredNewline
                .IgnoreThen(RelationshipList)
                .Select(relationships => new Policy(PolicyName.From(name), relationships)));

    // <policy-list> ::= <policy> | <policy-list> <comment-lines> <policy>
    private static readonly TokenListParser<PdlToken, Seq<Policy>> PolicyList =
        PolicyParser.ManyDelimitedBy(CommentLines)
            .Select(policies => toSeq(policies));

    // <document> ::= <comment-lines> <policy-list>
    private static readonly TokenListParser<PdlToken, Document> DocumentParser =
        CommentLines
            .IgnoreThen(PolicyList)
            .Then(policies => CommentLines.Select(_ => policies))
            .Select(policies => new Document(policies));

    public static Either<Error, Document> Parse(string input) =>
        Try.lift(() =>
            {
                var tokensResult = PdlTokenizer.Create().TryTokenize(input);
                var tokensEither = tokensResult.HasValue
                    ? Right<Superpower.Model.TokenList<PdlToken>>(tokensResult.Value)
                    : Left<Error>(Error.New($"Tokenization failed: {tokensResult}"));

                return tokensEither.Bind(tokens =>
                {
                    var parseResult = DocumentParser.AtEnd().TryParse(tokens);
                    return parseResult.HasValue
                        ? Right<Document>(parseResult.Value)
                        : Left<Error>(Error.New(parseResult.ToString()));
                });
            })
            .Match(
                Succ: result => result,
                Fail: ex => Error.New($"Unexpected error during parsing: {ex.Message}"));
}
