using System.Diagnostics.CodeAnalysis;

namespace Kingo.Facts;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Fact is a discriminated union; SubjectFact, SubjectSetFact, and ResourceFact are its cases, nested under the closed base and deliberately public — Fact.SubjectFact reads as the case it is.")]
public abstract record Fact
{
    private protected Fact() { }

    public sealed record SubjectFact(
        SubjectSet SubjectSet,
        Identity Subject)
        : Fact;

    public sealed record SubjectSetFact(
        SubjectSet SubjectSet,
        SubjectSet Subject)
        : Fact;

    public sealed record ResourceFact(
        SubjectSet SubjectSet,
        Resource Subject)
        : Fact;
}
