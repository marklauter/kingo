namespace Kingo.Closures;

/// <summary>
/// The outcome of a Contains evaluation. The value the Check host serializes into the audit event alongside its request envelope.
/// Expected to carry the query judged, a <c>Query</c> member typed <see cref="Kingo.Graphs.Fact.SubjectFact"/>, the putative fact
/// held as a hypothesis rather than a stored assertion. It also carries the verdict, the snapshot pin, the spec version, and the
/// wall timestamp. Caller identity lives in the host's envelope, never here.
/// Shape to be determined. Stub capturing the domain name ahead of the rewrite-interpreter work.
/// </summary>
public sealed record Decision;
