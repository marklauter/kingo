using Superpower;
using Superpower.Parsers;
using Superpower.Tokenizers;

namespace Kingo.Policies.Pdl;

public static class PdlTokenizer
{
    public static Tokenizer<PdlToken> Create() =>
        new TokenizerBuilder<PdlToken>()
            .Ignore(Span.WhiteSpace)
            .Match(Span.EqualTo("\r\n").Try().Or(Span.EqualTo("\n")), PdlToken.Newline, true)
            .Match(Span.EqualTo("pn:"), PdlToken.PolicyPrefix, true)
            .Match(Span.EqualTo("re:"), PdlToken.RelationshipPrefix, true)
            .Match(Span.EqualTo("cp:"), PdlToken.ComputedPrefix, true)
            .Match(Span.EqualTo("tp:"), PdlToken.TuplePrefix, true)
            .Match(Character.EqualTo('|'), PdlToken.Union, true)
            .Match(Character.EqualTo('&'), PdlToken.Intersection, true)
            .Match(Character.EqualTo('!'), PdlToken.Exclusion, true)
            .Match(Character.EqualTo('('), PdlToken.LeftParen, true)
            .Match(Character.EqualTo(')'), PdlToken.RightParen, true)
            .Match(Character.EqualTo(','), PdlToken.Comma, true)
            .Match(Comment.ShellStyle, PdlToken.Comment, true)
            .Match(Superpower.Parsers.Identifier.CStyle, PdlToken.Identifier, true)
            .Build();
}
