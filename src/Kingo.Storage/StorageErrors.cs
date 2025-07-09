using LanguageExt;
using LanguageExt.Common;

namespace Kingo.Storage;

public static class ErrorCodes
{
    public const int DuplicateKeyError = 1;
    public const int NotFoundError = 2;
    public const int TimeoutError = 3;
    public const int VersionConflictError = 4;
}

public sealed record DocumentWriterError(
    string Message,
    int Code,
    Option<Error> Inner = default)
    : Expected(Message, Code, Inner)
{
    // mimics Error.New
    public static DocumentWriterError New(int code, string message, Option<Error> inner = default) =>
        new(message, code, inner);

    public override ErrorException ToErrorException() => new DocumentWriterException(Message, Code);
}

public sealed class DocumentWriterException(
    string message,
    int code,
    Option<ErrorException> inner = default)
    : ExpectedException(message, code, inner)
{
}
