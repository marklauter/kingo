namespace Kingo.Facts;

/// <summary>
/// The set of subjects that hold <see cref="Relationship"/> on <see cref="Resource"/>. The <c>&lt;subject-set&gt;</c> production of the fact grammar:
/// <c>&lt;resource&gt;#&lt;relationship name&gt;</c>, for example, <c>io/doc:readme#viewer</c>. The left-hand side of every <see cref="Fact"/>, and the member of a
/// <see cref="Fact.SubjectSetFact"/>. The name is bare: the resource carries the <see cref="NamespacePath"/> it qualifies against, so the pair
/// (<see cref="Resource"/>, <see cref="Relationship"/>) already says which relationship this is ([[identifiers]]).
/// </summary>
public sealed record SubjectSet(
    Resource Resource,
    RelationshipName Relationship);
