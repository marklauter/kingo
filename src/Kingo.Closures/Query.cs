using Kingo.Graphs;
using Results;
using Values;

namespace Kingo.Closures;

/// <summary>
/// The question Contains answers — the <c>&lt;subjectset&gt;@&lt;subject-id&gt;</c> shape (e.g. <c>doc:readme#viewer@user:anne</c>):
/// the <see cref="Graphs.Subject.SubjectSet"/> asked about paired with the <see cref="Graphs.Subject.DirectSubject"/> sought. Narrower by construction than a
/// <see cref="Fact"/>, whose subject seat is the wide <see cref="Graphs.Subject"/> union: storage needs subjectset members for userset
/// expansion; a question never does — "does subjectset A contain subjectset B" has no meaning in ReBAC, so the shape is unrepresentable
/// here. <see cref="Decision"/> carries the Query judged.
/// </summary>
public sealed record Query(
    Subject.SubjectSet SubjectSet,
    Subject.DirectSubject Subject)
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
                Subject.SubjectSet.Parse(s[..separator]).Map<Func<Subject.DirectSubject, Query>>(set => subject => new Query(set, subject)),
                ParseSubject(s[(separator + 1)..]));
    }

    private static Result<Subject.DirectSubject> ParseSubject(string s) =>
        s.Contains('#', StringComparison.Ordinal)
            ? Result.Failure<Subject.DirectSubject>(Error.Validation("query.subject", $"query subject '{s}' is a subjectset; a query's subject is always a direct subject"))
            : SubjectIdentifier.Parse(s).Map(id => new Subject.DirectSubject(id));

    /// <summary>Canonical text form: <c>&lt;subjectset&gt;@&lt;subject-id&gt;</c>.</summary>
    public override string ToString() => $"{SubjectSet}{Separator}{Subject}";
}
