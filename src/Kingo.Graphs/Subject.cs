using Results;

namespace Kingo.Graphs;

/// <summary>
/// The <c>&lt;subject&gt;</c> production of the tuple grammar — a discriminated union: either a direct subject reference (<see cref="DirectSubject"/>) or a set
/// of subjects reachable through a relationship (<see cref="SubjectSet"/>). The hierarchy is closed; pattern-match to consume. A value object of the fact
/// context — a subject is the unified identity a set of authn-side principals maps to, referenced by <see cref="SubjectIdentifier"/>.
/// </summary>
public abstract record Subject
{
    private protected Subject() { }

    /// <summary>
    /// Dismisses empty or whitespace input, then dispatches on <c>#</c> to the owning case — <see cref="SubjectSet"/> when present
    /// (e.g. <c>team:sales#member</c>), otherwise <see cref="DirectSubject"/> (e.g. <c>user:anne</c>). Each case owns its own format validation; this method
    /// only routes.
    /// </summary>
    public static Result<Subject> Parse(string s) =>
        string.IsNullOrWhiteSpace(s)
            ? Result.Failure<Subject>(Error.Validation("subject.empty", "subject cannot be empty or whitespace"))
            : s.Contains('#', StringComparison.Ordinal)
                ? SubjectSet.Parse(s).Map(Subject (set) => set)
                : DirectSubject.Parse(s).Map(Subject (d) => d);
}
