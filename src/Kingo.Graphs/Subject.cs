using Results;
using Values;

namespace Kingo.Graphs;

/// <summary>
/// A subject — the party a fact is about — referenced by its identifier (the <c>&lt;subject-id&gt;</c> production, e.g. <c>user:anne</c>): the unified identity
/// a set of authn-side principals maps to; need not be human. The member of a <see cref="Fact.SubjectFact"/>.
/// </summary>
public sealed record Subject(
    SubjectIdentifier Id)
    : IParse<Subject>
{
    /// <summary>
    /// Parses the canonical text form — a bare <see cref="SubjectIdentifier"/> — delegating every character and emptiness rule to the identifier terminal.
    /// </summary>
    public static Result<Subject> Parse(string s) =>
        SubjectIdentifier.Parse(s).Map(id => new Subject(id));

    /// <summary>Canonical text form: the identifier itself.</summary>
    public override string ToString() => Id.ToString();
}
