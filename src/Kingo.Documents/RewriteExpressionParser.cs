using Kingo.Domains;
using Results;
using Superpower;
using Superpower.Display;
using Superpower.Parsers;
using Superpower.Tokenizers;
using System.Collections.Immutable;

namespace Kingo.Documents;

/// <summary>
/// Recursive-descent parser for the rewrite-expression mini-language embedded in SDL relationship values, for example
/// <c>(this | editor | (parent, viewer)) ! banned</c>. Produces the core <c>SubjectSetRewrite</c> algebra. The grammar and its precedence
/// are given in [[specs]]: a cascade, tightest first, <c>!</c> exclusion, then <c>&amp;</c> intersection, then <c>|</c> union. Each level is
/// left-associative, so <c>a | b &amp; c</c> is <c>a | (b &amp; c)</c>. The Superpower grammar produces the internal <see cref="RewriteNode"/>
/// tree. The transform at the exit parses every identifier through <c>RelationshipName.Parse</c> and accumulates the errors, so bad input
/// surfaces as <see cref="Result{T}"/> validation failures rather than exceptions. The expression language writes bare names only, so the
/// transform needs no namespace to qualify against. The rewrite algebra stores names, and evaluation qualifies them against the resource in
/// hand ([[identifiers]]).
/// </summary>
internal static class RewriteExpressionParser
{
    /// <summary>Parses <paramref name="expression"/> into the core <c>SubjectSetRewrite</c> algebra.</summary>
    /// <returns>
    /// A successful <see cref="Result{T}"/> carrying the parsed <c>SubjectSetRewrite</c>. A <c>domain.rewrite</c> validation failure when the
    /// text does not tokenize, when parenthesis nesting exceeds <c>SubjectSetRewrite.MaxDepth</c>, or when the token stream does not parse.
    /// A <c>rewrite.depth</c> failure when the parsed tree exceeds <c>SubjectSetRewrite.MaxDepth</c>. Identifier validation failures from
    /// <c>RelationshipName.Parse</c> when any name in the expression is not a valid relationship name.
    /// </returns>
    public static Result<SubjectSetRewrite> Parse(string expression)
    {
        var tokens = Tokenizer.TryTokenize(expression);
        if (!tokens.HasValue)
            return Result.Failure<SubjectSetRewrite>(Error.Validation("domain.rewrite", $"invalid rewrite expression '{expression}': {tokens}"));

        if (WouldOverflowTheParserStack(tokens.Value))
            return Result.Failure<SubjectSetRewrite>(
                Error.Validation("domain.rewrite", $"invalid rewrite expression '{expression}': parenthesis nesting exceeds {SubjectSetRewrite.MaxDepth} levels"));

        var parsed = Expression.AtEnd().TryParse(tokens.Value);
        return !parsed.HasValue
            ? Result.Failure<SubjectSetRewrite>(Error.Validation("domain.rewrite", $"invalid rewrite expression '{expression}': {parsed}"))
            : ExceedsMaxDepth(parsed.Value)
                ? Result.Failure<SubjectSetRewrite>(SubjectSetRewrite.DepthError())
                : Transform(parsed.Value);
    }

