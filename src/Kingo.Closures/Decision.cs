namespace Kingo.Closures;

/// <summary>
/// A judgment of one membership question, recorded with everything its replay needs. Expected to carry the question judged, a
/// <c>Query</c> member typed <see cref="Kingo.Facts.Fact.SubjectFact"/> — the putative fact held as a hypothesis rather than a
/// stored assertion — along with the verdict, the snapshot pin, the theory version, and the wall timestamp. Caller identity
/// belongs to the Check host's envelope, never here.
/// Shape to be determined. Stub capturing the term ahead of the rewrite-interpreter work.
/// </summary>
public sealed record Decision;
