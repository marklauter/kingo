using Results;
using System.Diagnostics.CodeAnalysis;
using Values;

namespace Kingo.Graphs;

/// <summary>
/// A stored fact — the <c>&lt;tuple&gt;</c> production of the tuple grammar: <c>&lt;subjectset&gt;@&lt;subject&gt;</c>
/// (e.g. <c>doc:readme#viewer@user:anne</c>). A discriminated union over the two shapes its subject (the RDF-object) can take:
/// <see cref="SubjectFact"/> when the member is a <see cref="Subject"/>, <see cref="SubjectSetFact"/> when it is a <see cref="SubjectSet"/> (a userset).
/// The hierarchy is closed; pattern-match to consume. A set-membership assertion read set-first: the RDF-subject is the left-hand <see cref="SubjectSet"/>, the
/// predicate is membership itself (∋), and the RDF-object is the subject asserted into the set. An aggregate root: created and deleted atomically, never
/// mutated; its domain key is the whole value. Covers permission edges, memberships, and structural edges (e.g. <c>folder:a#parent@folder:b</c>) alike —
/// access semantics come from the rewrite rules, not the fact itself. Not to be confused with <c>Kingo.Schemas.Relationship</c>, the schema-side definition.
/// </summary>
public abstract record Fact
    : IParse<Fact>
{
    private protected Fact() { }

    private const char Separator = '@';

    /// <summary>
    /// Parses the canonical text form <c>&lt;namespace&gt;:&lt;resource-id&gt;#&lt;relationship&gt;@&lt;subject&gt;</c>. Dismisses empty or whitespace input,
    /// then dispatches on the subject's shape — a <c>#</c> in the subject builds a <see cref="SubjectSetFact"/>, otherwise a <see cref="SubjectFact"/> — with
    /// each side self-guarding and errors accumulating across the set and subject.
    /// </summary>
    public static Result<Fact> Parse(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return Result.Failure<Fact>(Error.Validation("fact.empty", "fact cannot be empty or whitespace"));

        var separator = s.IndexOf(Separator, StringComparison.Ordinal);
        if (separator < 0)
            return Result.Failure<Fact>(Error.Validation("fact.format", $"fact '{s}' is malformed; expected '<namespace>:<resource-id>#<relationship>@<subject>'"));

        var set = s[..separator];
        var subject = s[(separator + 1)..];
        return subject.Contains('#', StringComparison.Ordinal)
            ? Result.Apply(
                SubjectSet.Parse(set).Map<Func<SubjectSet, Fact>>(lhs => rhs => new SubjectSetFact(lhs, rhs)),
                SubjectSet.Parse(subject))
            : Result.Apply(
                SubjectSet.Parse(set).Map<Func<Subject, Fact>>(lhs => rhs => new SubjectFact(lhs, rhs)),
                Subject.Parse(subject));
    }

    /// <summary>
    /// A <see cref="Fact"/> whose subject is a <see cref="Subject"/> — <c>&lt;subjectset&gt;@&lt;subject-id&gt;</c>
    /// (e.g. <c>doc:readme#viewer@user:anne</c>).
    /// </summary>
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Discriminated-union case nested under its closed base; Fact.SubjectFact reads as the case of the union it belongs to.")]
    public sealed record SubjectFact(
        SubjectSet SubjectSet,
        Subject Subject)
        : Fact
    {
        /// <summary>Canonical text form: <c>&lt;subjectset&gt;@&lt;subject-id&gt;</c>.</summary>
        public override string ToString() => $"{SubjectSet}{Separator}{Subject}";
    }

    /// <summary>
    /// A <see cref="Fact"/> whose subject is a <see cref="SubjectSet"/> (a userset) — <c>&lt;subjectset&gt;@&lt;subjectset&gt;</c>
    /// (e.g. <c>doc:readme#viewer@team:sales#member</c>).
    /// </summary>
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Discriminated-union case nested under its closed base; Fact.SubjectSetFact reads as the case of the union it belongs to.")]
    public sealed record SubjectSetFact(
        SubjectSet SubjectSet,
        SubjectSet Subject)
        : Fact
    {
        /// <summary>Canonical text form: <c>&lt;subjectset&gt;@&lt;subjectset&gt;</c>.</summary>
        public override string ToString() => $"{SubjectSet}{Separator}{Subject}";
    }
}
