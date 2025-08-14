namespace Kingo.Storage;

public class KingoStorageException
    : ApplicationException
{
    public KingoStorageException()
    {
    }

    public KingoStorageException(string? message) : base(message)
    {
    }

    public KingoStorageException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

public class KingoReaderException
    : KingoStorageException
{
    public KingoReaderException()
    {
    }

    public KingoReaderException(string? message) : base(message)
    {
    }

    public KingoReaderException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

public class KingoSqlBuilderException
    : KingoReaderException
{
    public KingoSqlBuilderException()
    {
    }

    public KingoSqlBuilderException(string? message) : base(message)
    {
    }

    public KingoSqlBuilderException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

public sealed class KingoWriterException
    : KingoStorageException
{
    public KingoWriterException()
    {
    }

    public KingoWriterException(string? message) : base(message)
    {
    }

    public KingoWriterException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
