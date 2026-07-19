namespace Kingo.Closures;

/// <summary>
/// The question Contains answers: the <see cref="Kingo.Graphs.SubjectSet"/> asked about paired with the
/// <see cref="Kingo.Graphs.DirectSubject"/> sought — check's shape, narrower by construction than a <see cref="Kingo.Graphs.Fact"/>,
/// whose subject seat is the wide <see cref="Kingo.Graphs.Subject"/> union (storage needs subjectset members; a question never does).
/// <see cref="Decision"/> carries the Query judged.
/// Shape TBD — stub capturing the domain name ahead of the rewrite-interpreter work.
/// </summary>
public sealed record Query;
