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

    [Token(Category = "keyword", Example = "policy")]
    PolicyPrefix,

    [Token(Category = "keyword", Example = "relation | rel")]
    RelationshipPrefix,

    [Token(Category = "keyword", Example = "computed | cmp")]
    ComputedPrefix,

    [Token(Category = "keyword", Example = "tuple | tpl")]
    TuplePrefix,

    [Token(Category = "keyword", Example = "direct | dir")]
    Direct,

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
    private static readonly Tokenizer<PdlToken> Tokenizer = Create();

    public static Either<ParseError, TokenList<PdlToken>> TryTokenize(string input)
    {
        var tokensResult = Tokenizer.TryTokenize(input);
        return tokensResult.HasValue
            ? tokensResult.Value
            : ParseError.New(ErrorCodes.ParseEerror, $"tokenization error: {tokensResult}");
    }

    private static Tokenizer<PdlToken> Create() =>
        new TokenizerBuilder<PdlToken>()
            .Ignore(Span.WhiteSpace)
            .Ignore(Comment.ShellStyle)

            .Match(Character.EqualTo('('), PdlToken.LeftParen)
            .Match(Character.EqualTo(')'), PdlToken.RightParen)
            .Match(Character.EqualTo(','), PdlToken.Comma)
            .Match(Character.EqualTo('|'), PdlToken.Union)
            .Match(Character.EqualTo('&'), PdlToken.Intersection)
            .Match(Character.EqualTo('!'), PdlToken.Exclusion)

            .Match(Span.EqualToIgnoreCase("policy"), PdlToken.PolicyPrefix, true)
            .Match(Span.EqualToIgnoreCase("relation").Try().Or(Span.EqualToIgnoreCase("rel")), PdlToken.RelationshipPrefix, true)
            .Match(Span.EqualToIgnoreCase("computed").Try().Or(Span.EqualToIgnoreCase("cmp")), PdlToken.ComputedPrefix, true)
            .Match(Span.EqualToIgnoreCase("tuple").Try().Or(Span.EqualToIgnoreCase("tpl")), PdlToken.TuplePrefix, true)
            .Match(Span.EqualToIgnoreCase("direct").Try().Or(Span.EqualToIgnoreCase("dir")), PdlToken.Direct, true)
            .Match(Superpower.Parsers.Identifier.CStyle, PdlToken.Identifier, true)

            .Build();
}
