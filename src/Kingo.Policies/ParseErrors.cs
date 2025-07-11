using LanguageExt;
using LanguageExt.Common;

namespace Kingo.Policies;

public static class ParseErrorCodes
{
    public const int Unknown = 0;
    public const int ParseEerror = 1;
}

public sealed record ParseError(
    string Message,
    int Code,
    Option<Error> Inner = default)
    : Expected(Message, Code, Inner)
{
    // mimics Error.New
    public static ParseError New(int code, string message, Option<Error> inner = default) =>
        new(message, code, inner);

    public override ErrorException ToErrorException() => new ParseException(Message, Code);
}

public sealed class ParseException(
    string message,
    int code,
    Option<ErrorException> inner = default)
    : ExpectedException(message, code, inner)
{
}
