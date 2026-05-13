namespace Kingo;

/// <summary>
/// Thrown when a <see cref="Result{T}.Failure"/> escapes a <see cref="Result{T}"/>-returning boundary into an exception-throwing API — for example, when an <see cref="IParsable{TSelf}"/>.<c>Parse</c> implementation must surface a validation failure as a thrown exception. Carries the structured <see cref="Kingo.Error"/> so callers can still branch on <see cref="Error.Type"/> and <see cref="Error.Code"/>.
/// </summary>
public sealed class ValidationException : Exception
{
    /// <summary>The structured error that triggered this exception.</summary>
    public Error Error { get; }

    /// <summary>Wrap a domain <see cref="Kingo.Error"/> as a thrown exception.</summary>
    public ValidationException(Error error)
        : base(error.Message) => Error = error;

    /// <summary>Default constructor; <see cref="Error"/> remains <see langword="default"/>.</summary>
    public ValidationException() { }

    /// <summary>Construct with a message; <see cref="Error"/> remains <see langword="default"/>.</summary>
    public ValidationException(string message)
        : base(message) { }

    /// <summary>Construct with a message and inner exception; <see cref="Error"/> remains <see langword="default"/>.</summary>
    public ValidationException(string message, Exception innerException)
        : base(message, innerException) { }
}
