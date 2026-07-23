using Kingo.Schemas;
using Results;
using Superpower;
using Superpower.Display;
using Superpower.Parsers;
using Superpower.Tokenizers;

namespace Kingo.Sdl;

/// <summary>
/// Parses the rewrite-expression mini-language embedded in SDL relationship values — e.g. <c>(this | editor | (parent, viewer)) ! banned</c> — into the core
/// <c>SubjectSetRewrite</c> algebra. Grammar and precedence: [[schema-definition-language]] (<c>!</c> binds tightest; <c>&amp;</c> and <c>|</c> share precedence,
/// left-associative). The Superpower grammar produces the internal <see cref="RewriteNode"/> tree; the transform at the exit parses every identifier through
/// <c>RelationshipPath.Parse</c> and accumulates the errors, so bad input surfaces as <see cref="Result{T}"/> validation failures, never exceptions.
/// </summary>
internal static class RewriteExpressionParser
{
    public static Result<SubjectSetRewrite> Parse(string expression)
    {
        var tokens = Tokenizer.TryTokenize(expression);
        if (!tokens.HasValue)
            return Result.Failure<SubjectSetRewrite>(Error.Validation("sdl.rewrite", $"invalid rewrite expression '{expression}': {tokens}"));

        if (WouldOverflowTheParserStack(tokens.Value))
            return Result.Failure<SubjectSetRewrite>(
                Error.Validation("sdl.rewrite", $"invalid rewrite expression '{expression}': parenthesis nesting exceeds {SubjectSetRewrite.MaxDepth} levels"));

        var parsed = Expression.AtEnd().TryParse(tokens.Value);
        return !parsed.HasValue
            ? Result.Failure<SubjectSetRewrite>(Error.Validation("sdl.rewrite", $"invalid rewrite expression '{expression}': {parsed}"))
            : ExceedsMaxDepth(parsed.Value)
                ? Result.Failure<SubjectSetRewrite>(SubjectSetRewrite.DepthError())
                : Transform(parsed.Value);
    }

    /// <summary>
    /// The grammar-recursion guard: <c>Superpower.Parse.Ref</c> recurses one ~2-3KB combinator frame per grouping-parenthesis level and nothing else in the
    /// grammar recurses (operator chains, exclusion links included, are iterative <c>Many()</c> folds), so on untrusted text the grouping <em>nesting</em> is
    /// the one quantity that must stay bounded before the grammar runs. A running counter measures it exactly: a factset — lexically unmistakable as the
    /// five-token window <c>( identifier , identifier )</c>, which the grammar always parses without recursing — is skipped whole, every other <c>(</c>
    /// counts, and a stray <c>)</c> below level zero is ignored here and fails as plain bad syntax in the grammar. Tree depth is not this guard's business —
    /// <see cref="ExceedsMaxDepth"/> measures it on the parsed tree, where it is exact.
    /// </summary>
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

    /// <summary>The exact factset window starting at <paramref name="openParen"/>: <c>( identifier , identifier )</c>. Anything looser is grouping to the grammar too, so it counts.</summary>
    private static bool IsFactsetShape(RewriteExpressionToken[] kinds, int openParen) =>
        openParen + 4 < kinds.Length
        && kinds[openParen + 1] == RewriteExpressionToken.Identifier
        && kinds[openParen + 2] == RewriteExpressionToken.Comma
        && kinds[openParen + 3] == RewriteExpressionToken.Identifier
        && kinds[openParen + 4] == RewriteExpressionToken.RightParen;

