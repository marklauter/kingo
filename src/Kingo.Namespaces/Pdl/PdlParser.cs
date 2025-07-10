using LanguageExt;
using LanguageExt.Common;
using Superpower;
using Superpower.Display;
using Superpower.Model;
using Superpower.Parsers;
using Superpower.Tokenizers;
using static LanguageExt.Prelude;
using Unit = LanguageExt.Unit;

namespace Kingo.Policies.Pdl;

public enum PdlToken
{
    None,

    [Token(Category = "identifier", Example = "file")]
    Identifier,

    [Token(Category = "keyword", Example = "pn:")]
    PolicyPrefix,    // pn:

    [Token(Category = "keyword", Example = "re:")]
    RelationshipPrefix, // re:

    [Token(Category = "keyword", Example = "cp:")]
    ComputedPrefix,     // cp:

    [Token(Category = "keyword", Example = "tp:")]
    TuplePrefix,        // tp:

    [Token(Category = "keyword", Example = "this")]
    This,               // this

    [Token(Category = "operator", Example = "|")]
    Union,              // |

    [Token(Category = "operator", Example = "&")]
    Intersection,       // &

    [Token(Category = "operator", Example = "!")]
    Exclusion,          // !

    [Token(Category = "delimiter", Example = "(")]
    LeftParen,          // (

    [Token(Category = "delimiter", Example = ")")]
    RightParen,         // )

    [Token(Category = "delimiter", Example = ",")]
    Comma,              // ,

    [Token(Category = "comment", Example = "# This is a comment")]
    Comment,            // # ...

    [Token(Category = "newline", Example = "\\n")]
    Newline             // \n
}

public static class PdlTokenizer
{
    public static Tokenizer<PdlToken> Create() => new TokenizerBuilder<PdlToken>()
            .Ignore(Character.EqualTo(' ').Or(Character.EqualTo('\t')).Many())
            .Match(Character.EqualTo('\n'), PdlToken.Newline)
            .Match(Span.EqualTo("pn:"), PdlToken.PolicyPrefix)
            .Match(Span.EqualTo("re:"), PdlToken.RelationshipPrefix)
            .Match(Span.EqualTo("cp:"), PdlToken.ComputedPrefix)
            .Match(Span.EqualTo("tp:"), PdlToken.TuplePrefix)
            .Match(Span.EqualTo("this"), PdlToken.This)
            .Match(Character.EqualTo('|'), PdlToken.Union)
            .Match(Character.EqualTo('&'), PdlToken.Intersection)
            .Match(Character.EqualTo('!'), PdlToken.Exclusion)
            .Match(Character.EqualTo('('), PdlToken.LeftParen)
            .Match(Character.EqualTo(')'), PdlToken.RightParen)
            .Match(Character.EqualTo(','), PdlToken.Comma)
            .Match(Comment, PdlToken.Comment)
            .Match(Identifier, PdlToken.Identifier)
            .Build();

    private static readonly TextParser<Unit> Comment =
        from hash in Character.EqualTo('#')
        from content in Character.ExceptIn('\n').Many()
        select unit;

    private static readonly TextParser<Unit> Identifier =
        from first in Character.Letter.Or(Character.EqualTo('_'))
        from rest in Character.LetterOrDigit.Or(Character.EqualTo('_')).Many()
        select unit;
}

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
        from comment in Token.EqualTo(PdlToken.Comment)
        from newline in RequiredNewline
        select unit;

    private static readonly TokenListParser<PdlToken, Unit> CommentLines =
        CommentLine.Many().Select(_ => unit);

    // <policy-identifier> ::= 'pn:' <policy-name>
    private static readonly TokenListParser<PdlToken, string> PolicyIdentifierParser =
        from prefix in Token.EqualTo(PdlToken.PolicyPrefix)
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
            .Value(Policies.This.Default as SubjectSetRewrite);

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
        select (SubjectSetRewrite)TupleToSubjectSetRewrite.Cons(Kingo.RelationshipName.From(tuplesetRelation), Kingo.RelationshipName.From(computedSubjectSetRelation));

    // Forward reference for recursive grammar
    private static readonly TokenListParser<PdlToken, SubjectSetRewrite> RewriteRule =
        Superpower.Parse.Ref(() => UnionExpression);

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
    private static SubjectSetRewrite CombineUnion(SubjectSetRewrite left, SubjectSetRewrite right) =>
        left switch
        {
            UnionRewrite union => UnionRewrite.Cons(union.Children.Add(right)),
            _ => UnionRewrite.Cons(Seq(left, right))
        };

    private static SubjectSetRewrite CombineIntersection(SubjectSetRewrite left, SubjectSetRewrite right) =>
        left switch
        {
            IntersectionRewrite intersection => IntersectionRewrite.Cons(intersection.Children.Add(right)),
            _ => IntersectionRewrite.Cons(Seq(left, right))
        };

    // <relationship> ::= <relationship-identifier> | <relationship-identifier> '(' <rewrite-rule> ')'
    private static readonly TokenListParser<PdlToken, Relationship> RelationshipParser =
        from name in RelationshipIdentifier
        from rewriteRule in (from leftParen in Token.EqualTo(PdlToken.LeftParen)
                             from rule in RewriteRule
                             from rightParen in Token.EqualTo(PdlToken.RightParen)
                             select rule).OptionalOrDefault(Policies.This.Default)
        select new Relationship(Kingo.RelationshipName.From(name), rewriteRule);

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
        from name in PolicyIdentifierParser
        from newline in RequiredNewline
        from relationships in RelationshipList
        select new Policy(Kingo.PolicyName.From(name), relationships);

    // <policy-list> ::= <policy> | <policy-list> <comment-lines> <policy>
    private static readonly TokenListParser<PdlToken, Seq<Policy>> PolicyList =
        from policies in PolicyParser.ManyDelimitedBy(CommentLines)
        select toSeq(policies);

    // <document> ::= <comment-lines> <policy-list>
    private static readonly TokenListParser<PdlToken, Document> DocumentParser =
        from commentLines in CommentLines
        from policies in PolicyList
        from endComments in CommentLines
        select new Document(policies);

    public static Either<Error, Document> Parse(string input)
    {
        try
        {
            var tokenizer = PdlTokenizer.Create();
            var tokens = tokenizer.Tokenize(input);
            TokenListParserResult<PdlToken, Document> result = DocumentParser.AtEnd().Parse(tokens);
            return result.HasValue ? Right(result.Value) : Left<Error, Document>(Error.New(result.ToString()));
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

    public static Either<Error, Document> ParseFromFile(string filePath)
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
