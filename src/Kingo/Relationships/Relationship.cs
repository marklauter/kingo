using Kingo.Subjects;
using Results;
using Values;

namespace Kingo.Relationships;

/// <summary>
/// The stored fact — the <c>&lt;tuple&gt;</c> production of the tuple grammar: <c>&lt;resource&gt;#&lt;relationship&gt;@&lt;subject&gt;</c> (e.g. <c>doc:readme#viewer@user:anne</c>) — asserting membership of <see cref="Subject"/> in <see cref="SubjectSet"/>. An aggregate root: created and deleted atomically, never mutated; its domain key is the whole value. Not to be confused with <c>Kingo.Namespaces.RelationshipDefinition</c>, the policy-side definition.
/// </summary>
public sealed record Relationship(SubjectSet SubjectSet, Subject Subject)
    : IParse<Relationship>
{
    private const char Separator = '@';

    /// <summary>
    /// Parses the canonical text form <c>&lt;namespace&gt;:&lt;resource-id&gt;#&lt;relationship&gt;@&lt;subject&gt;</c> with full validation, accumulating errors across both sides.
    /// </summary>
    public static Result<Relationship> Parse(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return Result.Failure<Relationship>(Error.Validation("relationship.empty", "relationship cannot be empty or whitespace"));

        var separator = s.IndexOf(Separator, StringComparison.Ordinal);
        return separator < 0
            ? Result.Failure<Relationship>(Error.Validation("relationship.format", $"relationship '{s}' is malformed; expected '<namespace>:<resource-id>#<relationship>@<subject>'"))
            : Result.Apply(
                SubjectSet.Parse(s[..separator]).Map<Func<Subject, Relationship>>(set => subject => new Relationship(set, subject)),
                Subject.Parse(s[(separator + 1)..]));
    }

    /// <summary>Canonical text form: <c>&lt;resource&gt;#&lt;relationship&gt;@&lt;subject&gt;</c>.</summary>
    public override string ToString() => $"{SubjectSet}{Separator}{Subject}";
}
