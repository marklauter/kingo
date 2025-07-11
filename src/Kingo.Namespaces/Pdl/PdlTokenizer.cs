using LanguageExt;
using Superpower;
using Superpower.Parsers;
using Superpower.Tokenizers;
using static LanguageExt.Prelude;
using Unit = LanguageExt.Unit;

namespace Kingo.Policies.Pdl;

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
        Character.EqualTo('#')
            .Then(_ => Character.ExceptIn('\n').Many())
            .Select(_ => unit);

    private static readonly TextParser<Unit> Identifier =
        Character.Letter.Or(Character.EqualTo('_'))
            .Then(first => Character.LetterOrDigit.Or(Character.EqualTo('_')).Many())
            .Select(_ => unit);
}
