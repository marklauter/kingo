using Kingo.Testing;
using System.Reflection;

namespace Kingo.Graphs.Tests.Architecture;

public sealed class ArchitectureTests()
    : ArchitectureTestsBase(Assembly.Load("Kingo.Graphs"), @"^Kingo\.Graphs(\..*)?$")
{
    /// <summary>
    /// The statement model and the config model are independent halves of the domain — they meet only in the rewrite
    /// interpreter, which consumes both. Neither references the other; their only shared vocabulary is the identifiers
    /// in <c>Kingo</c>.
    /// </summary>
    [Fact]
    public void DoesNotDependOnDomains()
    {
        var violations = Assembly.Load("Kingo.Graphs")
            .GetReferencedAssemblies()
            .Where(reference => reference.Name == "Kingo.Domains")
            .Select(reference => reference.FullName)
            .ToList();

        if (violations.Count > 0)
            Assert.Fail($"the statement model never depends on the config model — the two meet in the rewrite interpreter, not in each other. Violations: {string.Join(", ", violations)}");
    }
}
