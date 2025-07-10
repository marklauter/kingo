using LanguageExt;
using LanguageExt.Common;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using static LanguageExt.Prelude;
using Unit = LanguageExt.Unit;

namespace Kingo.Polcies.Pdl;

public static class PdlParser
{
    private static readonly TokenListParser<PdlToken, string> Identifier =
        Token.EqualTo(PdlToken.Identifier)
            .Apply(Span.EqualTo)
            .Select(span => span.ToStringValue());

    private static readonly TokenListParser<PdlToken, Unit> OptionalNewlines =
        Token.EqualTo(PdlToken.Newline).IgnoreMany().Select(_ => Unit.Value);

    private static readonly TokenListParser<PdlToken, Unit> RequiredNewline =
        Token.EqualTo(PdlToken.Newline).Ignore().Select(_ => Unit.Value);

    private static readonly TokenListParser<PdlToken, Unit> CommentLine =
        from comment in Token.EqualTo(PdlToken.Comment)
        from newline in RequiredNewline
        select Unit.Value;

    private static readonly TokenListParser<PdlToken, Unit> CommentLines =
        CommentLine.IgnoreMany().Select(_ => Unit.Value);

    // <namespace-identifier> ::= 'ns:' <namespace-name>
    private static readonly TokenListParser<PdlToken, string> NamespaceIdentifier =
        from prefix in Token.EqualTo(PdlToken.NamespacePrefix)
        from name in Identifier
        select name;

    // <relationship-identifier> ::= 're:' <relationship-name>
    private static readonly TokenListParser<PdlToken, string> RelationshipIdentifier =
        from prefix in Token.EqualTo(PdlToken.RelationshipPrefix)
        from name in Identifier
        select name;

    // <all-direct-subjects> ::= 'this'
    private static readonly TokenListParser<PdlToken, SubjectSetRewrite> AllDirectSubjects =
        Token.EqualTo(PdlToken.This)
            .Value(This.Default as SubjectSetRewrite);

    // <computed-subjectset-rewrite> ::= 'cp:' <relationship-name>
    private static readonly TokenListParser<PdlToken, SubjectSetRewrite> ComputedSubjectSetRewriteParser =
        from prefix in Token.EqualTo(PdlToken.ComputedPrefix)
        from relationshipName in Identifier
        select (SubjectSetRewrite)ComputedSubjectSetRewrite.Cons(Kingo.RelationshipName.From(relationshipName));

    // <tuple-to-subjectset-rewrite> ::= 'tp:(' <tupleset-relationship> ',' <computed-subjectset-relationship> ')'
    private static readonly TokenListParser<PdlToken, SubjectSetRewrite> TupleToSubjectSetRewriteParser =
        from prefix in Token.EqualTo(PdlToken.TuplePrefix)
        from leftParen in Token.EqualTo(PdlToken.LeftParen)
        from tuplesetRelation in Identifier
        from comma in Token.EqualTo(PdlToken.Comma)
        from computedSubjectSetRelation in Identifier
        from rightParen in Token.EqualTo(PdlToken.RightParen)
        select (SubjectSetRewrite)TupleToSubjectSetRewrite.From(Kingo.RelationshipName.From(tuplesetRelation), Kingo.RelationshipName.From(computedSubjectSetRelation));

    // Forward reference for recursive grammar
    private static readonly TokenListParser<PdlToken, SubjectSetRewrite> RewriteRule =
        Parse.Ref(() => UnionExpression);

    // Parenthesized expression: '(' <rewrite-rule> ')'
    private static readonly TokenListParser<PdlToken, SubjectSetRewrite> ParenthesizedExpression =
        from leftParen in Token.EqualTo(PdlToken.LeftParen)
        from rule in RewriteRule
        from rightParen in Token.EqualTo(PdlToken.RightParen)
        select rule;

    // Primary expressions (highest precedence)
    private static readonly TokenListParser<PdlToken, SubjectSetRewrite> PrimaryExpression =
        AllDirectSubjects
            .Or(ComputedSubjectSetRewriteParser)
            .Or(TupleToSubjectSetRewriteParser)
            .Or(ParenthesizedExpression);

    // Exclusion expressions: <primary> | '!' <exclusion>
    private static readonly TokenListParser<PdlToken, SubjectSetRewrite> ExclusionExpression =
        Parse.Chain(
            Token.EqualTo(PdlToken.Exclusion),
            PrimaryExpression,
            (left, right) => ExclusionRewrite.From(left, right));

