namespace Results;

/// <summary>
/// Named failure shapes the domain knows how to act on.
/// </summary>
public enum ErrorType
{
    /// <summary>Failure outside the domain's named cases. Treated as a bug rather than a domain outcome.</summary>
    Unexpected,

    /// <summary>Input failed validation. Caller should fix the input and retry.</summary>
    Validation,

    /// <summary>Resource was not found. May or may not have ever existed.</summary>
    NotFound,

    /// <summary>Resource existed but has been deleted. Distinct from <see cref="NotFound"/>.</summary>
    Gone,

    /// <summary>Operation would violate a uniqueness or version invariant.</summary>
    Conflict,
}
