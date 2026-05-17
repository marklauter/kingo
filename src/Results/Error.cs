using System.Diagnostics.CodeAnalysis;

namespace Results;

/// <summary>
/// A named domain failure carrying a typed category, a machine-readable code, and a human-readable message.
/// </summary>
[SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "the domain word is 'error' and matches the ErrorOr pattern; alternatives like 'Failure' or 'DomainError' would obscure intent")]
public readonly record struct Error
{
    /// <summary>The category of failure — drives caller's handling logic.</summary>
    public ErrorType Type { get; }

    /// <summary>Stable, machine-readable identifier for the specific failure (e.g. "tuple.not_found").</summary>
    public string Code { get; }

    /// <summary>Human-readable message; suitable for logs and error responses.</summary>
    public string Message { get; }

    private Error(ErrorType type, string code, string message)
    {
        Type = type;
        Code = code;
        Message = message;
    }

    private static Error Create(ErrorType type, string code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new Error(type, code, message);
    }

    /// <summary>Construct a <see cref="ErrorType.Validation"/> error with the given <paramref name="code"/> and <paramref name="message"/>.</summary>
    public static Error Validation(string code, string message) => Create(ErrorType.Validation, code, message);

    /// <summary>Construct a <see cref="ErrorType.NotFound"/> error with the given <paramref name="code"/> and <paramref name="message"/>.</summary>
    public static Error NotFound(string code, string message) => Create(ErrorType.NotFound, code, message);

    /// <summary>Construct a <see cref="ErrorType.Gone"/> error with the given <paramref name="code"/> and <paramref name="message"/>.</summary>
    public static Error Gone(string code, string message) => Create(ErrorType.Gone, code, message);

    /// <summary>Construct a <see cref="ErrorType.Conflict"/> error with the given <paramref name="code"/> and <paramref name="message"/>.</summary>
    public static Error Conflict(string code, string message) => Create(ErrorType.Conflict, code, message);

    /// <summary>Construct an <see cref="ErrorType.Unexpected"/> error with the given <paramref name="code"/> and <paramref name="message"/>.</summary>
    public static Error Unexpected(string code, string message) => Create(ErrorType.Unexpected, code, message);
}
