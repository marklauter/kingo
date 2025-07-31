using Kingo.Policies.Puddle;
using LanguageExt;
using Superpower;
using Superpower.Display;
using Superpower.Model;
using Superpower.Parsers;
using Superpower.Tokenizers;

namespace Kingo.Policies.Yaml;

internal enum RewriteExpressionToken
{
    None,

    [Token(Category = "identifier", Example = "myFile")]
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

internal static class RewriteExpressionTokenizer
{
    public static Eff<TokenList<RewriteExpressionToken>> Tokenize(string input)
    {
        var tokensResult = Tokenizer.TryTokenize(input);
        return tokensResult.HasValue
            ? Prelude.Pure(tokensResult.Value)
            : ParseError.New(ParseErrorCodes.ParseEerror, $"tokenization error: {tokensResult}");
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
            .Match(Span.EqualToIgnoreCase("this"), RewriteExpressionToken.This, true)
            .Match(Superpower.Parsers.Identifier.CStyle, RewriteExpressionToken.Identifier, true)

            .Build();
}