    /// <summary>
    /// Bounds grouping-parenthesis nesting before the grammar runs. <c>Superpower.Parse.Ref</c> recurses one combinator frame per
    /// grouping-parenthesis level. Nothing else in the grammar recurses, because operator chains and exclusion links are iterative
    /// <c>Many()</c> folds, so grouping nesting is the one quantity that must stay bounded on untrusted text. A running counter measures it.
    /// A factset is the five-token window <c>( identifier , identifier )</c>, which the grammar parses without recursing, so it is skipped
    /// whole. Every other <c>(</c> counts, and a stray <c>)</c> below level zero is ignored here and fails as bad syntax in the grammar.
    /// Tree depth is measured separately by <see cref="ExceedsMaxDepth"/> on the parsed tree.
    /// </summary>
    /// <returns><see langword="true"/> when grouping nesting exceeds <c>SubjectSetRewrite.MaxDepth</c>; otherwise <see langword="false"/>.</returns>
    private static bool WouldOverflowTheParserStack(Superpower.Model.TokenList<RewriteExpressionToken> tokens)
    {
        var kinds = tokens.Select(token => token.Kind).ToArray();
        var depth = 0;
        for (var i = 0; i < kinds.Length; i++)
        {
            if (kinds[i] == RewriteExpressionToken.LeftParen)
            {
                if (IsFactsetShape(kinds, i))
                {
                    i += 4; // land on the factset's ')', so it neither opens a level nor closes one
                    continue;
                }

                if (++depth > SubjectSetRewrite.MaxDepth)
                    return true;
            }
            else if (kinds[i] == RewriteExpressionToken.RightParen && depth > 0)
            {
                depth--;
            }
        }

        return false;
    }

    /// <summary>Reports whether the five tokens starting at <paramref name="openParen"/> form the factset window <c>( identifier , identifier )</c>. Anything looser is grouping and counts toward nesting.</summary>
    /// <returns><see langword="true"/> when the window is an exact factset; otherwise <see langword="false"/>.</returns>
    private static bool IsFactsetShape(RewriteExpressionToken[] kinds, int openParen) =>
        openParen + 4 < kinds.Length
        && kinds[openParen + 1] == RewriteExpressionToken.Identifier
        && kinds[openParen + 2] == RewriteExpressionToken.Comma
        && kinds[openParen + 3] == RewriteExpressionToken.Identifier
        && kinds[openParen + 4] == RewriteExpressionToken.RightParen;

    /// <summary>
    /// Measures the parsed tree's height against <c>SubjectSetRewrite.MaxDepth</c> before <see cref="Transform"/> runs, which recurses per
    /// tree level. The height is measured on the tree itself rather than a token-shape estimate, so it cannot misjudge association. An
    /// explicit stack keeps the measurement depth-proof. A too-deep tree is refused with the same <c>rewrite.depth</c> error the operator
    /// factories issue.
    /// </summary>
    /// <returns><see langword="true"/> when the tree height exceeds <c>SubjectSetRewrite.MaxDepth</c>; otherwise <see langword="false"/>.</returns>
    private static bool ExceedsMaxDepth(RewriteNode root)
    {
        var pending = new Stack<(RewriteNode Node, int Depth)>();
        pending.Push((root, 1));
        while (pending.Count > 0)
        {
            var (node, depth) = pending.Pop();
            if (depth > SubjectSetRewrite.MaxDepth)
                return true;

            switch (node)
            {
                case RewriteNode.Union union:
                    foreach (var child in union.Children)
                        pending.Push((child, depth + 1));
                    break;
                case RewriteNode.Intersection intersection:
                    foreach (var child in intersection.Children)
                        pending.Push((child, depth + 1));
                    break;
                case RewriteNode.Exclusion exclusion:
                    pending.Push((exclusion.Include, depth + 1));
                    pending.Push((exclusion.Exclude, depth + 1));
                    break;
                default:
                    break; // leaves end the walk
            }
        }

        return false;
    }

    private static Result<SubjectSetRewrite> Transform(RewriteNode node) =>
        node switch
        {
            RewriteNode.This => Result.Success<SubjectSetRewrite>(SubjectSetRewrite.This.Default),
            RewriteNode.ComputedSubjectSet computed => RelationshipName.Parse(computed.Relationship)
                .Map(SubjectSetRewrite (relationship) => SubjectSetRewrite.ComputedSubjectSet.Create(relationship)),
            RewriteNode.FactToSubjectSet factTo => Result.Apply(
                RelationshipName.Parse(factTo.FactsetRelationship)
                    .Map(Func<RelationshipName, SubjectSetRewrite> (factset) => computed => SubjectSetRewrite.FactToSubjectSet.Create(factset, computed)),
                RelationshipName.Parse(factTo.ComputedSubjectSetRelationship)),
            RewriteNode.Union union => union.Children.Select(Transform).Sequence()
                .Bind(children => SubjectSetRewrite.Union.Create(children).Map(SubjectSetRewrite (rewrite) => rewrite)),
            RewriteNode.Intersection intersection => intersection.Children.Select(Transform).Sequence()
                .Bind(children => SubjectSetRewrite.Intersection.Create(children).Map(SubjectSetRewrite (rewrite) => rewrite)),
            // the last inhabitant of the closed hierarchy: a discard arm (rather than a type pattern)
            // keeps the compiler from synthesizing an unreachable default branch under the switch
            _ => TransformExclusion((RewriteNode.Exclusion)node),
        };

