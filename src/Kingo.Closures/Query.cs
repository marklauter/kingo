using Kingo.Graphs;
using Results;
using Values;

namespace Kingo.Closures;

/// <summary>
/// The question Contains answers — the <c>&lt;subjectset&gt;@&lt;subject-id&gt;</c> shape (e.g. <c>doc:readme#viewer@user:anne</c>):
/// the <see cref="SubjectSet"/> asked about paired with the <see cref="Subject"/> sought. Narrower by construction than a
/// <see cref="Fact"/>: a fact's subject may be a userset (a <see cref="Fact.SubjectSetFact"/>), but a question's subject is always direct —
/// "does subjectset A contain subjectset B" has no meaning in ReBAC, so that shape is unrepresentable here. <see cref="Decision"/> carries the Query judged.
/// </summary>
public sealed record Query(
    SubjectSet SubjectSet,
    Subject Subject)
    : IParse<Query>
{
    private const char Separator = '@';

    /// <summary>
    /// Parses the canonical text form <c>&lt;namespace&gt;:&lt;resource-id&gt;#&lt;relationship&gt;@&lt;subject-id&gt;</c> with full validation,
    /// accumulating errors across both sides. A subjectset on the subject side is rejected: a query's subject is always direct.
    /// </summary>
    public static Result<Query> Parse(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return Result.Failure<Query>(Error.Validation("query.empty", "query cannot be empty or whitespace"));

        var separator = s.IndexOf(Separator, StringComparison.Ordinal);
        return separator < 0
            ? Result.Failure<Query>(Error.Validation("query.format", $"query '{s}' is malformed; expected '<namespace>:<resource-id>#<relationship>@<subject-id>'"))
            : Result.Apply(
                SubjectSet.Parse(s[..separator]).Map<Func<Subject, Query>>(set => subject => new Query(set, subject)),
                ParseSubject(s[(separator + 1)..]));
    }

    private static Result<Subject> ParseSubject(string s) =>
        s.Contains('#', StringComparison.Ordinal)
            ? Result.Failure<Subject>(Error.Validation("query.subject", $"query subject '{s}' is a subjectset; a query's subject is always a direct subject"))
            : SubjectIdentifier.Parse(s).Map(id => new Subject(id));

    /// <summary>Canonical text form: <c>&lt;subjectset&gt;@&lt;subject-id&gt;</c>.</summary>
    public override string ToString() => $"{SubjectSet}{Separator}{Subject}";
}
