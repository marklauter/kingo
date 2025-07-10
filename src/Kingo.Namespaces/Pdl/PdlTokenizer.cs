using Superpower;
using Superpower.Parsers;
using Superpower.Tokenizers;
using LanguageExt;
using static LanguageExt.Prelude;
using Unit = LanguageExt.Unit;

namespace Kingo.Polcies.Pdl;

public static class PdlTokenizer
{
    public static Tokenizer<PdlToken> Create() => new TokenizerBuilder<PdlToken>()
            .Ignore(Character.EqualTo(' ').Or(Character.EqualTo('\t')).Many())
            .Match(Character.EqualTo('\n'), PdlToken.Newline)
            .Match(Span.EqualTo("ns:"), PdlToken.NamespacePrefix)
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
