namespace Kingo.Graphs;

/// <summary>
/// A subject referenced directly by its identifier — the <c>&lt;subject-id&gt;</c> alternative of the <c>&lt;subject&gt;</c> production.
/// </summary>
public sealed record DirectSubject(
    SubjectIdentifier Id)
    : Subject
{
    /// <summary>Canonical text form: the identifier itself.</summary>
    public override string ToString() => Id.ToString();
}
