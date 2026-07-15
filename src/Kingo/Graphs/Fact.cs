using Results;
using Values;

namespace Kingo.Graphs;

/// <summary>
/// A stored statement — the <c>&lt;tuple&gt;</c> production of the tuple grammar:
/// <c>&lt;resource&gt;#&lt;relationship&gt;@&lt;subject&gt;</c> (e.g. <c>doc:readme#viewer@user:anne</c>).
/// A set-membership assertion, and an RDF triple read set-first: the RDF-subject is the
/// <see cref="SubjectSet"/> (<see cref="Resource"/> + <see cref="Relationship"/> name the set), the predicate
/// is membership itself (∋), and the RDF-object is the <see cref="Subject"/> being asserted into the set —
/// which is why the parameter order mirrors the text form. An aggregate root: created and deleted
/// atomically, never mutated; its domain key is the whole value. Covers permission edges, memberships,
/// and structural edges (e.g. <c>folder:a#parent@folder:b</c>) alike — access semantics come from the
/// rewrite rules, not the statement itself. Not to be confused with <c>Kingo.Schemas.Relationship</c>,
/// the schema-side definition.
/// </summary>
public sealed record Fact(
    Resource Resource,
    RelationshipIdentifier Relationship,
    Subject Subject)
    : IParse<Fact>
{
    private const char Separator = '@';

    /// <summary>The statement's left-hand side as the <see cref="Kingo.Graphs.SubjectSet"/> it asserts membership in.</summary>
    public SubjectSet SubjectSet => new(Resource, Relationship);

    /// <summary>
    /// Parses the canonical text form <c>&lt;namespace&gt;:&lt;resource-id&gt;#&lt;relationship&gt;@&lt;subject&gt;</c> with full validation, accumulating errors across both sides.
    /// </summary>
    public static Result<Fact> Parse(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return Result.Failure<Fact>(Error.Validation("fact.empty", "statement cannot be empty or whitespace"));

        var separator = s.IndexOf(Separator, StringComparison.Ordinal);
        return separator < 0
            ? Result.Failure<Fact>(Error.Validation("fact.format", $"statement '{s}' is malformed; expected '<namespace>:<resource-id>#<relationship>@<subject>'"))
            : Result.Apply(
                SubjectSet.Parse(s[..separator]).Map<Func<Subject, Fact>>(set => subject => new Fact(set.Resource, set.Relationship, subject)),
                Subject.Parse(s[(separator + 1)..]));
    }

    /// <summary>Canonical text form: <c>&lt;resource&gt;#&lt;relationship&gt;@&lt;subject&gt;</c>.</summary>
    public override string ToString() => $"{SubjectSet}{Separator}{Subject}";
}
