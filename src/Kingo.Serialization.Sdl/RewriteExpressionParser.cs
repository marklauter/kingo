using Kingo.Schemas;
using Results;
using Superpower;
using Superpower.Display;
using Superpower.Parsers;
using Superpower.Tokenizers;

namespace Kingo.Serialization.Sdl;

/// <summary>
/// Parses the rewrite-expression mini-language embedded in SDL relationship values — e.g. <c>(this | editor | (parent, viewer)) ! banned</c> — into the core
/// <c>SubjectSetRewrite</c> algebra. Grammar and precedence: docs/notes/sdl-yaml.md (<c>!</c> binds tightest; <c>&amp;</c> and <c>|</c> share precedence,
/// left-associative). The Superpower grammar produces the internal <see cref="RewriteNode"/> tree; the transform at the exit parses every identifier through
/// <c>RelationshipIdentifier.Parse</c> and accumulates the errors, so bad input surfaces as <see cref="Result{T}"/> validation failures, never exceptions.
/// </summary>
internal static class RewriteExpressionParser
{
    public static Result<SubjectSetRewrite> Parse(string expression)
    {
        var tokens = Tokenizer.TryTokenize(expression);
        if (!tokens.HasValue)
            return Result.Failure<SubjectSetRewrite>(Error.Validation("sdl.rewrite", $"invalid rewrite expression '{expression}': {tokens}"));

        var parsed = Expression.AtEnd().TryParse(tokens.Value);
        return parsed.HasValue
            ? Transform(parsed.Value)
            : Result.Failure<SubjectSetRewrite>(Error.Validation("sdl.rewrite", $"invalid rewrite expression '{expression}': {parsed}"));
    }

    private static Result<SubjectSetRewrite> Transform(RewriteNode node) =>
        node switch
        {
            ThisNode => Result.Success<SubjectSetRewrite>(ThisRewrite.Default),
            ComputedNode computed => RelationshipIdentifier.Parse(computed.Relationship)
                .Map(SubjectSetRewrite (relationship) => new ComputedSubjectSetRewrite(relationship)),
            TupleToNode tupleTo => Result.Apply(
                RelationshipIdentifier.Parse(tupleTo.TuplesetRelationship)
                    .Map(Func<RelationshipIdentifier, SubjectSetRewrite> (tupleset) => computed => new TupleToSubjectSetRewrite(tupleset, computed)),
                RelationshipIdentifier.Parse(tupleTo.ComputedRelationship)),
            UnionNode union => union.Children.Select(Transform).Sequence()
                .Map(children => (SubjectSetRewrite)new UnionRewrite(children)),
            IntersectionNode intersection => intersection.Children.Select(Transform).Sequence()
                .Map(children => (SubjectSetRewrite)new IntersectionRewrite(children)),
            // the last inhabitant of the closed hierarchy: a discard arm (rather than a type pattern)
            // keeps the compiler from synthesizing an unreachable default branch under the switch
            _ => TransformExclusion((ExclusionNode)node),
        };

    private static Result<SubjectSetRewrite> TransformExclusion(ExclusionNode exclusion) =>
        Result.Apply(
            Transform(exclusion.Include)
                .Map(Func<SubjectSetRewrite, SubjectSetRewrite> (include) => exclude => new ExclusionRewrite(include, exclude)),
            Transform(exclusion.Exclude));

    private enum RewriteExpressionToken
    {
        None,

        [Token(Category = "identifier", Example = "myRelationship")]
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

        var thisTerm = Token.EqualTo(RewriteExpressionToken.This).Select(_ => (RewriteNode)ThisNode.Instance);

        var computed = identifier.Select(name => (RewriteNode)new ComputedNode(name));

        var tupleToSubjectSet =
            from lparen in Token.EqualTo(RewriteExpressionToken.LeftParen)
            from tupleset in identifier
            from comma in Token.EqualTo(RewriteExpressionToken.Comma)
            from computedRelationship in identifier
            from rparen in Token.EqualTo(RewriteExpressionToken.RightParen)
            select (RewriteNode)new TupleToNode(tupleset, computedRelationship);

        TokenListParser<RewriteExpressionToken, RewriteNode>? expressionRef = null;
        var term = tupleToSubjectSet.Try().Or(thisTerm.Try()).Or(computed)
            .Or(Superpower.Parse.Ref(() => expressionRef!)
                .Between(Token.EqualTo(RewriteExpressionToken.LeftParen), Token.EqualTo(RewriteExpressionToken.RightParen)));

        var exclusion =
            from include in term
            from excludes in Token.EqualTo(RewriteExpressionToken.Exclusion).IgnoreThen(term).Many()
            select excludes.Aggregate(include, (accumulated, exclude) => new ExclusionNode(accumulated, exclude));

        expressionRef =
            from first in exclusion
            from rest in Token.EqualTo(RewriteExpressionToken.Intersection).Or(Token.EqualTo(RewriteExpressionToken.Union))
                .Then(op => exclusion.Select(right => (Op: op.Kind, Right: right)))
                .Many()
            select ChainBinaryOperators(first, rest);

        return expressionRef;
    }

    /// <summary>
    /// Folds an operator chain left-to-right, flattening each run of consecutive same-operator applications into one n-ary node: <c>a | b | c</c> is a single
    /// three-child union. Only nodes built by this chain are appended into — a parenthesized operand arrives as an opaque <paramref name="first"/> or
    /// <c>Right</c> and is never absorbed, so <c>(a | b) | c</c> keeps its nested shape and round-trips structurally.
    /// </summary>
    private static RewriteNode ChainBinaryOperators(RewriteNode first, (RewriteExpressionToken Op, RewriteNode Right)[] rest)
    {
        var accumulated = first;
        RewriteExpressionToken? previousOp = null;
        foreach (var (op, right) in rest)
        {
            accumulated = (accumulated, samePrecedingOp: op == previousOp) switch
            {
                (UnionNode union, true) => new UnionNode([.. union.Children, right]),
                (IntersectionNode intersection, true) => new IntersectionNode([.. intersection.Children, right]),
                _ => op == RewriteExpressionToken.Union
                    ? new UnionNode([accumulated, right])
                    : new IntersectionNode([accumulated, right]),
            };
            previousOp = op;
        }

        return accumulated;
    }
}
