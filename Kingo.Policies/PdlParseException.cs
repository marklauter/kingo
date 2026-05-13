namespace Kingo.Policies;

public static class PdlParseErrorCodes
{
    public const int Unexpected = 0;
    public const int SyntaxError = 1;
}

public sealed class PdlParseException : Exception
{
    public int Code { get; }

    public PdlParseException()
        : this(PdlParseErrorCodes.Unexpected, string.Empty) { }

    public PdlParseException(string message)
        : this(PdlParseErrorCodes.Unexpected, message) { }

    public PdlParseException(string message, Exception innerException)
        : this(PdlParseErrorCodes.Unexpected, message, innerException) { }

    public PdlParseException(int code, string message)
        : base(message) => Code = code;

    public PdlParseException(int code, string message, Exception innerException)
        : base(message, innerException) => Code = code;
}
