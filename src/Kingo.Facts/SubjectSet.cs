namespace Kingo.Facts;

/// <summary>
/// The subjects that hold <see cref="Relation"/> on <see cref="Resource"/>. The left-hand side of every <see cref="Fact"/>, and the subject of a
/// <see cref="Fact.SubjectSetFact"/>. The name is bare: the resource carries the <see cref="NamespacePath"/> it qualifies against, so the pair
/// (<see cref="Resource"/>, <see cref="Relation"/>) already says which relation this is.
/// </summary>
public sealed record SubjectSet(
    Resource Resource,
    RelationName Relation);
