using Kingo.Policies.Puddle;
using LanguageExt;
using Superpower;
using Superpower.Display;
using Superpower.Model;
using Superpower.Parsers;
using Superpower.Tokenizers;

namespace Kingo.Policies.Pdl;

internal enum PdlToken
{
    None,

    [Token(Category = "identifier", Example = "myFile")]
    Identifier,

    [Token(Category = "keyword", Example = "namespace | /n")]
    NamespacePrefix,

    [Token(Category = "keyword", Example = "relation | /r")]
    RelationshipPrefix,

    [Token(Category = "keyword", Example = "computed | /c")]
    ComputedPrefix,

    [Token(Category = "keyword", Example = "tuple | /t")]
    TuplePrefix,

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

internal static class PdlTokenizer
{
    public static Eff<TokenList<PdlToken>> Tokenize(string input)
    {
        var tokensResult = Tokenizer.TryTokenize(input);
        return tokensResult.HasValue
            ? Prelude.Pure(tokensResult.Value)
            : ParseError.New(ParseErrorCodes.ParseEerror, $"tokenization error: {tokensResult}");
    }

    private static readonly Tokenizer<PdlToken> Tokenizer =
        new TokenizerBuilder<PdlToken>()
            .Ignore(Span.WhiteSpace)
            .Ignore(Comment.ShellStyle)

            .Match(Character.EqualTo('('), PdlToken.LeftParen)
            .Match(Character.EqualTo(')'), PdlToken.RightParen)
            .Match(Character.EqualTo(','), PdlToken.Comma)
            .Match(Character.EqualTo('|'), PdlToken.Union)
            .Match(Character.EqualTo('&'), PdlToken.Intersection)
            .Match(Character.EqualTo('!'), PdlToken.Exclusion)

            .Match(Span.EqualToIgnoreCase("namespace").Try().Or(Span.EqualToIgnoreCase("/n")), PdlToken.NamespacePrefix, true)
            .Match(Span.EqualToIgnoreCase("relation").Try().Or(Span.EqualToIgnoreCase("/r")), PdlToken.RelationshipPrefix, true)
            .Match(Span.EqualToIgnoreCase("computed").Try().Or(Span.EqualToIgnoreCase("/c")), PdlToken.ComputedPrefix, true)
            .Match(Span.EqualToIgnoreCase("tuple").Try().Or(Span.EqualToIgnoreCase("/t")), PdlToken.TuplePrefix, true)
            .Match(Span.EqualToIgnoreCase("this"), PdlToken.This, true)
            .Match(Superpower.Parsers.Identifier.CStyle, PdlToken.Identifier, true)

            .Build();
}
