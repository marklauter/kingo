using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using Superpower.Tokenizers;

namespace Kingo.Policies.Pdl;

public static class PdlTokenizer
{
    public static Tokenizer<PdlToken> Create() => new TokenizerBuilder<PdlToken>()
            .Ignore(Character.EqualTo(' ').Or(Character.EqualTo('\t')).AtLeastOnce())
            .Match(Span.EqualTo("\r\n").Try().Or(Span.EqualTo("\n")), PdlToken.Newline)
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
            .Match(Comment.ShellStyle, PdlToken.Comment)
            .Match(Identifier, PdlToken.Identifier)
            .Build();

    internal static TextParser<TextSpan> Identifier { get; } =
        Span.MatchedBy(
            Character.Letter.Or(Character.EqualTo('_'))
                .IgnoreThen(Character.LetterOrDigit.Or(Character.EqualTo('_')).Many()));
}
