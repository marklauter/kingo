using LanguageExt;
using LanguageExt.Common;

namespace Kingo.Policies;

public static class PdlParseErrorCodes
{
    public const int Unexpected = 0;
    public const int SyntaxError = 1;
}

public sealed record PdlParseError(
    string Message,
    int Code,
    Option<Error> Inner = default)
    : Expected(Message, Code, Inner)
{
    // mimics Error.New
    public static PdlParseError New(int code, string message, Option<Error> inner = default) =>
        new(message, code, inner);

    public override ErrorException ToErrorException() => new PdlParseException(Message, Code);
}

public sealed class PdlParseException(
    string message,
    int code,
    Option<ErrorException> inner = default)
    : ExpectedException(message, code, inner)
{
}
