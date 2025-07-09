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

public sealed record StorageError(
    string Message,
    int Code,
    Option<Error> Inner = default)
    : Expected(Message, Code, Inner)
{
    public override ErrorException ToErrorException() => new StorageException(Message, Code);
}

public sealed class StorageException(
    string message,
    int code,
    Option<ErrorException> inner = default)
    : ExpectedException(message, code, inner)
{
}