    private static Result<SubjectSetRewrite> TransformExclusion(RewriteNode.Exclusion exclusion) =>
        Result.Apply(
            Transform(exclusion.Include)
                .Map(Func<SubjectSetRewrite, (SubjectSetRewrite Include, SubjectSetRewrite Exclude)> (include) => exclude => (include, exclude)),
            Transform(exclusion.Exclude))
        .Bind(operands => SubjectSetRewrite.Exclusion.Create(operands.Include, operands.Exclude).Map(SubjectSetRewrite (rewrite) => rewrite));

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

        var thisTerm = Token.EqualTo(RewriteExpressionToken.This).Select(_ => (RewriteNode)RewriteNode.This.Instance);

        var computed = identifier.Select(name => (RewriteNode)new RewriteNode.ComputedSubjectSet(name));

        var factToSubjectSet =
            from lparen in Token.EqualTo(RewriteExpressionToken.LeftParen)
            from factset in identifier
            from comma in Token.EqualTo(RewriteExpressionToken.Comma)
            from computedSubjectSetRelationship in identifier
            from rparen in Token.EqualTo(RewriteExpressionToken.RightParen)
            select (RewriteNode)new RewriteNode.FactToSubjectSet(factset, computedSubjectSetRelationship);

        TokenListParser<RewriteExpressionToken, RewriteNode>? expressionRef = null;
        var term = factToSubjectSet.Try().Or(thisTerm.Try()).Or(computed)
            .Or(Superpower.Parse.Ref(() => expressionRef!)
                .Between(Token.EqualTo(RewriteExpressionToken.LeftParen), Token.EqualTo(RewriteExpressionToken.RightParen)));

        var exclusion =
            from include in term
            from excludes in Token.EqualTo(RewriteExpressionToken.Exclusion).IgnoreThen(term).Many()
            select excludes.Aggregate(include, (accumulated, exclude) => new RewriteNode.Exclusion(accumulated, exclude));

        var intersection =
            from first in exclusion
            from rest in Token.EqualTo(RewriteExpressionToken.Intersection).IgnoreThen(exclusion).Many()
            select Chain(first, rest, static children => new RewriteNode.Intersection(children));

        expressionRef =
            from first in intersection
            from rest in Token.EqualTo(RewriteExpressionToken.Union).IgnoreThen(intersection).Many()
            select Chain(first, rest, static children => new RewriteNode.Union(children));

        return expressionRef;
    }

    /// <summary>
    /// Collapses one precedence level's run into a single n-ary node. <c>a | b | c</c> becomes a single three-child union, and a lone operand
    /// passes through untouched, so the level costs nothing when its operator is absent. Only the operands this level parsed are gathered. A
    /// parenthesized operand arrives as an opaque <see cref="RewriteNode"/> and is never absorbed, so <c>(a | b) | c</c> keeps its nested
    /// shape and round-trips structurally.
    /// </summary>
    /// <returns><paramref name="first"/> when <paramref name="rest"/> is empty; otherwise a node built by <paramref name="materialize"/> over all operands.</returns>
    private static RewriteNode Chain(RewriteNode first, RewriteNode[] rest, Func<ImmutableArray<RewriteNode>, RewriteNode> materialize) =>
        rest.Length == 0 ? first : materialize([first, .. rest]);
}
