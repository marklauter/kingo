using Kingo.Resources;
using Kingo.Subjects;
using Results;
using Values;

namespace Kingo.Statements;

/// <summary>
/// A stored statement — the <c>&lt;tuple&gt;</c> production of the tuple grammar: <c>&lt;resource&gt;#&lt;relationship&gt;@&lt;subject&gt;</c> (e.g. <c>doc:readme#viewer@user:anne</c>). A subject&#8211;predicate&#8211;object triple in the RDF sense: <see cref="Relationship"/> is the predicate connecting <see cref="Subject"/> to <see cref="Resource"/>. An aggregate root: created and deleted atomically, never mutated; its domain key is the whole value. Covers permission edges, memberships, and structural edges (e.g. <c>folder:a#parent@folder:b</c>) alike — access semantics come from the rewrite rules, not the statement itself. Not to be confused with <c>Kingo.Namespaces.Relationship</c>, the policy-side definition.
/// </summary>
public sealed record Statement(
    Resource Resource,
    RelationshipIdentifier Relationship,
    Subject Subject)
    : IParse<Statement>
{
    private const char Separator = '@';

    /// <summary>The statement's left-hand side as the <see cref="Subjects.SubjectSet"/> it asserts membership in.</summary>
    public SubjectSet SubjectSet => new(Resource, Relationship);

    /// <summary>
    /// Parses the canonical text form <c>&lt;namespace&gt;:&lt;resource-id&gt;#&lt;relationship&gt;@&lt;subject&gt;</c> with full validation, accumulating errors across both sides.
    /// </summary>
    public static Result<Statement> Parse(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return Result.Failure<Statement>(Error.Validation("statement.empty", "statement cannot be empty or whitespace"));

        var separator = s.IndexOf(Separator, StringComparison.Ordinal);
        return separator < 0
            ? Result.Failure<Statement>(Error.Validation("statement.format", $"statement '{s}' is malformed; expected '<namespace>:<resource-id>#<relationship>@<subject>'"))
            : Result.Apply(
                SubjectSet.Parse(s[..separator]).Map<Func<Subject, Statement>>(set => subject => new Statement(set.Resource, set.Relationship, subject)),
                Subject.Parse(s[(separator + 1)..]));
    }

    /// <summary>Canonical text form: <c>&lt;resource&gt;#&lt;relationship&gt;@&lt;subject&gt;</c>.</summary>
    public override string ToString() => $"{SubjectSet}{Separator}{Subject}";
}
