using Results;
using System.Diagnostics.CodeAnalysis;
using Values;

namespace Kingo.Graphs;

/// <summary>
/// A stored fact — the <c>&lt;fact&gt;</c> production of the fact grammar: <c>&lt;subjectset&gt;@&lt;subject&gt;</c>
/// (e.g. <c>doc:readme#viewer@user:anne</c>). A closed discriminated union over the shape of its member (the seat the grammar names <c>&lt;subject&gt;</c>,
/// the RDF-object): <see cref="SubjectFact"/> when the member is a bare <see cref="SubjectIdentifier"/>, <see cref="SubjectSetFact"/> when it is a
/// <see cref="SubjectSet"/> (a userset), and <see cref="ResourceFact"/> when it is a <see cref="Resource"/> carried in canonical text by the <c>#...</c> marker
/// (e.g. <c>folder:x#parent@folder:y#...</c>, the object-object edge — Table 1's <c>doc:readme#parent@folder:A#...</c>). The <c>#...</c> is fact-grammar
/// punctuation, not a relationship. The hierarchy is closed; pattern-match to consume. A set-membership assertion read set-first: the RDF-subject is the
/// left-hand <see cref="SubjectSet"/>, the predicate is membership itself (∋), and the RDF-object is the member asserted into the set. An aggregate root:
/// created and deleted atomically, never mutated; its domain key is the whole value. Covers permission edges, memberships, and structural edges alike —
/// access semantics come from the rewrite rules, not the fact itself. Not to be confused with <c>Kingo.Schemas.Relationship</c>, the schema-side definition.
/// </summary>
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Fact is a discriminated union; SubjectFact, SubjectSetFact, and ResourceFact are its cases, nested under the closed base and deliberately public — Fact.SubjectFact reads as the case it is.")]
public abstract record Fact
    : IParse<Fact>
{
    private protected Fact() { }

    private const char Separator = '@';
    private const string ResourceMarker = "#...";

    /// <summary>
    /// Parses the canonical text form <c>&lt;namespace&gt;:&lt;resource-id&gt;#&lt;relationship&gt;@&lt;subject&gt;</c>. Dismisses empty or whitespace input,
    /// then dispatches on the member's shape — a member ending in <c>#...</c> builds a <see cref="ResourceFact"/> (the resource is the text before the marker),
    /// a member carrying a <c>#</c> builds a <see cref="SubjectSetFact"/>, and a bare member builds a <see cref="SubjectFact"/> — with each side self-guarding
    /// and errors accumulating across the set and member in left-to-right order.
    /// </summary>
    public static Result<Fact> Parse(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return Result.Failure<Fact>(Error.Validation("fact.empty", "fact cannot be empty or whitespace"));

        var separator = s.IndexOf(Separator, StringComparison.Ordinal);
        if (separator < 0)
            return Result.Failure<Fact>(Error.Validation("fact.format", $"fact '{s}' is malformed; expected '<namespace>:<resource-id>#<relationship>@<subject>'"));

        var subjectSet = SubjectSet.Parse(s[..separator]);
        var member = s[(separator + 1)..];

        return member.EndsWith(ResourceMarker, StringComparison.Ordinal)
            ? Result.Apply(
                subjectSet.Map<Func<Resource, Fact>>(lhs => rhs => new ResourceFact(lhs, rhs)),
                Resource.Parse(member[..^ResourceMarker.Length]))
            : member.Contains('#', StringComparison.Ordinal)
                ? Result.Apply(
                    subjectSet.Map<Func<SubjectSet, Fact>>(lhs => rhs => new SubjectSetFact(lhs, rhs)),
                    SubjectSet.Parse(member))
                : Result.Apply(
                    subjectSet.Map<Func<SubjectIdentifier, Fact>>(lhs => rhs => new SubjectFact(lhs, rhs)),
                    SubjectIdentifier.Parse(member));
    }

    /// <summary>
    /// A <see cref="Fact"/> whose member is a bare <see cref="SubjectIdentifier"/> — <c>&lt;subjectset&gt;@&lt;subject-id&gt;</c>
    /// (e.g. <c>doc:readme#viewer@user:anne</c>). The identifier seats directly: subjects exist only as identifiers inside facts.
    /// </summary>
    public sealed record SubjectFact(
        SubjectSet SubjectSet,
        SubjectIdentifier Subject)
        : Fact
    {
        /// <summary>Canonical text form: <c>&lt;subjectset&gt;@&lt;subject-id&gt;</c>.</summary>
        public override string ToString() => $"{SubjectSet}{Separator}{Subject}";
    }

    /// <summary>
    /// A <see cref="Fact"/> whose member is a <see cref="SubjectSet"/> (a userset) — <c>&lt;subjectset&gt;@&lt;subjectset&gt;</c>
    /// (e.g. <c>doc:readme#viewer@team:sales#member</c>).
    /// </summary>
    public sealed record SubjectSetFact(
        SubjectSet SubjectSet,
        SubjectSet Subject)
        : Fact
    {
        /// <summary>Canonical text form: <c>&lt;subjectset&gt;@&lt;subjectset&gt;</c>.</summary>
        public override string ToString() => $"{SubjectSet}{Separator}{Subject}";
    }

    /// <summary>
    /// A <see cref="Fact"/> whose member is a <see cref="Resource"/> — the resource itself, the object-object edge — carried in canonical text by the
    /// <c>#...</c> marker: <c>&lt;subjectset&gt;@&lt;resource&gt;#...</c> (e.g. <c>folder:x#parent@folder:y#...</c>). The <c>#...</c> is fact-grammar
    /// punctuation that keeps a resource member distinct from a bare <see cref="SubjectFact"/> member in text; it is not a relationship.
    /// </summary>
    public sealed record ResourceFact(
        SubjectSet SubjectSet,
        Resource Subject)
        : Fact
    {
        /// <summary>Canonical text form: <c>&lt;subjectset&gt;@&lt;resource&gt;#...</c>.</summary>
        public override string ToString() => $"{SubjectSet}{Separator}{Subject}{ResourceMarker}";
    }
}
