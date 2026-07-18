using Results;
using Values;

namespace Kingo.Graphs;

/// <summary>
/// The <c>&lt;subject&gt;</c> production of the tuple grammar — a discriminated union: either a direct subject reference (<see cref="DirectSubject"/>) or a set
/// of subjects reachable through a relationship (<see cref="SubjectSet"/>). The hierarchy is closed; pattern-match to consume. The subject aggregate root's
/// domain key is <see cref="SubjectIdentifier"/> — the unified identity a set of authn-side principals maps to.
/// </summary>
public abstract record Subject
    : IParse<Subject>
{
    private protected Subject() { }

    /// <summary>
    /// Parses the canonical text form: a <see cref="SubjectSet"/> when <paramref name="s"/> contains <c>#</c> (e.g. <c>team:sales#member</c>); otherwise a
    /// <see cref="DirectSubject"/> (e.g. <c>user:anne</c>).
    /// </summary>
    public static Result<Subject> Parse(string s) =>
        string.IsNullOrWhiteSpace(s)
            ? Result.Failure<Subject>(Error.Validation("subject.empty", "subject cannot be empty or whitespace"))
            : s.Contains('#', StringComparison.Ordinal)
                ? SubjectSet.Parse(s).Map(Subject (set) => set)
                : SubjectIdentifier.Parse(s).Map(Subject (id) => new DirectSubject(id));
}
