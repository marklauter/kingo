using System.Diagnostics.CodeAnalysis;

namespace Kingo.Graphs;

/// <summary>
/// A stored fact — the <c>&lt;fact&gt;</c> production of the fact grammar: <c>&lt;subjectset&gt;@&lt;subject&gt;</c>
/// (e.g. <c>doc:readme#viewer@anne</c>). A closed discriminated union over the shape of its member (the seat the grammar names <c>&lt;subject&gt;</c>,
/// the RDF-object): <see cref="SubjectFact"/> when the member is a bare <see cref="SubjectId"/>, <see cref="SubjectSetFact"/> when it is a
/// <see cref="SubjectSet"/> (a userset), and <see cref="ResourceFact"/> when it is a <see cref="Resource"/> (the object-object edge — Table 1's
/// <c>doc:readme#parent@folder:A#...</c>). The hierarchy is closed; pattern-match to consume. A set-membership assertion read set-first: the RDF-subject is the
/// left-hand <see cref="SubjectSet"/>, the predicate is membership itself (∋), and the RDF-object is the member asserted into the set. An aggregate root:
/// created and deleted atomically, never mutated; its domain key is the whole value. Covers permission edges, memberships, and structural edges alike —
/// access semantics come from the rewrite rules, not the fact itself. Not to be confused with <c>Kingo.Schemas.Relationship</c>, the spec-side definition.
/// </summary>
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Fact is a discriminated union; SubjectFact, SubjectSetFact, and ResourceFact are its cases, nested under the closed base and deliberately public — Fact.SubjectFact reads as the case it is.")]
public abstract record Fact
{
    private protected Fact() { }

    /// <summary>
    /// A <see cref="Fact"/> whose member is a bare <see cref="SubjectId"/> — <c>&lt;subjectset&gt;@&lt;subject-id&gt;</c>
    /// (e.g. <c>doc:readme#viewer@anne</c>). The identifier seats directly: subjects exist only as identifiers inside facts.
    /// </summary>
    public sealed record SubjectFact(
        SubjectSet SubjectSet,
        SubjectId Subject)
        : Fact;

    /// <summary>
    /// A <see cref="Fact"/> whose member is a <see cref="SubjectSet"/> (a userset) — <c>&lt;subjectset&gt;@&lt;subjectset&gt;</c>
    /// (e.g. <c>doc:readme#viewer@team:sales#member</c>).
    /// </summary>
    public sealed record SubjectSetFact(
        SubjectSet SubjectSet,
        SubjectSet Subject)
        : Fact;

    /// <summary>
    /// A <see cref="Fact"/> whose member is a <see cref="Resource"/> — the resource itself, the object-object edge:
    /// <c>&lt;subjectset&gt;@&lt;resource&gt;</c> (e.g. <c>folder:x#parent@folder:y</c>). Keeps a resource member distinct from a bare
    /// <see cref="SubjectFact"/> member.
    /// </summary>
    public sealed record ResourceFact(
        SubjectSet SubjectSet,
        Resource Subject)
        : Fact;
}
