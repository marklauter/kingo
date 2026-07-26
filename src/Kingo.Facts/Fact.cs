using System.Diagnostics.CodeAnalysis;

namespace Kingo.Facts;

/// <summary>
/// A stored fact: an assertion joining a <see cref="SubjectSet"/> to a subject. A closed discriminated union over the shape of that subject:
/// <see cref="SubjectFact"/> when the subject is a bare <see cref="SubjectId"/>, <see cref="SubjectSetFact"/> when it is a
/// <see cref="SubjectSet"/>, and <see cref="ResourceFact"/> when it is a <see cref="Resource"/> (the resource-to-resource edge). The hierarchy is closed.
/// Pattern-match to consume. A set-membership assertion read set-first: the left-hand
/// <see cref="SubjectSet"/> names the set, the predicate is membership itself (∋), and the subject is asserted into that set. An aggregate root:
/// created and deleted atomically, never mutated. Its identity is the whole value. Covers permission edges, memberships, and structural edges alike.
/// Access semantics come from the rewrite rules, not the fact itself. Not to be confused with <c>Kingo.Theories.Relation</c>, the theory-side definition.
/// </summary>
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Fact is a discriminated union; SubjectFact, SubjectSetFact, and ResourceFact are its cases, nested under the closed base and deliberately public — Fact.SubjectFact reads as the case it is.")]
public abstract record Fact
{
    private protected Fact() { }

    /// <summary>
    /// A <see cref="Fact"/> whose subject is a bare <see cref="SubjectId"/>. The identifier seats directly: subjects exist only as identifiers inside facts.
    /// </summary>
    public sealed record SubjectFact(
        SubjectSet SubjectSet,
        SubjectId Subject)
        : Fact;

    /// <summary>
    /// A <see cref="Fact"/> whose subject is a <see cref="SubjectSet"/>.
    /// </summary>
    public sealed record SubjectSetFact(
        SubjectSet SubjectSet,
        SubjectSet Subject)
        : Fact;

    /// <summary>
    /// A <see cref="Fact"/> whose subject is a <see cref="Resource"/>, the resource-to-resource edge. Keeps a resource subject distinct from a bare
    /// <see cref="SubjectFact"/> subject.
    /// </summary>
    public sealed record ResourceFact(
        SubjectSet SubjectSet,
        Resource Subject)
        : Fact;
}