    // Intersection expressions: <exclusion> ('&' <exclusion>)*
    private static readonly TokenListParser<PdlToken, SubjectSetRewrite> IntersectionExpression =
        Parse.Chain(
            Token.EqualTo(PdlToken.Intersection),
            ExclusionExpression,
            (left, right) => CombineIntersection(left, right));

    // Union expressions: <intersection> ('|' <intersection>)*
    private static readonly TokenListParser<PdlToken, SubjectSetRewrite> UnionExpression =
        Parse.Chain(
            Token.EqualTo(PdlToken.Union),
            IntersectionExpression,
            (left, right) => CombineUnion(left, right));

    // Helper methods to combine union and intersection expressions
    private static SubjectSetRewrite CombineUnion(SubjectSetRewrite left, SubjectSetRewrite right) =>
        left switch
        {
            UnionRewrite union => UnionRewrite.From(union.Children.Add(right)),
            _ => UnionRewrite.From(Seq.create(left, right))
        };

    private static SubjectSetRewrite CombineIntersection(SubjectSetRewrite left, SubjectSetRewrite right) =>
        left switch
        {
            IntersectionRewrite intersection => IntersectionRewrite.From(intersection.Children.Add(right)),
            _ => IntersectionRewrite.From(Seq.create(left, right))
        };

    // <relationship> ::= <relationship-identifier> | <relationship-identifier> '(' <rewrite-rule> ')'
    private static readonly TokenListParser<PdlToken, PdlRelationship> RelationshipParser =
        from name in RelationshipIdentifier
        from rewriteRule in (from leftParen in Token.EqualTo(PdlToken.LeftParen)
                           from rule in RewriteRule
                           from rightParen in Token.EqualTo(PdlToken.RightParen)
                           select rule).OptionalOrDefault()
        select rewriteRule != null
            ? PdlRelationship.Create(name, rewriteRule)
            : PdlRelationship.Create(name);

    // <relationship-line> ::= <relationship> <newline> | <comment> <newline>
    private static readonly TokenListParser<PdlToken, Option<PdlRelationship>> RelationshipLine =
        RelationshipParser.Select(Option<PdlRelationship>.Some)
            .Try()
            .Or(CommentLine.Value(Option<PdlRelationship>.None))
            .Then(RequiredNewline.Try().Optional());

    // <relationship-list> ::= <relationship-line> | <relationship-list> <relationship-line>
    private static readonly TokenListParser<PdlToken, Seq<PdlRelationship>> RelationshipList =
        RelationshipLine
            .Many()
            .Select(relationships => relationships.ToSeq().Choose(r => r));

    // <namespace> ::= <namespace-identifier> <newline> <relationship-list>
    private static readonly TokenListParser<PdlToken, PdlNamespace> NamespaceParser =
        from name in NamespaceIdentifier
        from newline in RequiredNewline
        from relationships in RelationshipList
        select PdlNamespace.Create(name, relationships);

    // <namespace-list> ::= <namespace> | <namespace-list> <comment-lines> <namespace>
    private static readonly TokenListParser<PdlToken, Seq<PdlNamespace>> NamespaceList =
        from namespaces in NamespaceParser.ManyDelimitedBy(CommentLines)
        select Seq.createRange(namespaces);

    // <document> ::= <comment-lines> <namespace-list>
    private static readonly TokenListParser<PdlToken, PdlDocument> Document =
        from commentLines in CommentLines
        from namespaces in NamespaceList
        from endComments in CommentLines
        select PdlDocument.Create(namespaces);

    public static Either<Error, PdlDocument> Parse(string input)
    {
        try
        {
            var tokenizer = PdlTokenizer.Create();
            var tokens = tokenizer.Tokenize(input);
            var result = Document.AtEnd().Parse(tokens);
            return result.HasValue ? Prelude.Right(result.Value) : Prelude.Left<Error, PdlDocument>(Error.New(result.ToString()));
        }
        catch (ParseException ex)
        {
            return Error.New(ex.Message);
        }
        catch (Exception ex)
        {
            return Error.New($"Unexpected error during parsing: {ex.Message}");
        }
    }

    public static Either<Error, PdlDocument> ParseFromFile(string filePath)
    {
        try
        {
            var content = File.ReadAllText(filePath);
            return Parse(content);
        }
        catch (Exception ex)
        {
            return Error.New($"Failed to read file '{filePath}': {ex.Message}");
        }
    }
}
