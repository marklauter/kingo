using Results;
using System.Collections.Immutable;

namespace Kingo.Theories;

public sealed record Theory
{
    public TheoryName Name { get; }

    public ImmutableArray<Namespace> Namespaces { get; }

    private Theory(TheoryName name, ImmutableArray<Namespace> namespaces) =>
        (Name, Namespaces) = (name, namespaces);

    public static Result<Theory> Create(TheoryName name, ImmutableArray<Namespace> namespaces)
    {
        if (namespaces.IsDefaultOrEmpty)
            return Result.Failure<Theory>(
                Error.Validation(Diagnostics.ErrorCodes.Theory.Empty, ErrorMessage.Unchecked("a theory requires at least one namespace; the absence of namespaces is the absence of a theory")));

        var duplicates = namespaces
            .GroupBy(ns => ns.Name)
            .Where(group => group.Count() > 1)
            .Select(group => Error.Validation(
                Diagnostics.ErrorCodes.Theory.DuplicateNamespace,
                ErrorMessage.Unchecked($"namespace '{group.Key}' is defined more than once in the theory")))
            .ToImmutableArray();

        return duplicates.IsEmpty
            ? Result.Success(new Theory(name, namespaces))
            : Result.Failure<Theory>(duplicates);
    }

    public bool Equals(Theory? other) =>
        other is not null
        && Name == other.Name
        && Namespaces.AsSpan().SequenceEqual(other.Namespaces.AsSpan());

    public override int GetHashCode() => HashCode.Combine(Name, SequenceHash.Of(Namespaces));
}
