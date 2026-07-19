using Results;
using Values;

namespace Kingo.Graphs;

/// <summary>
/// A subject referenced directly by its identifier — the <c>&lt;subject-id&gt;</c> alternative of the <c>&lt;subject&gt;</c> production.
/// </summary>
public sealed record DirectSubject(
    SubjectIdentifier Id)
    : Subject
    , IParse<DirectSubject>
{
    /// <summary>
    /// Parses the canonical text form — a bare <see cref="SubjectIdentifier"/> (e.g. <c>user:anne</c>) — delegating every character and emptiness rule to the
    /// identifier terminal.
    /// </summary>
    public static new Result<DirectSubject> Parse(string s) =>
        SubjectIdentifier.Parse(s).Map(id => new DirectSubject(id));

    /// <summary>Canonical text form: the identifier itself.</summary>
    public override string ToString() => Id.ToString();
}
