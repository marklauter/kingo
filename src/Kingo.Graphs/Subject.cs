using Results;
using Values;

namespace Kingo.Graphs;

/// <summary>
/// The <c>&lt;subject&gt;</c> production of the tuple grammar — a discriminated union: either a direct subject reference (<see cref="DirectSubject"/>) or a set
/// of subjects reachable through a relationship (<see cref="SubjectSet"/>). The hierarchy is closed; pattern-match to consume. The subject aggregate root's
/// domain key is <see cref="SubjectIdentifier"/> — the unified identity a set of authn-side principals maps to.
/// </summary>
public abstract record Subject
    : IParse<Subject>
{
    private protected Subject() { }

    /// <summary>
    /// Parses the canonical text form: a <see cref="SubjectSet"/> when <paramref name="s"/> contains <c>#</c> (e.g. <c>team:sales#member</c>); otherwise a
    /// <see cref="DirectSubject"/> (e.g. <c>user:anne</c>).
    /// </summary>
    public static Result<Subject> Parse(string s) =>
        string.IsNullOrWhiteSpace(s)
            ? Result.Failure<Subject>(Error.Validation("subject.empty", "subject cannot be empty or whitespace"))
            : s.Contains('#', StringComparison.Ordinal)
                ? SubjectSet.Parse(s).Map(Subject (set) => set)
                : SubjectIdentifier.Parse(s).Map(Subject (id) => new DirectSubject(id));
}

/// <summary>
/// A subject referenced directly by its identifier — the <c>&lt;subject-id&gt;</c> alternative of the <c>&lt;subject&gt;</c> production.
/// </summary>
public sealed record DirectSubject(
    SubjectIdentifier Id)
    : Subject
{
    /// <summary>Canonical text form: the identifier itself.</summary>
    public override string ToString() => Id.ToString();
}

/// <summary>
/// The set of subjects that hold <see cref="Relationship"/> on <see cref="Resource"/> — the <c>&lt;subjectset&gt;</c> production:
/// <c>&lt;resource&gt;#&lt;relationship&gt;</c> (e.g. <c>doc:readme#viewer</c>). Also the indirect-membership alternative of the <c>&lt;subject&gt;</c>
/// production.
/// </summary>
public sealed record SubjectSet(
    Resource Resource,
    RelationshipIdentifier Relationship)
    : Subject
    , IParse<SubjectSet>
{
    private const char Separator = '#';

    /// <summary>
    /// Parses the canonical text form <c>&lt;namespace&gt;:&lt;resource-id&gt;#&lt;relationship&gt;</c> with full validation, accumulating errors across both
    /// parts.
    /// </summary>
    public static new Result<SubjectSet> Parse(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return Result.Failure<SubjectSet>(Error.Validation("subjectset.empty", "subject set cannot be empty or whitespace"));

        var separator = s.IndexOf(Separator, StringComparison.Ordinal);
        return separator < 0
            ? Result.Failure<SubjectSet>(Error.Validation("subjectset.format", $"subject set '{s}' is malformed; expected '<namespace>:<resource-id>#<relationship>'"))
            : Result.Apply(
                Resource.Parse(s[..separator]).Map<Func<RelationshipIdentifier, SubjectSet>>(resource => relationship => new SubjectSet(resource, relationship)),
                RelationshipIdentifier.Parse(s[(separator + 1)..]));
    }

    /// <summary>Canonical text form: <c>&lt;resource&gt;#&lt;relationship&gt;</c>.</summary>
    public override string ToString() => $"{Resource}{Separator}{Relationship}";
}
