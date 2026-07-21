using Results;
using Values;

namespace Kingo.Graphs;

/// <summary>
/// A namespaced resource — the <c>&lt;resource&gt;</c> production of the fact grammar: <c>&lt;namespace&gt;:&lt;resource-id&gt;</c> (e.g. <c>doc:readme</c>).
/// A value object of the fact context: no stored state of its own — a resource exists as the anchor facts attach to, and carries
/// <see cref="Namespace"/> as a reference-by-identity.
/// </summary>
public sealed record Resource(
    NamespaceIdentifier Namespace,
    ResourceIdentifier Id)
    : IParse<Resource>
{
    private const char Separator = ':';

    /// <summary>
    /// Parses the canonical text form <c>&lt;namespace&gt;:&lt;resource-id&gt;</c> with full validation, accumulating errors across both parts in
    /// left-to-right order.
    /// </summary>
    public static Result<Resource> Parse(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return Result.Failure<Resource>(Error.Validation("resource.empty", "resource cannot be empty or whitespace"));

        var separator = s.IndexOf(Separator, StringComparison.Ordinal);
        return separator < 0
            ? Result.Failure<Resource>(Error.Validation("resource.format", $"resource '{s}' is malformed; expected '<namespace>:<resource-id>'"))
            : Result.Apply(
                NamespaceIdentifier.Parse(s[..separator]).Map<Func<ResourceIdentifier, Resource>>(ns => id => new Resource(ns, id)),
                ResourceIdentifier.Parse(s[(separator + 1)..]));
    }

    /// <summary>Canonical text form: <c>&lt;namespace&gt;:&lt;resource-id&gt;</c>.</summary>
    public override string ToString() => $"{Namespace}{Separator}{Id}";
}
