namespace Kingo.Facts;

/// <summary>
/// The set of subjects that hold <see cref="Relation"/> on <see cref="Resource"/>. The <c>&lt;subject-set&gt;</c> production of the fact grammar:
/// <c>&lt;resource&gt;#&lt;relation name&gt;</c>, for example, <c>io/doc:readme#viewer</c>. The left-hand side of every <see cref="Fact"/>, and the member of a
/// <see cref="Fact.SubjectSetFact"/>. The name is bare: the resource carries the <see cref="NamespacePath"/> it qualifies against, so the pair
/// (<see cref="Resource"/>, <see cref="Relation"/>) already says which relation this is ([[identifiers]]).
/// </summary>
public sealed record SubjectSet(
    Resource Resource,
    RelationName Relation);
