using System.Diagnostics.CodeAnalysis;

namespace Kingo.Facts;

/// <summary>
/// A stored fact. The <c>&lt;fact&gt;</c> production of the fact grammar: <c>&lt;subjectset&gt;@&lt;subject&gt;</c>,
/// for example, <c>io/doc:readme#viewer@anne</c>. A closed discriminated union over the shape of its member, the seat the grammar names <c>&lt;subject&gt;</c>:
/// <see cref="SubjectFact"/> when the member is a bare <see cref="SubjectId"/>, <see cref="SubjectSetFact"/> when it is a
/// <see cref="SubjectSet"/>, and <see cref="ResourceFact"/> when it is a <see cref="Resource"/> (the object-object edge). The hierarchy is closed.
/// Pattern-match to consume. A set-membership assertion read set-first: the subject is the
/// left-hand <see cref="SubjectSet"/>, the predicate is membership itself (∋), and the member is asserted into the set. An aggregate root:
/// created and deleted atomically, never mutated. Its domain key is the whole value. Covers permission edges, memberships, and structural edges alike.
/// Access semantics come from the rewrite rules, not the fact itself. Not to be confused with <c>Kingo.Domains.Relationship</c>, the spec-side definition.
/// </summary>
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Fact is a discriminated union; SubjectFact, SubjectSetFact, and ResourceFact are its cases, nested under the closed base and deliberately public — Fact.SubjectFact reads as the case it is.")]
public abstract record Fact
{
    private protected Fact() { }

    /// <summary>
    /// A <see cref="Fact"/> whose member is a bare <see cref="SubjectId"/>: <c>&lt;subjectset&gt;@&lt;subject-id&gt;</c>,
    /// for example, <c>io/doc:readme#viewer@anne</c>. The identifier seats directly: subjects exist only as identifiers inside facts.
    /// </summary>
    public sealed record SubjectFact(
        SubjectSet SubjectSet,
        SubjectId Subject)
        : Fact;

    /// <summary>
    /// A <see cref="Fact"/> whose member is a <see cref="SubjectSet"/>: <c>&lt;subjectset&gt;@&lt;subjectset&gt;</c>,
    /// for example, <c>io/doc:readme#viewer@io/team:sales#member</c>.
    /// </summary>
    public sealed record SubjectSetFact(
        SubjectSet SubjectSet,
        SubjectSet Subject)
        : Fact;

    /// <summary>
    /// A <see cref="Fact"/> whose member is a <see cref="Resource"/>, the object-object edge:
    /// <c>&lt;subjectset&gt;@&lt;resource&gt;</c>, for example, <c>io/folder:x#parent@io/folder:y</c>. Keeps a resource member distinct from a bare
    /// <see cref="SubjectFact"/> member.
    /// </summary>
    public sealed record ResourceFact(
        SubjectSet SubjectSet,
        Resource Subject)
        : Fact;
}
