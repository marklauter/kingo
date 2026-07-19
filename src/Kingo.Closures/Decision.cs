namespace Kingo.Closures;

/// <summary>
/// The outcome of a Contains evaluation — the value the Check host serializes into the audit event (CloudTrail-style) alongside its
/// request envelope. Expected to carry the question judged (the <see cref="Kingo.Graphs.SubjectSet"/> asked about and the
/// <see cref="Kingo.Graphs.DirectSubject"/> sought), the verdict, the snapshot pin, the schema version, and the wall timestamp; caller
/// identity lives in the host's envelope, never here.
/// Shape TBD — stub capturing the domain name ahead of the rewrite-interpreter work.
/// </summary>
public sealed record Decision;
