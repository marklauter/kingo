namespace Kingo.Closures;

/// <summary>
/// The outcome of a Contains evaluation — the value the Check host serializes into the audit event (CloudTrail-style) alongside its
/// request envelope. Expected to carry the query judged — a <c>Query</c> member typed <see cref="Kingo.Graphs.Fact.SubjectFact"/>, the
/// putative fact held as a hypothesis rather than a stored assertion — the verdict, the snapshot pin, the schema version, and the wall
/// timestamp; caller identity lives in the host's envelope, never here.
/// Shape TBD — stub capturing the domain name ahead of the rewrite-interpreter work.
/// </summary>
public sealed record Decision;
