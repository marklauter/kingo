namespace Kingo.Storage;

public class StorageException
    : ApplicationException
{
    public StorageException()
    {
    }

    public StorageException(string? message) : base(message)
    {
    }

    public StorageException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

public sealed class ReaderException
    : StorageException
{
    public ReaderException()
    {
    }

    public ReaderException(string? message) : base(message)
    {
    }

    public ReaderException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

public sealed class SqlBuilderException
    : StorageException
{
    public SqlBuilderException()
    {
    }

    public SqlBuilderException(string? message) : base(message)
    {
    }

    public SqlBuilderException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

public sealed class WriterException
    : StorageException
{
    public WriterException()
    {
    }

    public WriterException(string? message) : base(message)
    {
    }

    public WriterException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

public sealed class NotFoundException
    : StorageException
{
    public NotFoundException()
    {
    }

    public NotFoundException(string? message) : base(message)
    {
    }

    public NotFoundException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

public sealed class DuplicateKeyException
    : StorageException
{
    public DuplicateKeyException()
    {
    }

    public DuplicateKeyException(string? message) : base(message)
    {
    }

    public DuplicateKeyException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

public sealed class VersionConflictException
    : StorageException
{
    public VersionConflictException()
    {
    }

    public VersionConflictException(string? message) : base(message)
    {
    }

    public VersionConflictException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
