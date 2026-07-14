using System.Diagnostics.CodeAnalysis;

namespace Results;

/// <summary>
/// A named domain failure carrying a typed category, a machine-readable code, and a human-readable message. Every valid instance comes from the static factories; <c>default(Error)</c> is an uninitialized instance — itself a bug — whose <see cref="Code"/> and <see cref="Message"/> reads throw <see cref="InvalidOperationException"/> rather than leaking nulls through their non-nullable declarations.
/// </summary>
[SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "the domain word is 'error' and matches the ErrorOr pattern; alternatives like 'Failure' or 'DomainError' would obscure intent")]
public readonly record struct Error
{
    /// <summary>The category of failure — drives caller's handling logic.</summary>
    public ErrorType Type { get; }

    private const string UninitializedMessage = "uninitialized Error: valid instances come from the Error factories";

    /// <summary>Stable, machine-readable identifier for the specific failure (e.g. "tuple.not_found"). Throws <see cref="InvalidOperationException"/> on an uninitialized (default) instance.</summary>
    public string Code => field ?? throw new InvalidOperationException(UninitializedMessage);

    /// <summary>Human-readable message; suitable for logs and error responses. Throws <see cref="InvalidOperationException"/> on an uninitialized (default) instance.</summary>
    public string Message => field ?? throw new InvalidOperationException(UninitializedMessage);

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

    /// <summary>Construct an <see cref="ErrorType.Undefined"/> error with the given <paramref name="code"/> and <paramref name="message"/>.</summary>
    public static Error Undefined(string code, string message) => Create(ErrorType.Undefined, code, message);
}