    /// <summary>
    /// The transform-recursion guard: <see cref="Transform"/> recurses per tree level, so before it runs the parsed tree's height is measured against
    /// <c>SubjectSetRewrite.MaxDepth</c> — on the tree itself, not a token-shape estimate, so it cannot misjudge association. An explicit stack keeps the
    /// measurement itself depth-proof, and a too-deep tree is refused with the same <c>rewrite.depth</c> error the operator factories issue: one invariant,
    /// one code.
    /// </summary>
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
                case UnionNode union:
                    foreach (var child in union.Children)
                        pending.Push((child, depth + 1));
                    break;
                case IntersectionNode intersection:
                    foreach (var child in intersection.Children)
                        pending.Push((child, depth + 1));
                    break;
                case ExclusionNode exclusion:
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
            ThisNode => Result.Success<SubjectSetRewrite>(ThisRewrite.Default),
            ComputedSubjectSetNode computed => RelationshipPath.Parse(computed.Relationship)
                .Map(SubjectSetRewrite (relationship) => ComputedSubjectSetRewrite.Create(relationship)),
            FactToSubjectSetNode factTo => Result.Apply(
                RelationshipPath.Parse(factTo.FactsetRelationship)
                    .Map(Func<RelationshipPath, SubjectSetRewrite> (factset) => computed => FactToSubjectSetRewrite.Create(factset, computed)),
                RelationshipPath.Parse(factTo.ComputedSubjectSetRelationship)),
            UnionNode union => union.Children.Select(Transform).Sequence()
                .Bind(children => UnionRewrite.Create(children).Map(SubjectSetRewrite (rewrite) => rewrite)),
            IntersectionNode intersection => intersection.Children.Select(Transform).Sequence()
                .Bind(children => IntersectionRewrite.Create(children).Map(SubjectSetRewrite (rewrite) => rewrite)),
            // the last inhabitant of the closed hierarchy: a discard arm (rather than a type pattern)
            // keeps the compiler from synthesizing an unreachable default branch under the switch
            _ => TransformExclusion((ExclusionNode)node),
        };

    private static Result<SubjectSetRewrite> TransformExclusion(ExclusionNode exclusion) =>
        Result.Apply(
            Transform(exclusion.Include)
                .Map(Func<SubjectSetRewrite, (SubjectSetRewrite Include, SubjectSetRewrite Exclude)> (include) => exclude => (include, exclude)),
            Transform(exclusion.Exclude))
        .Bind(operands => ExclusionRewrite.Create(operands.Include, operands.Exclude).Map(SubjectSetRewrite (rewrite) => rewrite));

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

        var computed = identifier.Select(name => (RewriteNode)new ComputedSubjectSetNode(name));

        var factToSubjectSet =
            from lparen in Token.EqualTo(RewriteExpressionToken.LeftParen)
            from factset in identifier
            from comma in Token.EqualTo(RewriteExpressionToken.Comma)
            from computedSubjectSetRelationship in identifier
            from rparen in Token.EqualTo(RewriteExpressionToken.RightParen)
            select (RewriteNode)new FactToSubjectSetNode(factset, computedSubjectSetRelationship);

        TokenListParser<RewriteExpressionToken, RewriteNode>? expressionRef = null;
        var term = factToSubjectSet.Try().Or(thisTerm.Try()).Or(computed)
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
    /// <c>Right</c> and is never absorbed, so <c>(a | b) | c</c> keeps its nested shape and round-trips structurally. Each run accumulates in one list and
    /// materializes once when the operator changes, so an untrusted flat chain costs linear work, not a re-copy per operand.
    /// </summary>
    private static RewriteNode ChainBinaryOperators(RewriteNode first, (RewriteExpressionToken Op, RewriteNode Right)[] rest)
    {
        var accumulated = first;
        List<RewriteNode>? run = null;
        var runOp = RewriteExpressionToken.None;
        foreach (var (op, right) in rest)
        {
            if (run is not null && op == runOp)
            {
                run.Add(right);
                continue;
            }

            if (run is not null)
                accumulated = MaterializeRun(runOp, run);
            run = [accumulated, right];
            runOp = op;
        }

        return run is null ? accumulated : MaterializeRun(runOp, run);
    }

    private static RewriteNode MaterializeRun(RewriteExpressionToken op, List<RewriteNode> children) =>
        op == RewriteExpressionToken.Union
            ? new UnionNode([.. children])
            : new IntersectionNode([.. children]);
}
