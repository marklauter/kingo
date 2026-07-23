using Results;
using Values;

namespace Kingo.Graphs;

/// <summary>
/// The set of subjects that hold <see cref="Relationship"/> on <see cref="Resource"/> — the <c>&lt;subjectset&gt;</c> production:
/// <c>&lt;resource&gt;#&lt;relationship&gt;</c> (e.g. <c>doc:readme#viewer</c>). The left-hand side of every <see cref="Fact"/>, and the member of a
/// <see cref="Fact.SubjectSetFact"/>.
/// </summary>
public sealed record SubjectSet(
    Resource Resource,
    RelationshipPath Relationship)
    : IParse<SubjectSet>
{
    private const char Separator = '#';

    /// <summary>
    /// Parses the canonical text form <c>&lt;namespace&gt;:&lt;resource-id&gt;#&lt;relationship&gt;</c> with full validation, accumulating errors across both
    /// parts in left-to-right order.
    /// </summary>
    public static Result<SubjectSet> Parse(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return Result.Failure<SubjectSet>(Error.Validation("subjectset.empty", "subject set cannot be empty or whitespace"));

        var separator = s.IndexOf(Separator, StringComparison.Ordinal);
        return separator < 0
            ? Result.Failure<SubjectSet>(Error.Validation("subjectset.format", $"subject set '{s}' is malformed; expected '<namespace>:<resource-id>#<relationship>'"))
            : Result.Apply(
                Resource.Parse(s[..separator]).Map<Func<RelationshipPath, SubjectSet>>(resource => relationship => new SubjectSet(resource, relationship)),
                RelationshipPath.Parse(s[(separator + 1)..]));
    }

    /// <summary>Canonical text form: <c>&lt;resource&gt;#&lt;relationship&gt;</c>.</summary>
    public override string ToString() => $"{Resource}{Separator}{Relationship}";
}
